from __future__ import annotations

import math
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Any

from jinja2 import Environment, FileSystemLoader, select_autoescape

SCOPE_LABELS = {
    0: "全部",
    1: "主线",
    2: "奖励关",
    3: "分段",
    "All": "全部",
    "Main": "主线",
    "Bonus": "奖励关",
    "Stage": "分段",
}


def _fmt_gap(seconds: float | None) -> str:
    if seconds is None:
        return "—"
    if seconds <= 0.001:
        return "0"
    return f"{seconds:.3f}".rstrip("0").rstrip(".")


def _mode_label(record: dict[str, Any]) -> str:
    stage = record.get("stage")
    track = record.get("track") or 0
    if stage is not None:
        return f"分段 S{stage}"
    if track and track > 0:
        return f"奖励 T{track}"
    return "主线"


@dataclass(frozen=True)
class RenderPayload:
    title: str
    subtitle: str
    scope_label: str
    revision: str
    records: list[dict[str, Any]]


def normalize_record(raw: dict[str, Any]) -> dict[str, Any]:
    gap_wr = raw.get("gapFromFirst")
    gap_pb = raw.get("gapFromPersonalBest")
    return {
        "player_name": raw.get("playerName") or f"#{raw.get('auth', '?')}",
        "map": raw.get("map") or "—",
        "time_formatted": raw.get("timeFormatted") or "—",
        "tier": raw.get("tier"),
        "first_place_time_formatted": raw.get("firstPlaceTimeFormatted"),
        "personal_best_time_formatted": raw.get("personalBestTimeFormatted"),
        "gap_from_first": gap_wr,
        "gap_from_personal_best": gap_pb,
        "gap_from_first_fmt": _fmt_gap(gap_wr),
        "gap_from_personal_best_fmt": _fmt_gap(gap_pb),
        "mode_label": _mode_label(raw),
    }


def scope_label(scope: Any) -> str:
    return SCOPE_LABELS.get(scope, str(scope))


def parse_records_message(payload: Any) -> tuple[str, Any, list[dict[str, Any]]]:
    """解析 RecordsUpdated / RecentSnapshot 的 SignalR 参数。"""
    if isinstance(payload, list):
        if len(payload) == 1 and isinstance(payload[0], dict):
            payload = payload[0]
        elif not payload:
            return "", "All", []

    if not isinstance(payload, dict):
        return "", "All", []

    revision = str(payload.get("revision") or "")
    scope = payload.get("scope", "All")
    items = payload.get("added") or payload.get("items") or []
    if not isinstance(items, list):
        items = []
    return revision, scope, items


class RecordsCardRenderer:
    def __init__(self, plugin_dir: Path) -> None:
        template_dir = plugin_dir / "templates"
        self._env = Environment(
            loader=FileSystemLoader(str(template_dir)),
            autoescape=select_autoescape(["html"]),
        )
        self._template = self._env.get_template("records_card.html")
        self._output_dir = plugin_dir / "data" / "images"
        self._output_dir.mkdir(parents=True, exist_ok=True)

    def build_payload(
        self,
        *,
        kind: str,
        revision: str,
        scope: Any,
        raw_records: list[dict[str, Any]],
    ) -> RenderPayload:
        records = [normalize_record(r) for r in raw_records]
        scope_text = scope_label(scope)
        now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        if kind == "snapshot":
            title = "Surf 最新成绩快照"
            subtitle = f"共 {len(records)} 条 · {now}"
        else:
            title = "Surf 新成绩"
            subtitle = f"新增 {len(records)} 条 · {now}"
        return RenderPayload(
            title=title,
            subtitle=subtitle,
            scope_label=scope_text,
            revision=revision or "—",
            records=records,
        )

    def _estimate_page_height(self, record_count: int) -> str:
        base = 120
        row = 58
        height = base + max(record_count, 1) * row
        return f"{min(max(height, 220), 1200)}px"

    def render_png(self, payload: RenderPayload) -> str:
        html = self._template.render(
            title=payload.title,
            subtitle=payload.subtitle,
            scope_label=payload.scope_label,
            revision=payload.revision,
            records=payload.records,
            page_height=self._estimate_page_height(len(payload.records)),
        )
        from weasyprint import HTML

        stamp = math.floor(datetime.now().timestamp() * 1000)
        out_path = self._output_dir / f"records_{stamp}.png"
        HTML(string=html, base_url=str(self._output_dir)).write_png(str(out_path))
        return str(out_path)
