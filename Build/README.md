# SurfWeb 构建与发布

**唯一入口脚本：** `.\Build\surf.ps1`（在仓库根目录执行；**无参数时弹出菜单**，选 Docker 或宿主机后自动完成）

## 目录

```
Build/
  surf.ps1                 # 统一入口
  README.md                # 本说明
  env.example              # 复制为 .env（Docker / 宿主机共用）
  .env                     # 本地配置（勿提交 Git）
  docker/
    compose.yml
    nginx.conf
    *.dockerfile
  host/
    nginx.example.conf
```

产物目录（仍在仓库根，不入 Git）：`publish/api/`、`Web/dist/`。

**配置：** `Build/.env` 为 **Docker 与宿主机共用**；脚本会同步 `Web/.env.production`、宿主机 API 环境变量。

---

## 快速开始

```powershell
.\Build\surf.ps1
```

交互菜单：

1. **Docker** 或 **宿主机** 均会先检查 **`Build/.env`**
2. 再编译、部署（Docker 另选编译方式）

首次直接运行即可；若无 `Build/.env` 会自动从 `env.example` 复制并打开记事本，填好保存后按 Enter 继续：

```powershell
.\Build\surf.ps1
```

部署配置**仅**使用 `Build/.env`（勿在仓库根放置 `.env`）。

### 非交互（脚本 / CI）

| 命令 | 结果 |
|------|------|
| `.\Build\surf.ps1 -NonInteractive` | Docker 标准（等同选 1 → 1） |
| `.\Build\surf.ps1 docker` | Docker 标准 |
| `.\Build\surf.ps1 docker -FullImage` | Docker 容器内编译 |
| `.\Build\surf.ps1 docker -SkipBuild` | Docker 仅重建镜像 |
| `.\Build\surf.ps1 host` | 宿主机编译 + 启动 API |
| `.\Build\surf.ps1 host -NoStartApi` | 宿主机仅编译，不启 API |

宿主机 Nginx / 宝塔细节见 [`doc/deploy.md`](../doc/deploy.md)。

---

## 架构（Docker 运行时）

- 浏览器 → **web**（Nginx `:8080`）→ 静态 `dist`；`/api/*` 反代到 **api**（Kestrel `:5240`）
- 配置：**`Build/.env`**（模板 `Build/env.example`）

| 变量 | 必填 | 说明 |
|------|------|------|
| `DATABASE_PROVIDER` | 否 | `MySql`（默认）或 `Sqlite` |
| `SHAVIT_CONNECTION_STRING` | 是 | 数据库连接串（MySql 连接串或 Sqlite `Data Source=…`） |
| `SURFWEB_BUILD_MODE` | 否 | `prebuilt`（默认）或 `full` |
| `WEB_PORT` | 否 | 默认 `8080` |
| `VITE_SITE_TITLE` | 否 | 站点名（同步到 Web/.env.production；full 镜像构建亦用） |

容器内 `127.0.0.1` 不是宿主机；库在宿主机用 `host.docker.internal`。

---

## 常用命令

```powershell
.\Build\surf.ps1                    # 改代码后重新部署（Docker prebuilt）
.\Build\surf.ps1 docker -SkipBuild  # 只重建镜像

docker compose --project-directory . -f Build/docker/compose.yml --env-file Build/.env ps
docker compose --project-directory . -f Build/docker/compose.yml --env-file Build/.env logs -f api
docker compose --project-directory . -f Build/docker/compose.yml --env-file Build/.env down
```

Compose 文件唯一位置：`Build/docker/compose.yml`（勿在仓库根再放 `docker-compose.yml`）。

---

## 拉镜像慢 / 失败

`web` 默认经 **DaoCloud** 拉 `nginx` / `node`（见 `Build/env.example` 的 `NGINX_IMAGE`、`NODE_IMAGE`），不走 Docker Hub。`api` 基础镜像为 `mcr.microsoft.com`。

仍超时时可试：

1. Docker Desktop → **Settings → Proxies** → HTTP/HTTPS `http://127.0.0.1:7897` → **Apply & Restart**
2. 在 `Build/.env` 改回 Hub 或其它镜像源，例如 `nginx:1.27-alpine`

本机已 `publish` + `dist` 时可用 `.\Build\surf.ps1 docker -SkipBuild` 跳过编译，但仍需能拉到 nginx 基础镜像。

---

## 排错

| 现象 | 处理 |
|------|------|
| `Failed to construct 'URL'` | 旧版前端 bug；`.\Build\surf.ps1` 重建后 **Ctrl+F5** 强刷。可先测 API：`http://localhost:8080/api/v1/servers` 应返回 JSON |
| 标题乱码（鍦版弧…） | `Build/.env` 存 UTF-8；重新 `.\Build\surf.ps1` 会按 UTF-8 同步 `Web/.env.production` |
| `up 0/2`、拉镜像超时 | 配置 Docker Desktop 代理或镜像加速 |
| `/health` 非 Healthy | `docker compose logs api`；检查连接串 |
| 首页无数据 | 数据库地址/白名单；勿用 `127.0.0.1` 指远程 RDS |

---

设计文档中的发布说明：[`doc/design.md`](../doc/design.md) §8.4；宿主机宝塔等：[`doc/deploy.md`](../doc/deploy.md)。
