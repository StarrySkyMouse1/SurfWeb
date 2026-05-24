# SurfWeb 前端

Vue 3 + TypeScript + Vite + Tailwind CSS 4，UI 为极简高对比（方向 C）。

## 开发

```powershell
npm install
npm run dev
```

默认开发地址：<http://localhost:5173>

## 环境变量

复制 `.env.example` 为 `.env.development`（仓库已包含开发用配置）：

```env
VITE_API_BASE_URL=http://localhost:5240/api/v1
```

须先在本机启动后端 API（见仓库根目录 `README.md`）。

## 构建

```powershell
npm run build
```

产物在 `dist/` 目录。

## 目录说明

| 目录 | 说明 |
|------|------|
| `src/views/` | 页面：首页（含排行与最新记录）、地图、玩家 |
| `src/components/` | 公共组件：导航、地图卡片、排行榜表格等 |
| `src/api/client.ts` | 调用 `/api/v1` 的封装 |
| `src/router/` | Vue Router 路由 |

## Visual Studio

默认在 `SurfWeb.slnx` 中以**解决方案文件夹**浏览本目录（无需 `surfweb.esproj`）。

可选：安装 VS 工作负载「使用 JavaScript 和 TypeScript 进行开发」后，将 `surfweb.esproj` 加入解决方案，即可在 VS 内 F5 运行 `npm run dev`。

## 相关文档

- 设计文档：`../../doc/design.md`
- 实施计划：`../../docs/superpowers/plans/2026-05-22-surfweb.md`
