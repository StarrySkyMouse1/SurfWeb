# SurfWeb

SurfWeb 是一个面向 Surf 服务器的只读成绩查询站点，后端使用 .NET 10 Web API + EF Core，前端使用 Vue 3 + Vite + Tailwind CSS。

## 环境要求

- .NET 10 SDK
- Node.js 20+
- MySQL-compatible Shavit 数据库

## 快速开始

### 1. 配置数据库

推荐使用 User Secrets 保存数据库连接串，避免把密码提交到 Git：

```powershell
cd Server/SurfWeb.Api
dotnet user-secrets set "ConnectionStrings:Shavit" "Server=YOUR_DB_HOST;Port=3306;Database=shavit;User=readonly;Password=YOUR_PASSWORD;SslMode=None;CharSet=utf8mb4"
```

也可以复制本地配置模板：

```powershell
Copy-Item Server/SurfWeb.Api/appsettings.Development.local.json.example Server/SurfWeb.Api/appsettings.Development.local.json
```

`appsettings.Development.local.json` 已被 `.gitignore` 忽略，适合放本机数据库、地图图床和服务器地址。

### 2. 配置地图图床和服务器

公开的 `appsettings.json` 不包含真实服务器 IP、端口或 OSS 图床地址。请在 `Server/SurfWeb.Api/appsettings.Development.local.json` 中配置：

```json
{
  "SurfWeb": {
    "MapImages": {
      "BaseUrl": "https://example.com/surf-map-images/",
      "Extension": ".jpg"
    },
    "Servers": [
      {
        "Name": "Your Surf Server",
        "Address": "connect 127.0.0.1:27015",
        "Host": "127.0.0.1",
        "Port": 27015,
        "MaxPlayers": 64
      }
    ]
  }
}
```

### 3. 启动后端

```powershell
cd Server/SurfWeb.Api
dotnet run --launch-profile http
```

默认地址：<http://localhost:5240>

健康检查：<http://localhost:5240/health>

### 4. 启动前端

```powershell
cd Web
npm install
npm run dev
```

浏览器打开 <http://localhost:5173>。

## 生产发布

支持两种方式（不要在同一台机器混跑两套）：

| 模式 | 命令概要 | 文档 |
|------|----------|------|
| **构建发布** | `dotnet publish` + `npm run build` + 宿主机 Nginx | **`doc/deploy.md`** |
| **Docker** | `docker compose up -d --build` | **`doc/docker.md`** |

```powershell
# Docker 快速开始
Copy-Item .env.docker.example .env
docker compose up -d --build
```

```powershell
# 构建发布快速开始
dotnet publish Server/SurfWeb.Api -c Release -o publish/api
cd Web
Copy-Item .env.production.example .env.production
npm ci && npm run build
# 将 dist/ 与 publish/api 按 doc/deploy.md 配置 Nginx / systemd
```

## 验证

```powershell
dotnet test Server/SurfWeb.slnx
cd Web
npm run build
```

`SteamServerQueryLiveTests` 默认不访问真实服务器。需要测试 A2S 查询时，先设置：

```powershell
$env:SURFWEB_LIVE_STEAM_HOST="127.0.0.1"
$env:SURFWEB_LIVE_STEAM_PORT="27015"
```

## 文档

| 文档 | 路径 |
| --- | --- |
| 设计文档 | `doc/design.md` |
| 生产发布（Build / Docker） | `doc/deploy.md` |
| Docker 专题 | `doc/docker.md` |
| 前端说明 | `Web/README.md` |
| 实施计划 | `docs/superpowers/plans/2026-05-22-surfweb.md` |

修改代码时请同步更新 `doc/design.md`。
