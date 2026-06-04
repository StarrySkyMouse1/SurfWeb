"""确保插件目录在 sys.path 中，供 AstrBot 以包路径加载时仍能 import 同级模块。"""
from __future__ import annotations

import sys
from pathlib import Path

PLUGIN_DIR = Path(__file__).resolve().parent
_plugin_dir_str = str(PLUGIN_DIR)
if _plugin_dir_str not in sys.path:
    sys.path.insert(0, _plugin_dir_str)
