from __future__ import annotations

import asyncio
from typing import Any

from astrbot.api import AstrBotConfig, logger
from astrbot.api.event import AstrMessageEvent, MessageChain, filter
from astrbot.api.star import Context, Star, register

try:
    from .listener import SurfWebRecordsListener
    from .renderer import RecordsCardRenderer
    from ._plugin_path import PLUGIN_DIR
except ImportError:
    import _plugin_path  # noqa: F401 — 将插件目录加入 sys.path
    from _plugin_path import PLUGIN_DIR
    from listener import SurfWebRecordsListener
    from renderer import RecordsCardRenderer


@register(
    "astrbot_plugin_surfweb_records",
    "SurfWeb",
    "订阅 SurfWeb 最新成绩 SignalR 推送并渲染为图片",
    "0.1.0",
    "",
)
class SurfWebRecordsPlugin(Star):
    def __init__(self, context: Context, config: AstrBotConfig):
        super().__init__(context)
        self.config = config
        self._renderer = RecordsCardRenderer(PLUGIN_DIR)
        self._listener: SurfWebRecordsListener | None = None
        self._listener_lock = asyncio.Lock()
        self._last_snapshot: list[dict[str, Any]] = []

    @filter.on_astrbot_loaded()
    async def on_astrbot_loaded(self):
        try:
            await self._ensure_listener()
        except Exception as exc:
            logger.error(f"[surfweb_records] 启动 SignalR 失败: {exc}")

    async def _ensure_listener(self) -> None:
        async with self._listener_lock:
            if self._listener is not None:
                return
            self._listener = SurfWebRecordsListener(
                hub_url=str(self.config.get("hub_url") or "http://127.0.0.1:5240/hubs/records"),
                scope=str(self.config.get("scope") or "All"),
                snapshot_page_size=int(self.config.get("snapshot_page_size") or 10),
                on_records_updated=self._on_records_updated,
                on_snapshot=self._on_snapshot,
            )
            await self._listener.start()

    async def _restart_listener(self) -> None:
        async with self._listener_lock:
            if self._listener:
                await self._listener.stop()
                self._listener = None
        await self._ensure_listener()

    def _max_rows(self) -> int:
        return max(1, min(int(self.config.get("max_records_per_image") or 8), 50))

    async def _on_records_updated(
        self,
        revision: str,
        scope: Any,
        items: list[dict[str, Any]],
    ) -> None:
        if not self.config.get("notify_on_update", True):
            return
        await self._push_records_image("update", revision, scope, items)

    async def _on_snapshot(
        self,
        revision: str,
        scope: Any,
        items: list[dict[str, Any]],
    ) -> None:
        self._last_snapshot = list(items)
        if not self.config.get("notify_on_snapshot", False):
            return
        await self._push_records_image("snapshot", revision, scope, items)

    async def _push_records_image(
        self,
        kind: str,
        revision: str,
        scope: Any,
        items: list[dict[str, Any]],
    ) -> None:
        targets = list(self.config.get("push_targets") or [])
        if not targets:
            logger.info("[surfweb_records] 未配置 push_targets，跳过发图（使用 /surfweb bind）")
            return

        limited = items[: self._max_rows()]
        payload = self._renderer.build_payload(
            kind=kind,
            revision=revision,
            scope=scope,
            raw_records=limited,
        )
        try:
            image_path = await asyncio.to_thread(self._renderer.render_png, payload)
        except Exception as exc:
            logger.error(f"[surfweb_records] WeasyPrint 渲染失败: {exc}")
            return

        chain = MessageChain().file_image(image_path)
        for umo in targets:
            try:
                await self.context.send_message(umo, chain)
            except Exception as exc:
                logger.error(f"[surfweb_records] 发送到 {umo} 失败: {exc}")

    @filter.command_group("surfweb")
    def surfweb_group(self):
        pass

    @surfweb_group.command("bind")
    async def surfweb_bind(self, event: AstrMessageEvent):
        """将当前会话加入成绩图片推送列表"""
        umo = event.unified_msg_origin
        targets: list[str] = list(self.config.get("push_targets") or [])
        if umo in targets:
            yield event.plain_result("当前会话已在推送列表中。")
            return
        targets.append(umo)
        self.config["push_targets"] = targets
        self.config.save_config()
        yield event.plain_result("已绑定本会话，将接收 SurfWeb 成绩推送图片。")

    @surfweb_group.command("unbind")
    async def surfweb_unbind(self, event: AstrMessageEvent):
        """将当前会话移出推送列表"""
        umo = event.unified_msg_origin
        targets: list[str] = list(self.config.get("push_targets") or [])
        if umo not in targets:
            yield event.plain_result("当前会话未在推送列表中。")
            return
        targets.remove(umo)
        self.config["push_targets"] = targets
        self.config.save_config()
        yield event.plain_result("已取消绑定。")

    @surfweb_group.command("status")
    async def surfweb_status(self, event: AstrMessageEvent):
        """查看 SignalR 连接与推送配置"""
        await self._ensure_listener()
        listener = self._listener
        connected = listener.connected if listener else False
        err = listener.last_error if listener else None
        targets = self.config.get("push_targets") or []
        lines = [
            f"Hub: {self.config.get('hub_url')}",
            f"Scope: {self.config.get('scope')} · 连接: {'是' if connected else '否'}",
            f"推送目标: {len(targets)} 个",
            f"更新推送: {self.config.get('notify_on_update', True)} · "
            f"快照推送: {self.config.get('notify_on_snapshot', False)}",
        ]
        if err:
            lines.append(f"最近错误: {err}")
        if not connected and not err:
            lines.append(
                "提示: 若插件列表里看不到本插件，请重启 AstrBot 容器；"
                "若能看到但连不上，请在容器内 pip install -r requirements.txt"
            )
        yield event.plain_result("\n".join(lines))

    @surfweb_group.command("start")
    async def surfweb_start(self, event: AstrMessageEvent):
        """启动或重启 SignalR 监听"""
        await self._restart_listener()
        yield event.plain_result("已启动 SurfWeb RecordsHub 监听。")

    @surfweb_group.command("preview")
    async def surfweb_preview(self, event: AstrMessageEvent):
        """用最近一次快照或示例数据预览渲染效果"""
        items = self._last_snapshot
        if not items:
            yield event.plain_result(
                "尚无快照数据。请先 /surfweb start 并确保 SurfWeb API 在运行；"
                "或将 notify_on_snapshot 设为 true 后重连。"
            )
            return
        limited = items[: self._max_rows()]
        payload = self._renderer.build_payload(
            kind="snapshot",
            revision="preview",
            scope=self.config.get("scope") or "All",
            raw_records=limited,
        )
        try:
            path = await asyncio.to_thread(self._renderer.render_png, payload)
        except Exception as exc:
            yield event.plain_result(f"渲染失败: {exc}")
            return
        yield event.image_result(path)

    async def terminate(self):
        async with self._listener_lock:
            if self._listener:
                await self._listener.stop()
                self._listener = None
        logger.info("[surfweb_records] 插件已停止")
