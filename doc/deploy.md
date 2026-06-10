# SurfWeb 生产发布说明

**日期：** 2026-06-05  
**适用范围：** 将 SurfWeb 部署到生产环境（非本地 `dotnet run` / `npm run dev`）

本项目**同时支持**两种发布方式，按场景任选其一，**不要混用同一台机器上的两套进程**（避免端口冲突）。

| | 模式 A：构建发布（Build） | 模式 B：Docker |
|--|---------------------------|----------------|
| **适合** | 已有 Nginx/systemd、不想装 Docker、要精细控制主机目录 | 快速一致环境、开源一键部署、多机复现 |
| **前端** | `npm run build` → `Web/dist/` | 镜像内 Nginx + `dist` |
| **后端** | `dotnet publish` → 目录 + `dotnet SurfWeb.Api.dll` | 镜像内 Kestrel |
| **配置** | **`Build/.env`**（同步 Web/.env.production；API 用 appsettings 或进程环境变量） | **`Build/.env`**（compose + 同上） |
| **入口** | `.\Build\surf.ps1 host` | `.\Build\surf.ps1` |
| **详细文档** | 本文 §2、[`Build/README.md`](../Build/README.md) | [`Build/README.md`](../Build/README.md) |

```
                    ┌─────────────────────────────────────┐
  浏览器 ──────────►│  Nginx（80/443）                     │
                    │    /      → 静态 dist                │
                    │    /api/  → Kestrel :5240           │
                    └─────────────────────────────────────┘
         模式 A：Nginx 在宿主机，API 进程在宿主机
         模式 B：Nginx 在 web 容器，API 在 api 容器（对外只暴露 WEB_PORT）
```

---

## 1. 发布前共同准备

无论哪种模式，都需要：

1. **Shavit 只读库**连接串（RDS 白名单、账号权限）  
2. **`SurfWeb` 业务配置**：地图图床 `MapImages`、游戏服 `Servers[]`、可选 `ExternalApi:LatestRecordsToken`  
3. **勿将真实密码提交 Git**；生产密钥用环境变量或服务器上的本地 JSON  

配置键说明见 `doc/design.md` §8.1。

---

## 2. 模式 A：构建发布（`dotnet publish` + `npm run build`）

在构建机或服务器上安装 **.NET 10 SDK（构建）/ Runtime（仅运行）**、**Node.js 20+**；生产环境另需 **Nginx**（或其它反向代理）。

**一键（仓库根目录）：** `.\Build\surf.ps1` → 菜单选 **宿主机**，或 `.\Build\surf.ps1 host`。

### 2.1 后端

产物在 `publish/api/`，入口为 `dotnet SurfWeb.Api.dll`。

**生产配置（三选一或组合）：**

| 方式 | 说明 |
|------|------|
| 环境变量 | `ConnectionStrings__Shavit`、`SurfWeb__*`（双下划线嵌套） |
| `appsettings.Production.json` | 放在发布目录，与 `SurfWeb.Api.dll` 同级 |
| systemd `Environment=` | Linux 服务单元中注入 |

示例 `publish/api/appsettings.Production.json`（勿提交含真实密码的文件）：

```json
{
  "ConnectionStrings": {
    "Shavit": "Server=YOUR_DB;Port=3306;Database=shavit;User=readonly;Password=***;SslMode=None;CharSet=utf8mb4"
  },
  "SurfWeb": {
    "MapImages": {
      "BaseUrl": "https://your-cdn.example.com/maps/",
      "Extension": ".jpg"
    },
    "ExternalApi": {
      "LatestRecordsToken": "your-token"
    },
    "Servers": [
      {
        "Name": "Your Server",
        "Address": "connect 1.2.3.4:27015",
        "Host": "1.2.3.4",
        "Port": 27015,
        "MaxPlayers": 64
      }
    ]
  }
}
```

**运行 API：**

```powershell
cd publish/api
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5240"
dotnet SurfWeb.Api.dll
```

验证：`curl http://127.0.0.1:5240/health`

Linux 上建议用 **systemd** 托管上述命令（开机自启、`Restart=always`）。

### 2.2 前端

静态产物在 `Web/dist/`（`surf.ps1 host` 会按 `Web/.env.production` 构建；首次从 `.env.production.example` 生成）。

**说明：** `npm run build` 会读取 `Web/.env.production`（若存在）。开发用的 `.env.development` **不会**参与生产构建。

### 2.3 宿主机 Nginx（模式 A）

将 `Web/dist` 拷到服务器后，参考 **`Build/host/nginx.example.conf`**（API 反代到本机 `127.0.0.1:5240`）。

```bash
sudo cp Build/host/nginx.example.conf /etc/nginx/sites-available/surfweb
# 修改 root、server_name
sudo ln -s /etc/nginx/sites-available/surfweb /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

浏览器访问 Nginx 的 80/443；`/api/v1/...` 由 Nginx 转发到 Kestrel。

### 2.4 模式 A 检查清单

- [ ] `curl http://127.0.0.1:5240/health` 为 Healthy  
- [ ] `curl http://你的域名/health` 为 Healthy  
- [ ] 首页可打开，Network 中 API 为 `/api/v1/...` 且 200  
- [ ] `ASPNETCORE_ENVIRONMENT=Production`（无 Swagger）  

---

## 3. 模式 B：Docker

```powershell
.\Build\surf.ps1   # 菜单选 Docker；首次会自动处理 .env
```

默认 **http://localhost:8080**。推荐默认 **prebuilt**（本机编译）；仅 Docker 无 SDK 时用 `.\Build\surf.ps1 docker -FullImage`。

详见 [`Build/README.md`](../Build/README.md)。

---

## 4. 两种模式如何选？

| 场景 | 建议 |
|------|------|
| 个人 VPS、希望 `git clone` + 一条命令 | **Docker** |
| 公司已有 Nginx + systemd 规范、不能装 Docker | **Build** |
| CI 构建产物上传到多台机器 | **Build**（`publish/` + `dist/`）或 Docker 镜像 `save`/`pull` |
| 本地验证生产包 | **Build**（`dotnet publish` + `npm run build` + 本机 Nginx） |

---

## 5. 配置文件对照

| 用途 | 模式 A（Build） | 模式 B（Docker） |
|------|-----------------|------------------|
| API 地址（前端构建） | `Build/.env` → 同步 `Web/.env.production` | 同上 |
| 站点名 | `Build/.env` 的 `VITE_SITE_TITLE` | 同上 |
| 数据库 / SurfWeb | `Build/.env` 的 `DATABASE_PROVIDER`、`SHAVIT_CONNECTION_STRING` → 宿主机 API 环境变量 | 同上 → compose |
| 开发本地 API | `Web/.env.development` | 不使用（dev 仍 `npm run dev`） |
| 反向代理 | `Build/host/nginx.example.conf` | `Build/docker/nginx.conf` |

---

## 7. Windows 宝塔面板部署（模式 A 变体）

宝塔 **Windows 版** 的「**.NET 项目**」走 **IIS + ASP.NET Core Module**（界面提示「只支持 IIS」）。SurfWeb 需同时部署 **API（.NET）** 与 **前端（静态 `dist`）**，推荐 **一个域名 + 宝塔反向代理**，与 `Build/host/nginx.example.conf` 思路一致。

### 7.1 前置安装

在宝塔 **软件商店** 或微软官网安装：

| 组件 | 用途 |
|------|------|
| **IIS**（宝塔网站功能依赖） | 托管 .NET API |
| **[.NET 10 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0)** | IIS 反代 Kestrel 必需；装后 **重启 IIS** |
| **Nginx**（宝塔可选，用于对外 80/443 + 反代） | 同一域名下静态站 + `/api` |
| 本机构建用 **.NET 10 SDK**、**Node.js 20+** | `publish` 与 `npm run build`（可在开发机 build 后上传） |

### 7.2 构建产物

在开发机或服务器上（路径示例 `D:\www\surfweb\`）：

```powershell
.\Build\surf.ps1 host
# 再将 publish\api\* -> D:\www\surfweb\api\
# 将 Web\dist\*     -> D:\www\surfweb\wwwroot\
```

在 `D:\www\surfweb\api\` 放置 **`appsettings.Production.json`**（数据库、图床、Servers、token 等，勿提交 Git）。或在 IIS 站点里配置环境变量。

### 7.3 宝塔添加 .NET API 站点（IIS）

1. **网站** → **.NET 项目** → **添加站点**  
2. **域名**：填内网用主机名即可，如 `surfweb-api.local` 或 `127.0.0.1:5240` 绑定方式（不同宝塔版本字段略有差异；核心是 IIS 站点指向 publish 目录）  
3. **根目录 / 项目路径**：`D:\www\surfweb\api`（含 `SurfWeb.Api.dll`、`web.config`）  
4. **.NET 版本**：选已安装的 **.NET 10** / **无托管代码** 应用程序池（按面板选项选 ASP.NET Core 对应项）  
5. 保存后访问 `http://绑定地址/health`，应返回 **Healthy**

`dotnet publish` 一般会生成 `web.config`，内容类似将请求交给 AspNetCoreModuleV2；若 502.5，多为未装 Hosting Bundle 或未重启 IIS。

**API 仅本机访问**：IIS 绑定 `127.0.0.1:5240` 或防火墙只放行本机，对外只通过 Nginx 反代。

### 7.4 对外站点：静态前端 + 反向代理

**做法一（推荐）：Nginx 站点 + 反代**

1. **网站** → **HTML 项目** 或 **PHP 项目**（仅用静态能力）→ 添加站点  
2. **域名**：你的公网域名，如 `surf.example.com`  
3. **根目录**：`D:\www\surfweb\wwwroot`（`dist` 内容）  
4. 站点 **设置** → **反向代理**（或 **配置文件**）增加：

| 代理目录 | 目标 URL |
|----------|----------|
| `/api` | `http://127.0.0.1:5240`（或 IIS API 站点地址） |
| `/hubs` | 同上（需 WebSocket 支持） |
| `/health` | 同上 |

Nginx 配置要点（与 `Build/docker/nginx.conf`、`Build/host/nginx.example.conf` 类似）：

```nginx
location / {
    try_files $uri $uri/ /index.html;
}
location /api/ {
    proxy_pass http://127.0.0.1:5240/api/;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
location /hubs/ {
    proxy_pass http://127.0.0.1:5240/hubs/;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
}
```

5. 宝塔 **SSL** → 申请证书并开启 HTTPS  

**做法二：仅 IIS 两个站点**  
前端 IIS 静态站点 + API 单独 IIS 站点；前端需 URL Rewrite 将 `/api` 转到 API 站点，配置比 Nginx 反代繁琐，一般不优先。

### 7.5 验证

- `https://你的域名/` 首页正常  
- `https://你的域名/health` → Healthy  
- 浏览器 Network：`/api/v1/...` 为 200  
- `ASPNETCORE_ENVIRONMENT=Production`（无 `/swagger`）

### 7.6 常见问题

| 现象 | 处理 |
|------|------|
| 502.5 / 503 | 安装 **Hosting Bundle**、重启 IIS、确认应用程序池为「无托管代码」 |
| 前端 API 404 | 反代未配 `/api/` 或 `VITE_API_BASE_URL` 不是 `/api/v1` |
| 数据库连不上 | `appsettings.Production.json` 勿用 `127.0.0.1` 指 RDS；检查 RDS 白名单为 **服务器公网 IP** |
| WebSocket 失败 | 反代需 `Upgrade` / `Connection` 头（见上） |

### 7.7 与 Docker 的关系

Windows 宝塔也可安装 Docker 后直接用 **`docker compose`**（见 `doc/docker.md`），与 IIS 二选一，**勿在同一端口混跑两套**。

---

## 8. 相关文档

- 产品与 API：`doc/design.md`  
- Docker / 构建：`Build/README.md`（`doc/docker.md` 为跳转）  
- 本地开发：仓库根目录 `README.md`  
- 前端环境变量：`Web/README.md`
