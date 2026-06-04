# astrbot_plugin_surfweb_records

AstrBot 插件：连接 SurfWeb `RecordsHub`（`/hubs/records`），接收 `RecordsUpdated` / `RecentSnapshot` 推送，用 **WeasyPrint** 将 HTML 渲染为 PNG，并向已绑定会话发送图片。

## 安装（本机 / 非容器）

1. 将整个 `astrbot_plugin_surfweb_records` 目录复制到 AstrBot 的 `data/plugins/` 下。
2. 在 AstrBot WebUI 安装插件依赖，或在该目录执行：

```bash
pip install -r requirements.txt
```

### WeasyPrint（Windows 本机）

WeasyPrint 依赖 Pango/Cairo。Windows 上若导入失败，请参考官方说明安装 GTK3 运行时：

https://doc.courtbouillon.org/weasyprint/stable/first_steps.html#installation

---

## 报 `ModuleNotFoundError: No module named 'listener'`？

AstrBot 以 `data.plugins.<插件名>.main` 包路径加载插件，不能写裸的 `from listener import`。

请更新为含 `_plugin_path.py`、`__init__.py` 的最新版插件文件，然后 **重载插件** 或重启容器。

---

## AstrBot 报「pysignalr / websockets 版本冲突」？

若日志类似：

`pysignalr 1.3.2 depends on websockets<17 and >=16.0 vs websockets==15.0.1`

请把 `requirements.txt` 里的 **`pysignalr` 固定为 `1.3.0`**（不要用 1.3.1+）。1.3.0 与 AstrBot 自带的 `websockets==15.0.1` 兼容。

更新插件文件后，在容器内：

```bash
pip install "pysignalr==1.3.0" -r .../requirements.txt
docker restart astrbot
```

---

## 放进 `data/plugins` 后 WebUI 里看不到？

按下面逐项检查（你当前路径类似 `C:\Dockers\astrbot\data\plugins\astrbot_plugin_surfweb_records`）：

1. **文件夹里必须有这些文件**（不能只建空目录）  
   打开 `astrbot_plugin_surfweb_records`，应能看到至少：`main.py`、`metadata.yaml`、`listener.py`、`renderer.py`、`requirements.txt`、`templates/records_card.html`。  
   若只有空文件夹，请从 SurfWeb 仓库 `Python/astrbot_plugin_surfweb_records/` **整夹复制**过来。

2. **重启 AstrBot 容器**（只复制文件往往不会热加载新插件）  
   ```bash
   docker restart astrbot
   ```
   或在 Docker Desktop 里重启对应容器。

3. **确认卷挂载**  
   宿主机 `C:\Dockers\astrbot\data\plugins` 必须映射到容器内的 `.../data/plugins`。  
   在容器里确认能看到插件：  
   ```bash
   docker exec astrbot ls -la /AstrBot/data/plugins/astrbot_plugin_surfweb_records
   ```
   （若容器内路径不是 `/AstrBot`，以你 compose 里 `data` 挂载为准。）

4. **看是否「加载失败」**  
   WebUI → 插件页，向下滚动或看是否有红色/失败项；点 **重载插件**。  
   看容器日志：  
   ```bash
   docker logs astrbot --tail 100
   ```
   搜索 `surfweb_records`、`ModuleNotFoundError`、`weasyprint`。

5. **在容器内手动装依赖**（自动安装失败时很常见）  
   ```bash
   docker exec -it astrbot bash
   pip install -r /AstrBot/data/plugins/astrbot_plugin_surfweb_records/requirements.txt
   ```
   WeasyPrint 还需系统库，见下文 Docker 章节 `apt-get install`。

6. **改用 ZIP 安装（与放目录二选一）**  
   WebUI「安装插件 → 从文件安装」只接受 **.zip**：  
   - 将 `astrbot_plugin_surfweb_records` 文件夹打成 zip（zip 根目录里直接是 `main.py` 等，不要多套一层父目录）。  
   - 上传后安装，再在插件列表里启用。

---

## 安装（Docker / 容器版 AstrBot）

容器里 **`127.0.0.1` 指向容器自身**，不是宿主机。SurfWeb 若在宿主机跑，必须把插件里的 `hub_url` 改成能访问宿主机的地址（见下文）。

### 1. 挂载插件目录

把本仓库中的插件挂到 AstrBot 的 `data/plugins`（路径以你实际镜像为准，常见为 `/AstrBot/data/plugins`）：

**docker run 示例：**

```bash
docker run -d --name astrbot \
  -v D:/AstrBot/data:/AstrBot/data \
  -v C:/Projects/else/.net/SurfWeb/Python/astrbot_plugin_surfweb_records:/AstrBot/data/plugins/astrbot_plugin_surfweb_records \
  ...你的其它参数... \
  <astrbot镜像>
```

**docker compose 示例：**

```yaml
services:
  astrbot:
  image: <你的 astrbot 镜像>
  volumes:
    - ./data:/AstrBot/data
    - C:/Projects/else/.net/SurfWeb/Python/astrbot_plugin_surfweb_records:/AstrBot/data/plugins/astrbot_plugin_surfweb_records
```

改代码后可在 WebUI **重载插件**，无需重建镜像（依赖已装好时）。

### 2. 在容器内安装系统库 + Python 依赖

WeasyPrint 需要 Pango/Cairo。在容器里执行（镜像基于 Debian/Ubuntu 时）：

```bash
docker exec -it astrbot bash

apt-get update
apt-get install -y --no-install-recommends \
  libpango-1.0-0 libpangocairo-1.0-0 libcairo2 libgdk-pixbuf-2.0-0 \
  libffi-dev shared-mime-info fonts-noto-cjk

pip install -r /AstrBot/data/plugins/astrbot_plugin_surfweb_records/requirements.txt
exit
```

若镜像基于 **Alpine**，需改用 musl 对应包或换用 Debian 系 AstrBot 镜像；WeasyPrint 在 Alpine 上配置较麻烦。

> 容器重建后 `apt` / `pip` 会丢失，除非写入自定义 Dockerfile 或启动脚本。

**自定义 Dockerfile 片段（可选，避免每次 exec）：**

```dockerfile
RUN apt-get update && apt-get install -y --no-install-recommends \
    libpango-1.0-0 libpangocairo-1.0-0 libcairo2 libgdk-pixbuf-2.0-0 \
    libffi-dev shared-mime-info fonts-noto-cjk \
 && rm -rf /var/lib/apt/lists/*
COPY astrbot_plugin_surfweb_records /AstrBot/data/plugins/astrbot_plugin_surfweb_records
RUN pip install -r /AstrBot/data/plugins/astrbot_plugin_surfweb_records/requirements.txt
```

### 3. 配置 `hub_url`（容器 → SurfWeb）

| SurfWeb 运行位置 | 插件 `hub_url` 建议 |
|------------------|---------------------|
| 宿主机 Windows/Mac（Docker Desktop） | `http://host.docker.internal:5240/hubs/records` |
| 宿主机 Linux | `http://172.17.0.1:5240/hubs/records` 或 compose 里 `extra_hosts: host.docker.internal:host-gateway` 后用 `host.docker.internal` |
| 同一 compose 里的服务名 `surfweb` | `http://surfweb:5240/hubs/records` |

在 AstrBot WebUI 插件配置中修改，或编辑 `data/config/astrbot_plugin_surfweb_records_config.json`。

SurfWeb API 需监听 `0.0.0.0`（不要只绑 `localhost`），否则容器访问不到宿主机端口。开发时可在 `launchSettings` 使用 `http://0.0.0.0:5240`，或：

```bash
dotnet run --urls http://0.0.0.0:5240
```

### 4. 启用与验证

1. WebUI 启用 **astrbot_plugin_surfweb_records** → **重载插件**。
2. 群内 `/surfweb status`，应显示 **连接: 是**。
3. `/surfweb bind` 绑定推送群。
4. 若 `status` 为否：检查 `hub_url`、SurfWeb 是否运行、防火墙、API 是否 `0.0.0.0` 监听。

### 5. 容器联调 checklist

- [ ] 插件目录已 volume 挂载到 `data/plugins/astrbot_plugin_surfweb_records`
- [ ] 容器内已 `apt` + `pip install -r requirements.txt`
- [ ] `hub_url` 不是 `127.0.0.1`（除非 SurfWeb 也在同一容器网络内）
- [ ] SurfWeb 在宿主机时可从容器 `curl` 通：`docker exec astrbot curl -s -o /dev/null -w "%{http_code}" http://host.docker.internal:5240/health`
- [ ] 已 `/surfweb bind`

## 配置

在 AstrBot 插件管理面板中配置，或编辑 `data/config/<插件>_config.json`：

| 项 | 说明 |
|----|------|
| `hub_url` | 默认 `http://127.0.0.1:5240/hubs/records` |
| `scope` | `All` / `Main` / `Bonus` / `Stage` |
| `snapshot_page_size` | 订阅时首屏条数（1–50） |
| `push_targets` | 推送会话列表（也可用命令绑定） |
| `notify_on_update` | 收到新成绩时发图（默认 true） |
| `notify_on_snapshot` | 连接后首屏快照是否发图（默认 false） |

## 命令

- `/surfweb bind` — 当前会话加入推送列表
- `/surfweb unbind` — 移出推送列表
- `/surfweb status` — 连接与配置状态
- `/surfweb start` — 启动/重启 SignalR 监听
- `/surfweb preview` — 用最近一次快照预览图片

## 联调 SurfWeb

1. 启动 SurfWeb API（默认 `http://localhost:5240`）。
2. 在群内 `/surfweb bind`。
3. `/surfweb start`（插件加载时也会自动尝试连接）。
4. 开发环境可在 Swagger 调用 `POST /api/v1/realtime/push/trigger` 触发一轮推送（需游标已初始化且库中有新成绩）。

## SignalR 协议

- Hub：`RecordsHub`
- 订阅：`SubscribeRecent(scope, snapshotPageSize)`
- 事件：`RecordsUpdated`、`RecentSnapshot`

与 `Server/SurfWeb.Realtime/Hubs/RecordsHub.cs` 保持一致。
