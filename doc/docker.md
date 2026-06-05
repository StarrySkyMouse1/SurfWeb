# SurfWeb Docker 部署说明

**日期：** 2026-06-05  
**适用范围：** 仓库根目录 `docker-compose.yml` 编排的前后端容器化部署  

> **发布方式选型：** 本项目支持 **Docker** 与 **构建发布（`dotnet publish` + `npm run build`）** 两种模式，对照表见 [`doc/deploy.md`](deploy.md)。本文仅描述 Docker 模式。

---

## 1. 概述

SurfWeb 使用 **两个容器** 部署，浏览器只访问 `web` 服务：

| 服务 | 镜像构建 | 对外 | 职责 |
|------|----------|------|------|
| `web` | `Web/Dockerfile` | `WEB_PORT`（默认 8080） | Nginx 托管 Vue 静态资源；反代 `/api`、`/hubs`、`/health` |
| `api` | `Server/SurfWeb.Api/Dockerfile` | 仅容器内 5240 | .NET 10 Kestrel API + SignalR |

前端构建时 `VITE_API_BASE_URL=/api/v1`，与 Nginx 同域反代，**无需**额外配置 CORS。

```
浏览器 ──► web:80 (Nginx)
              ├── /           → Web/dist 静态文件
              ├── /api/       → api:5240/api/
              ├── /hubs/      → api:5240/hubs/  (WebSocket)
              └── /health     → api:5240/health
```

---

## 2. 前置条件

- 已安装 [Docker](https://docs.docker.com/get-docker/) 与 Docker Compose（Docker Desktop 自带）
- 可访问的 Shavit MySQL 只读库（连接串、白名单）
- 仓库根目录执行以下命令

---

## 3. 快速开始

```powershell
cd <仓库根目录>

# 1. 创建环境变量文件（勿提交 Git）
Copy-Item .env.docker.example .env
# 编辑 .env，至少填写 SHAVIT_CONNECTION_STRING

# 2. 构建镜像并后台启动容器
docker compose up -d --build

# 3. 验证
curl http://localhost:8080/health
```

浏览器访问：**http://localhost:8080**

### `docker compose up -d --build` 做了什么？

同一条命令会顺序完成两件事：

1. **`--build`**：根据 `Dockerfile` 构建（或重建）`api`、`web` 镜像  
2. **`up -d`**：用镜像创建并启动容器；`-d` 表示后台运行

等价于：

```powershell
docker compose build
docker compose up -d
```

改代码后重新部署，一般仍用 `docker compose up -d --build`。

---

## 4. 相关文件

| 路径 | 说明 |
|------|------|
| `docker-compose.yml` | Compose 服务定义与环境变量映射 |
| `.env.docker.example` | 环境变量模板；复制为 `.env` 使用 |
| `.dockerignore` | 构建上下文排除项（`bin/`、`node_modules/` 等） |
| `Server/SurfWeb.Api/Dockerfile` | API 多阶段构建（sdk → aspnet） |
| `Web/Dockerfile` | 前端 `npm run build` + Nginx |
| `Web/nginx.conf` | SPA 路由与反向代理规则 |

---

## 5. 环境变量（`.env`）

Compose 通过 `.env` 注入配置。变量名与 ASP.NET Core 配置键的对应关系：双下划线 `__` 表示嵌套节。

### 5.1 必填

| 变量 | 映射到 | 说明 |
|------|--------|------|
| `SHAVIT_CONNECTION_STRING` | `ConnectionStrings:Shavit` | Shavit 只读库连接串 |

示例：

```env
SHAVIT_CONNECTION_STRING=Server=YOUR_DB_HOST;Port=3306;Database=shavit;User=readonly;Password=***;SslMode=None;CharSet=utf8mb4
```

**注意：** 容器内的 `127.0.0.1` 指向容器自身，不是宿主机。数据库在宿主机上时请用 `host.docker.internal` 或 RDS 公网/内网地址。

### 5.2 可选

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `WEB_PORT` | `8080` | 浏览器访问端口 |
| `LATEST_RECORDS_TOKEN` | 空 | `GET /api/v1/api/records/latest` 的 `token`；空则接口 401 |
| `MAP_IMAGES_BASE_URL` | 示例图床 URL | 地图缩略图前缀 |
| `MAP_IMAGES_EXTENSION` | `.jpg` | 地图图扩展名 |
| `SURF_SERVER_NAME` | `Surf Server` | 服务器展示名 |
| `SURF_SERVER_ADDRESS` | `connect host.docker.internal:27015` | 连接字符串展示 |
| `SURF_SERVER_HOST` | `host.docker.internal` | A2S 查询主机 |
| `SURF_SERVER_PORT` | `27015` | A2S 查询端口 |
| `SURF_SERVER_MAX_PLAYERS` | `64` | 占位最大人数 |

### 5.3 复杂配置（挂载 JSON）

`SurfWeb:Styles`、`SurfWeb:Servers[]` 多项、缓存秒数等，建议在宿主机准备 `appsettings.Production.json`，挂载到 API 容器：

```yaml
# docker-compose.yml 的 api 服务下增加：
volumes:
  - ./deploy/appsettings.Production.json:/app/appsettings.Production.json:ro
```

环境变量与 JSON 同时存在时，**环境变量优先**。

---

## 6. 网络与访问宿主机服务

### 6.1 数据库

- **云 RDS：** 使用 RDS 地址；安全组/白名单放行**运行 Docker 的机器**出口 IP。  
- **宿主机 MySQL：** 连接串主机填 `host.docker.internal`（Windows / macOS Docker Desktop；Linux 已在 compose 配置 `extra_hosts: host-gateway`）。

### 6.2 游戏服 A2S（Steam 查询）

默认通过 `host.docker.internal` 查询宿主机上的 SRCDS。要求：

- 容器能访问游戏服 UDP 端口（出网/firewall）
- 游戏服监听地址允许来自 Docker 网的访问

游戏服在另一台机器时，将 `SURF_SERVER_HOST` 改为该机器 IP 或域名。

---

## 7. 常用命令

```powershell
# 查看运行状态
docker compose ps

# 查看日志
docker compose logs -f
docker compose logs -f api
docker compose logs -f web

# 停止并删除容器（镜像保留）
docker compose down

# 停止并删除容器、网络、本 compose 构建的镜像
docker compose down --rmi local

# 仅构建镜像，不启动
docker compose build

# 仅启动（镜像已存在时）
docker compose up -d
```

### 单独构建镜像（不通过 Compose）

```powershell
docker build -f Server/SurfWeb.Api/Dockerfile -t surfweb-api .
docker build -f Web/Dockerfile -t surfweb-web .
```

---

## 8. 健康检查与排错

| 检查项 | 命令 / 地址 | 期望 |
|--------|-------------|------|
| 站点健康 | `http://localhost:8080/health` | HTTP 200，`Healthy` |
| 前端页面 | `http://localhost:8080/` | 首页可加载 |
| API 反代 | 浏览器 Network → `/api/v1/...` | 200，非 CORS 错误 |

**API 启动失败常见原因：**

1. `SHAVIT_CONNECTION_STRING` 未设置或数据库不可达  
2. 连接串使用 `127.0.0.1` 但库在宿主机/云上  
3. RDS 白名单未包含服务器 IP  

**查看 API 日志：**

```powershell
docker compose logs api --tail 100
```

**进入容器调试：**

```powershell
docker compose exec api sh
# 容器内
curl -s http://127.0.0.1:5240/health
```

---

## 9. 生产环境说明

- `ASPNETCORE_ENVIRONMENT=Production`：不启用 Swagger（`/swagger` 不可用）。  
- 勿将真实密码、token 写入仓库；`.env` 应加入 `.gitignore`（模板仅用 `.env.docker.example`）。  
- 对外 HTTPS：在 `web` 前再加一层反向代理（Nginx、Caddy、云 LB），或改 `web` 的 Nginx 配置挂载 TLS 证书。  
- `restart: unless-stopped` 已在 compose 中配置，宿主机重启后容器会自动拉起。

---

## 10. 与本地开发的区别

| 项目 | 本地开发 | Docker |
|------|----------|--------|
| 前端 | `npm run dev` :5173 | Nginx 静态 `dist` |
| 后端 | `dotnet run` :5240 | Kestrel 容器内 :5240 |
| API 地址 | `http://localhost:5240/api/v1` | 浏览器侧 `/api/v1`（同域） |
| Swagger | Development 可用 | Production 关闭 |
| 配置 | User Secrets / `appsettings.*.local.json` | `.env` + 可选挂载 JSON |

本地开发说明见仓库根目录 `README.md`；产品与 API 契约见 `doc/design.md`。

---

## 11. 部署到 Linux 服务器

将 SurfWeb 跑到 Linux 上，常见有 **三种方式**。开源项目**默认推荐方式 A**，无需自建镜像仓库。

### 方式 A：在服务器上 clone 后直接构建（推荐）

把仓库拉到 Linux，在机器上现场 `build` 并启动：

```bash
git clone https://github.com/<你的组织>/SurfWeb.git
cd SurfWeb

cp .env.docker.example .env
# 编辑 .env，至少填写 SHAVIT_CONNECTION_STRING

docker compose up -d --build
```

- 镜像在**服务器本机**构建，无需事先在本机打包上传  
- 要求服务器能访问外网（拉取 `mcr.microsoft.com/dotnet/*`、`node`、`nginx` 等基础镜像）  
- 配置写在服务器上的 `.env`，**不要**把真实密码提交到 Git  

验证：`curl http://localhost:8080/health`，浏览器访问 `http://<服务器IP>:8080`（安全组需放行 `WEB_PORT`）。

### 方式 B：本机构建 → 导出镜像文件 → 上传到 Linux

适用于服务器**不能访问外网**或希望在 CI/本机统一构建的场景。

**本机（Windows / macOS / Linux）：**

```powershell
docker compose build

# 查看镜像名（多为 <项目目录名>-api-1、<项目目录名>-web-1 或 compose 定义的 name）
docker images

# 导出为 tar（按实际镜像名替换）
docker save surfweb-api surfweb-web -o surfweb-images.tar
```

**Linux 服务器：**

```bash
docker load -i surfweb-images.tar

# 还需上传：docker-compose.yml、.env（及可选 appsettings.Production.json）
docker compose up -d    # 已有镜像，一般不必再加 --build
```

**说明：**

- `docker compose build` **不会**自动生成 `.tar` 文件；镜像留在本机 Docker 中，上传文件需用 **`docker save`**，对端用 **`docker load`**  
- 仅有镜像不够，还必须带 **`docker-compose.yml` + `.env`**，否则无法知道如何启动、连哪个数据库  

### 方式 C：推送到镜像仓库（可选，非开源必需）

适合多台机器、CI 自动发布，或希望用户 `docker pull` 即可、无需在服务器上编译。

```
本机/CI：build → tag → push → 公共/私有镜像仓库（Docker Hub、GHCR、阿里云 ACR 等）
Linux：pull → docker compose up -d（compose 中改用 image: 而非 build:）
```

开源小项目**可以不提供**预构建镜像，只维护源码 + `doc/docker.md` 即可。

### 上传到 Linux 最少要带什么？

| 内容 | 方式 A（服务器 build） | 方式 B（导入镜像） |
|------|------------------------|---------------------|
| 完整源码（含 Dockerfile） | 是 | 否（除非要在服务器改 build） |
| `docker-compose.yml` | 是 | 是 |
| `.env` | 是 | 是 |
| `api` / `web` 镜像（tar 或 pull） | 否（现场构建） | 是 |

---

## 12. 镜像仓库是什么？

| 概念 | 存放内容 | 典型用途 |
|------|----------|----------|
| **Git 仓库**（GitHub 等） | 源代码 | `git clone` |
| **镜像仓库**（Docker Hub、GHCR、阿里云 ACR 等） | 已 build 好的 Docker 镜像 | `docker pull` / `docker push` |

镜像仓库相当于「镜像的网盘 + 下载站」。本项目的 `Dockerfile` 里引用的 `mcr.microsoft.com/dotnet/aspnet:10.0`、`node:22-alpine` 等，就是从 **Microsoft / Docker 官方公共镜像仓库**拉取的**基础镜像**。

SurfWeb 自己的 `api`、`web` 镜像：

- **不强制**发布到镜像仓库；用户 `git clone` 后 `docker compose up -d --build` 即可  
- 若维护者希望提供「一键 pull」，可在 CI 中 build 并 push 到 Docker Hub 等，属于**可选增强**

---

## 13. 开源项目：拉代码就能跑容器吗？

**可以**，但需理解「拉代码」≠「自动变容器」，还要执行 Compose：

1. `git clone` 得到源码与 `Dockerfile`、`docker-compose.yml`  
2. 复制 `.env.docker.example` → `.env`，填入数据库等**本机/本服务器**配置  
3. 执行 `docker compose up -d --build`，由 Docker 完成构建与启动  

仓库里的配置模板（`.env.docker.example`）会进 Git；**真实连接串、token 在 `.env`，每人自行配置，不提交仓库**。

其他贡献者或服主按同一套文档操作即可，与维护者是否在 Windows 上开发无关（见 §14）。

---

## 14. 跨平台：Linux 与 Windows 是否通用？

本项目使用的是 **Linux 容器**（`dotnet/aspnet`、`nginx:alpine`、`node:alpine`），**不是** Windows 容器。

| 运行环境 | 能否使用本 compose |
|----------|-------------------|
| **Linux 服务器**（常见 VPS / 云主机） | 可以，最标准 |
| **Windows + Docker Desktop**（默认 **Linux 容器**模式） | 可以；实际通过 WSL2/虚拟机跑 Linux 容器 |
| 未安装 Docker 的裸 Windows | 不可以，需先装 Docker Desktop |
| 仅支持 **Windows 容器**的主机 | 不可以（与本项目镜像类型不匹配） |

### 本机构建、Linux 部署

在 **Windows Docker Desktop（Linux 模式）** 上 `docker compose build` 得到的镜像，经 `docker save` / `docker load` 或推送到仓库后，在 **Linux x64** 服务器上运行，**通常可以直接使用**。

### CPU 架构需注意

| 构建机器 | 部署机器 | 说明 |
|----------|----------|------|
| x64 Windows → x64 Linux | 一般无问题 | 最常见 |
| Apple M 芯片（arm64）→ x64 Linux | 需指定平台 | 见下 |

在 ARM Mac 上为 x64 Linux 构建时：

```bash
docker compose build --platform linux/amd64
```

或在 `docker-compose.yml` 各服务下增加 `platform: linux/amd64`。

### 小结

- 同一套 `docker-compose.yml` 适用于 **Linux 生产环境** 与 **Windows 上的 Docker Desktop 开发/试跑**  
- 镜像与路径、换行符无关；差异主要在 **`.env` 中的数据库地址、端口、防火墙**  
- 开源用户典型路径：**Linux VPS → `git clone` → 配 `.env` → `docker compose up -d --build`**
