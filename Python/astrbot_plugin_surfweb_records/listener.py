from __future__ import annotations

import asyncio
from collections.abc import Awaitable, Callable
from typing import Any

from astrbot.api import logger

try:
    from .renderer import parse_records_message
except ImportError:
    import _plugin_path  # noqa: F401
    from renderer import parse_records_message


def _signalr_client():
    try:
        from pysignalr.client import SignalRClient

        return SignalRClient
    except ImportError as exc:
        raise RuntimeError(
            "未安装 pysignalr。请在 AstrBot 容器内执行: "
            "pip install -r .../astrbot_plugin_surfweb_records/requirements.txt"
        ) from exc

RecordsUpdatedHandler = Callable[[str, Any, list[dict[str, Any]]], Awaitable[None]]
SnapshotHandler = Callable[[str, Any, list[dict[str, Any]]], Awaitable[None]]

RECORDS_UPDATED = "RecordsUpdated"
RECENT_SNAPSHOT = "RecentSnapshot"


class SurfWebRecordsListener:
    """连接 SurfWeb RecordsHub，订阅并在收到推送时回调。"""

    def __init__(
        self,
        *,
        hub_url: str,
        scope: str,
        snapshot_page_size: int,
        on_records_updated: RecordsUpdatedHandler | None = None,
        on_snapshot: SnapshotHandler | None = None,
    ) -> None:
        self._hub_url = hub_url
        self._scope = scope
        self._snapshot_page_size = max(1, min(snapshot_page_size, 50))
        self._on_records_updated = on_records_updated
        self._on_snapshot = on_snapshot
        self._client: Any | None = None
        self._run_task: asyncio.Task | None = None
        self._stopped = asyncio.Event()
        self.connected = False
        self.last_error: str | None = None

    async def start(self) -> None:
        if self._run_task and not self._run_task.done():
            return
        self._stopped.clear()
        SignalRClient = _signalr_client()
        self._client = SignalRClient(self._hub_url)
        self._client.on_open(self._on_open)
        self._client.on_close(self._on_close)
        self._client.on_error(self._on_error)
        self._client.on(RECORDS_UPDATED, self._handle_records_updated)
        self._client.on(RECENT_SNAPSHOT, self._handle_snapshot)
        self._run_task = asyncio.create_task(
            self._client.run(),
            name="surfweb-records-signalr",
        )

    async def stop(self) -> None:
        self._stopped.set()
        if self._client:
            try:
                await self._client.send("UnsubscribeRecent", [self._scope])
            except Exception:
                pass
        if self._run_task and not self._run_task.done():
            self._run_task.cancel()
            with asyncio.suppress(asyncio.CancelledError):
                await self._run_task
        self._run_task = None
        self._client = None
        self.connected = False

    async def _on_open(self) -> None:
        self.connected = True
        self.last_error = None
        logger.info(
            f"[surfweb_records] SignalR 已连接，订阅 scope={self._scope} "
            f"pageSize={self._snapshot_page_size}"
        )
        if self._client:
            await self._client.send(
                "SubscribeRecent",
                [self._scope, self._snapshot_page_size],
            )

    async def _on_close(self) -> None:
        self.connected = False
        logger.info("[surfweb_records] SignalR 连接已关闭")

    async def _on_error(self, message: Any) -> None:
        err = getattr(message, "error", None) or str(message)
        self.last_error = str(err)
        logger.error(f"[surfweb_records] SignalR 错误: {err}")

    async def _handle_records_updated(self, payload: Any) -> None:
        revision, scope, items = parse_records_message(payload)
        if not items:
            return
        logger.info(
            f"[surfweb_records] RecordsUpdated: {len(items)} 条, revision={revision}"
        )
        if self._on_records_updated:
            await self._on_records_updated(revision, scope, items)

    async def _handle_snapshot(self, payload: Any) -> None:
        revision, scope, items = parse_records_message(payload)
        logger.info(
            f"[surfweb_records] RecentSnapshot: {len(items)} 条, revision={revision}"
        )
        if self._on_snapshot:
            await self._on_snapshot(revision, scope, items)
