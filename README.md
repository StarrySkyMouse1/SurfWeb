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

| 模式 | 命令 | 文档 |
|------|------|------|
| **Docker**（推荐） | `.\Build\surf.ps1` | **`Build/README.md`** |
| **宿主机 Nginx** | `.\Build\surf.ps1 host` | **`doc/deploy.md`** |

```powershell
.\Build\surf.ps1   # 交互菜单：Docker 或宿主机，自动完成构建与启动
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
