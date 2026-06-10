# SurfWeb 前端

Vue 3 + TypeScript + Vite + Tailwind CSS 4，UI 为极简高对比（方向 C）。

## 开发

```powershell
npm install
npm run dev
```

默认开发地址：<http://localhost:5173>

## 环境变量

| 文件 | 何时使用 |
|------|----------|
| `.env.development` | `npm run dev`（已提交，指向本地 API :5240） |
| `.env.production` | `npm run build`（**模式 A 构建发布**；从 `.env.production.example` 复制，一般不提交） |
| （无） | **Docker full** 模式在 `Build/docker/web.full.dockerfile` 内设置 `VITE_API_BASE_URL=/api/v1` |

开发示例：

```env
VITE_API_BASE_URL=http://localhost:5240/api/v1
VITE_SITE_TITLE=地满滑翔
```

生产构建发布（同域 Nginx 反代）：

```env
VITE_API_BASE_URL=/api/v1
VITE_SITE_TITLE=地满滑翔
```

`VITE_SITE_TITLE`：顶栏、页脚与浏览器标签标题（`{标题} · Surf Record`）；省略时默认「地满滑翔」。

须先在本机启动后端 API（见仓库根目录 `README.md`）。两种生产发布方式见 **`doc/deploy.md`**。

## 构建

```powershell
Copy-Item .env.production.example .env.production   # 首次构建发布时
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
