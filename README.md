# 渡劫录 / Tribulation

Unity 6 修仙题材弹幕生存 Roguelike 原型。

## 当前原型

- 打开 `Assets/Scenes/Start.unity` 后直接 Play。
- 运行时会自动生成玩家、地面、摄像机、刷怪器和原型 HUD。
- WASD / 方向键移动，按住鼠标右键拖拽调整视角，飞剑会自动攻击最近敌人。
- 击杀敌人掉落灵气球，拾取后升级并提升生命、速度和伤害。
- 死亡后按 `R` 重开。

## 设计文档

- `渡劫录_GDD_完整版_v1.0.md`
- `渡劫录_Steam单机版差异化策略.md`
- `渡劫录_30天独立开发计划.md`

## MCP

- 本项目使用 MCP for Unity：`com.coplaydev.unity-mcp`。
- 首次打开 Unity 后等待 Package Manager 导入插件。
- 在 Unity 菜单中启动 MCP for Unity 后，Codex 会通过 `UnityMCP` 连接当前项目。
- 本机 `uvx` 启动会遇到 Windows PE trampoline 权限问题，项目已提供 `Tools/MCP/mcp-for-unity-uvx-shim.cmd` 转发到 `.mcp-server` 中的本地 server。
