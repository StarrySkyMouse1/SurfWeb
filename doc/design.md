# SurfWeb 设计文档

**日期：** 2026-05-22  
**状态：** 已批准，按实现持续同步  
**唯一设计文档：** 本仓库只维护这一份设计文档 `doc/design.md`

---

## 1. 产品目标

SurfWeb 是一个面向 Surf 服务器成绩查询的只读网站，目标是提供地图、玩家、排行榜、最新记录等核心查询能力，并用一套足够明显的架构形态来展示 DDD 与传统三层架构的差异。

当前版本的产品目标有两层：
- 面向用户：提供可直接使用的成绩查询站点。
- 面向学习：后端保留显著的 DDD 特征，方便对照聚合、值对象、领域事件、仓储、命令用例与查询侧的职责边界。

非目标：
- 不提供用户登录、后台管理、成绩人工提交。
- 不直接修改 Shavit 原库表结构。
- 不在 v1 中实现检查点榜（`cptimes` / `cpwrs`）。

---

## 2. 当前实现摘要

- 后端：.NET 10，强特征 DDD + CQRS 读写分离。
- 前端：Vue 3 + Vite + Tailwind，目录位于 `Web/`。
- 数据源：Shavit MySQL，只读查询。
- 本地 API 开发地址：`http://localhost:5240`，HTTPS 地址：`https://localhost:7182`。
- 前端默认 API 地址：`http://localhost:5240/api/v1`。

当前核心能力：
- 地图列表、地图详情、排行榜分页。
- 玩家摘要、玩家成绩、玩家完赛地图。
- 全站排行榜、最新记录。
- 配置接口：样式、地图图床；服务器实时状态（Steam A2S + 后台刷新）。
- 教学型命令入口：`POST /api/v1/admin/runs`。

---

## 3. 代码范围与目录

### 3.1 后端

- `Server/SurfWeb.Api`：HTTP 适配层与组合根。
- `Server/SurfWeb.Application`：用例编排，分 `Commands` 与 `Queries`。
- `Server/SurfWeb.Domain`：聚合根、值对象、领域事件、领域服务接口、聚合仓储接口。
- `Server/SurfWeb.Infrastructure`：EF Core、读侧仓储实现、写侧仓储实现、策略实现。

### 3.2 前端

- `Web/`：Vue 3 单页应用。
- 路由：`/`、`/maps`、`/maps/:name`、`/players/:auth`、`/servers`。
- 兼容旧链接：`/rankings`、`/records` 重定向到首页。

### 3.3 文档

- 设计文档：`doc/design.md`
- 实施计划：`docs/superpowers/plans/*.md`

---

## 4. 数据来源与业务语义

当前数据主要来自以下表：
- `users`：玩家资料、积分、游玩时长。
- `playertimes`：通关记录，包含 `style`、`track`、`map`、`time`、`date`。
- `stagetimes`：分段记录，包含 `stage`。
- `maptiers`：地图 tier 与速度信息。

业务约定：
- 默认样式来自 `SurfWeb:Styles` 中 `Default = true` 的项；当前默认是 `style = 0`。
- 通关类列表默认只看常规主线路径，不混入非默认 style。
- API 返回时间字段时，同时提供可格式化显示所需的信息。

---

## 5. API 设计

基础路径：`/api/v1`

### 5.1 读侧接口

- `GET /maps`
  - 查询地图列表。
  - 支持 `tier`、`search`、`page`、`pageSize`。
  - 默认 `pageSize = 24`。
  - **读缓存：** 按查询参数（`tier`/`search`/`page`/`pageSize`）缓存整页 DTO；`SurfWeb:Cache:MapsMinutes`（默认 5）过期，懒刷新。

- `GET /maps/{mapName}`
  - 查询地图详情。
  - 返回主线信息、WR、完赛数、`bonusTracks`。
  - **读缓存：** 按地图名缓存详情（含「不存在」）；TTL 同 `MapsMinutes`，懒刷新。

- `GET /maps/{mapName}/leaderboard`
  - 查询地图排行榜。
  - 支持 `track`、`stage`、`page`、`pageSize`。
  - 默认 `pageSize = 10`。
  - **读缓存：** 按 `map`/`track`/`stage`/`page`/`pageSize` 缓存整页结果；`SurfWeb:Cache:LeaderboardSeconds`（默认 60）过期，懒刷新。

- `GET /players/{auth}`
  - 查询玩家摘要。

- `GET /players/{auth}/times`
  - 查询玩家成绩列表。
  - 支持 `map`、`page`、`pageSize`。
  - 默认 `pageSize = 50`。

- `GET /players/{auth}/completions`
  - 查询玩家完赛地图列表（每图取玩家最佳主线成绩，按最近完赛排序）。
  - `worldRecordTime` / `gapFromWr`：按地图全服主线最快时间（`track = 0`）计算，非「每玩家一条 WR」。
  - 默认 `pageSize = 20`。

- `GET /rankings`
  - 查询全站排行榜。
  - 支持 `type`、`page`、`pageSize`。
  - **读缓存：** `RankingQueryService` 按 `type` 缓存全量 Top 100（`SiteLimits.MaxRankingsTotal`），`page`/`pageSize` 在内存切片；过期由 `SurfWeb:Cache:RankingsRefreshMinutes` 控制，**过期后下一次用户请求**触发重新查库（非后台定时任务）。

- `GET /records/recent`
  - 查询最新记录。
  - 支持 `page`、`pageSize`，兼容 `limit`。
  - **读缓存：** 全量 Top 100（dedupe + WR 计算后的 DTO 列表）单 key 缓存；过期由 `SurfWeb:Cache:RecentRefreshMinutes` 控制，懒刷新策略同上。

- `GET /config/styles`
- `GET /config/map-images`
- `GET /config/servers`（静态配置摘要，不含在线玩家）

- `GET /servers`
  - 返回实时服务器状态（对齐旧版 `Steam/GetServerInfo` + 玩家列表）。
  - 查询参数：`refresh`（可选，`true` 时强制立即 Steam A2S 刷新）。
  - 响应字段：`name`、`address`、`online`、`map`、`mapTier`、`players`、`maxPlayers`、`note`、`onlinePlayers[]`（`name`、`auth`、`durationSeconds`、`durationDisplay`）。
  - 实现：`ServerStatusRefresher` 后台每 `SurfWeb:ServerQuery:RefreshSeconds` 秒 UDP 查询；`GET` 在缓存过期时懒刷新；Steam 逻辑移植自 `Server/参考/SurfWebDefault/Utils/Steam/SteamUtil.cs`；地图 Tier / 玩家 `auth` 来自 Shavit（失败时仍返回 Steam 数据）。

### 5.2 教学型写侧接口

- `POST /api/v1/admin/runs`
  - 用于展示 DDD 命令侧的流转。
  - 输入包含玩家、地图、style、track、stage、时间、记录时间等字段。
  - 该接口不承担首页查询职责，也不替代读侧排行榜接口。

### 5.3 响应约定

统一返回：
- 成功：`{ data, meta? }`
- 失败：`{ error: { code, message } }`

读 API（`/api/v1/*`，不含 `/api/v1/admin/*`）经 `MinimumResponseDelayMiddleware` 保证**最短响应时间**：`SurfWeb:MinResponseDelaySeconds`（默认 `0.2`）。实际处理快于该值时补齐等待；慢于该值则处理完立即返回。设为 `0` 可关闭。

分页响应使用：
- `page`
- `pageSize`
- `total`

### 5.4 读侧内存缓存

- 实现：`IMemoryCache` + `IQueryCache` / `QueryCache`（`SurfWeb.Application/Caching`），在 `AddSurfWebApplication` 注册。
- **全量快照（Top 100）：** `surfweb:rankings:*`、`surfweb:records:recent`；`RankingsRefreshMinutes` / `RecentRefreshMinutes`（默认 1 分钟）；内存分页。
- **按查询参数缓存（地图）：** `surfweb:maps:list:*`、`surfweb:maps:detail:*`、`surfweb:maps:lb:*`；`MapsMinutes`（默认 5 分钟）、`LeaderboardSeconds`（默认 60 秒）。
- **刷新策略：** 均为绝对过期 + **过期后下一次用户请求**才重新查库（无后台定时刷新）。
- 并发：同 key 过期时 `SemaphoreSlim` 单飞，避免击穿。

---

## 6. 前端设计

### 6.1 视觉方向

采用 **像素简约**（由方向 C 演进，预览见 `docs/design-previews/ui-pixel-minimal.html`）。

主要特征：
- 暖纸色底 `#f3f1eb` + 可选 8px 淡网格；墨黑 `#121212` 2px 描边。
- 阶梯硬阴影（4px / 2px），无模糊投影。
- 字体：**IBM Plex Sans**（中文正文）、**Silkscreen**（导航短码、区块代号、名次、Tier 芯片）、**JetBrains Mono**（时间、连接串、地图名）。
- 强调色 `#3d5afe`（第 1 名、主按钮、Logo 块）；表头反色、行 hover 填黑与方向 C 一致。
- 顶栏品牌：**地满滑翔** + 英文点缀 **SURF RECORD**；Logo 为 `Web/public/brand-icon.png`；导航为**中文 + 英文码**。站点 `favicon` 同图。
- 页内区块同为**中文主标题 + 英文像素小字**（如「最新记录」旁 RECENT）；表头等内容使用中文。

主题常量与工具类：`Web/src/constants/pixelTheme.ts`、`Web/src/style.css`（`px-panel`、`px-btn`、`px-chip` 等）。

### 6.2 页面

- 首页：排行榜、最新记录；双栏列表固定 10 行 + 底部分页，表体区横向裁剪（`overflow-x-hidden`），分页栏处不出现横向滚动条；地图名等长文本在单元格内截断。加载时仅表体 `SkeletonTable`，底部分页始终为真实 `PaginationBar`（`loading` 时按钮禁用，已知总数时仍显示「共 N 条」文案）。首页真实表与骨架表均使用 `table-fixed` + 相同 `colgroup`：排行榜为「名次 3rem / 玩家自适应 / 积分 7rem」，最新记录为「玩家 32% / 地图自适应 / 时间 10rem」；多行骨架占位按纵向排列，避免加载态与数据态切换时列宽、行内文本和底栏发生跳动。
- 服务器页（`/servers`）：顶栏为**当前地图名**（链至地图详情）+ Tier 芯片 + 在线/离线；主体为左右两张 **`px-panel-sm` 卡片**（约 2/3 地图预览 + 1/3 在线玩家列表，`gap-4` 分隔，风格同地图卡片）；「加入」进服按钮；数据来自 `GET /servers`（含 `onlinePlayers`），前端每 30s 轮询刷新。
- 地图页：地图卡片列表，滚动加载；加载态 `SkeletonMapCard` 与 `MapCard` 同结构（16:9 预览、`p-4` 信息区、Tier/完赛/标题/WR 占位行），真实卡片 WR 行固定 `min-h-4`，避免骨架与内容切换时高度跳动。
- 地图详情页：地图信息、排行榜、Bonus 切换；**同一套布局壳**内加载：首次进入时头图/Tab/表一并骨架，数据就绪后**一次切换**；排行榜加载态在 `LeaderboardTable` 内切换单元格（不替换整张表 DOM）；Tab 预留「主线 + 最多 6 个 Bonus」；分页加载文案占位 `共 — 条 · 第 n / — 页` 避免底栏跳动。
- 玩家页：玩家摘要、成绩列表、完成地图；完成地图每页固定 10 行槽位（末页不足补空行），骨架屏同为 10 行，切换分页时高度与地图详情排行榜一致。完赛地图真实表与骨架表均使用 `table-fixed` + 相同 `colgroup`：地图自适应、Tier 4rem、时间 7rem、同步 6rem、日期 9rem，避免分页加载态与数据态切换时列宽抖动。

### 6.3 前端运行约定

- 全局布局：`App.vue` 使用 `min-h-screen` + `flex-col`，`main` 占满剩余高度，页脚 `border-t` 在内容较少时仍贴齐视口底部。
- `Web/.env.development` 默认：`VITE_API_BASE_URL=http://localhost:5240/api/v1`
- 本地前端开发端口默认由 Vite 管理，后端 CORS 允许 `localhost:5173` 与 `127.0.0.1:5173`。

---

## 7. Strong DDD 架构说明

### 7.1 为什么不是普通三层

传统三层通常是：
- `Controller -> Service -> Repository -> Table Entity`

本项目当前刻意做成两条链路：
- 读侧：`Controller -> Query Service -> Read Repository -> Read Model`
- 写侧：`Controller -> Command UseCase -> Aggregate / Domain Service -> Repository Interface -> Infrastructure`

这样做的目的不是追求最省代码，而是让 DDD 特征足够明显，便于学习。

### 7.2 写侧核心概念

聚合根：
- `Player`
- `Map`
- `RunRecord`

值对象：
- `PlayerId`
- `MapName`
- `StyleId`
- `TrackId`
- `StageId`
- `RunTime`

领域事件：
- `RunRecordedDomainEvent`
- `WorldRecordBrokenDomainEvent`

领域服务接口：
- `ICompletionPolicy`
- `IWorldRecordPolicy`

聚合仓储接口：
- `IPlayerRepository`
- `IMapRepository`
- `IRunRecordRepository`

### 7.3 读写分离的边界

读侧负责：
- 排行榜分页
- 最近记录去重
- 地图列表聚合视图
- 玩家查询视图

写侧负责：
- 命令输入建模
- 聚合行为
- 规则判断
- 领域事件触发
- 仓储与提交边界

### 7.4 当前教学性质的限制

当前底层 Shavit 数据源仍按只读源处理，因此：
- `EfUnitOfWork` 主要用于表达命令侧提交边界。
- 写侧仓储主要用于演示 DDD 结构与规则编排。
- 命令侧不会把数据回写进现有 Shavit 表。

这意味着当前项目是：
- 真实可用的读侧查询系统。
- 明确可见的 DDD 写侧骨架。

---

## 8. 配置与运行

### 8.1 后端配置

`Server/SurfWeb.Api/appsettings.json` 中只保留可公开的默认配置与模板占位配置：
- `ConnectionStrings:Shavit`
- `SurfWeb:CorsOrigins`
- `SurfWeb:MinResponseDelaySeconds`（读 API 最小响应秒数，默认 `0.2`）
- `SurfWeb:Cache`（`MapsMinutes`、`LeaderboardSeconds`；`RankingsRefreshMinutes`、`RecentRefreshMinutes` 默认 `1`）
- `SurfWeb:Styles`
- `SurfWeb:MapImages`：`BaseUrl` 使用 `https://example.com/surf-map-images/` 作为模板占位，真实图床目录放入本地配置；前端拼图为 `{BaseUrl}{地图名}{Extension}`
- `SurfWeb:ServerQuery`：`RefreshSeconds`（后台 A2S 刷新间隔，默认 30）、`QueryTimeoutMs`（UDP 超时，默认 8000）
- `SurfWeb:Servers[]`：公开默认值使用 `127.0.0.1:27015` 作为模板占位；真实服务器放入本地配置。字段包含 `Name`、`Address`（`connect host:port` 或 `host:port`）、可选 `Host`/`Port` 覆盖、`MaxPlayers` 占位。

本地开发（二选一，均不提交 Git）：
- **推荐：** 复制 `Server/SurfWeb.Api/appsettings.Development.local.json.example` 为 `appsettings.Development.local.json`，填入数据库、地图图床和服务器地址；`Program.cs` 在 `Development` 下会自动加载该文件（见 `.gitignore` 的 `appsettings.*.local.json`）。
- 或使用 User Secrets：`dotnet user-secrets set "ConnectionStrings:Shavit" "…"`（`UserSecretsId` 见 `SurfWeb.Api.csproj`）。

阿里云 RDS：白名单需包含本机公网 IP；本实例连接串使用 `SslMode=None`（不支持 SSL）。

生产环境推荐：
- 数据库连接串放到部署环境变量或密钥管理，勿写入仓库。

### 8.2 本地运行

后端：
- HTTP：`http://localhost:5240`
- HTTPS：`https://localhost:7182`

前端：
- `Web/.env.development` 指向 `http://localhost:5240/api/v1`

### 8.3 注意事项

原本地端口 `7082` 在部分 Windows 机器上可能落入系统 TCP 排除端口区间，因此已改为 `7182`，避免 Kestrel 启动绑定失败。

---

## 9. 测试与验证

当前已建立的验证重点：
- 应用层查询规则测试。
- DDD 结构形状测试。
- 聚合行为测试。
- 命令用例测试。

本轮后端重构的基础验证命令：
- `dotnet test Server/SurfWeb.slnx`
- `dotnet build Server/SurfWeb.slnx`

前端基础验证命令：
- `npm.cmd run build`（工作目录 `Web/`）

---

## 10. 后续演进方向

计划中的后续工作：
- 玩家页等其余读接口按需接入 `IQueryCache`。
- 继续打磨移动端体验与空状态。
- 按需扩展更完整的写侧业务场景。
- 在保持读侧性能的前提下，继续补齐教学型 DDD 样例。

---

## 11. 文档同步约定

只要出现以下变化，本文件必须同轮更新：
- API 路径变化。
- 前端目录或运行方式变化。
- DDD 边界、聚合、值对象、命令侧结构变化。
- 本地默认端口、环境变量、配置入口变化。

禁止新建第二份总设计文档；所有总设计信息统一维护在本文件。
