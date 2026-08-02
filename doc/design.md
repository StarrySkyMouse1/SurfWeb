# SurfWeb 设计文档

**日期：** 2026-05-22  
**状态：** 已批准，按实现持续同步  
**唯一设计文档：** 本仓库只维护这一份设计文档 `doc/design.md`

---

## 1. 产品目标

SurfWeb 是一个面向 Surf 服务器成绩查询的只读网站，目标是提供地图、玩家、排行榜、最新记录等核心查询能力。

当前版本的产品目标：
- 面向用户：提供可直接使用的成绩查询站点。
- 面向维护：后端采用 **Api + Data 两层**，查询链路清晰，无 DDD 写侧样板代码。

非目标：
- 不提供用户登录、后台管理、成绩人工提交。
- 不直接修改 Shavit 原库表结构。
- 不实现完整检查点排行榜页（`cpwrs` 榜表）；地图详情仅提供主线 **TOP10 检查点差异折线图**（只读 `cptimes`；中间 CP tooltip 为当场最快差距，**终点**与右侧主线榜 `playertimes` 差距一致）。

---

## 2. 当前实现摘要

- 后端：.NET 10，**Api + 五项目**（`Configurations`、`Utils`、`Repositories`、`Services`、`SurfWeb.ServerStatus`）。
- 前端：Vue 3 + Vite + Tailwind，目录位于 `Web/`。
- 数据源：Shavit 成绩库只读查询；EF 提供程序可配置 **MySQL**（默认）或 **SQLite**（`SurfWeb:Database:Provider` + `ConnectionStrings:Shavit`）。
- 本地 API 开发地址：`http://localhost:5240`，HTTPS 地址：`https://localhost:7182`；开发环境提供 Swagger UI（`/swagger`）。
- 前端默认 API 地址：`http://localhost:5240/api/v1`。
- **生产发布：** 统一入口 **`Build/surf.ps1`**（无参数交互选 Docker / 宿主机并完成全流程），说明见 **`Build/README.md`**；概要见 **`doc/deploy.md`**。

当前核心能力：
- 地图列表、地图详情 v2（主线榜 + 检查点图 + 阶段/奖励双栏）、排行榜分页。
- 玩家冲浪档案（8 项排名 + 数值；上大卡蓝调渐变 + 下勋章，对齐设计稿 v2 预览）、记录列表（近期 / WR / 未完成 × 主线 / 阶段 / 奖励）与联动图表。
- 全站排行榜、最新记录。
- 对外 REST：`GET /api/v1/api/records/latest`（`IApiService` / `ApiLatestRecordsEngine` 直查库，**完成时间游标** + 类型筛选；后续 SignalR 拟复用该逻辑）。
- 配置接口：样式、地图图床；服务器实时状态（Steam A2S + 后台刷新）。

---

## 3. 代码范围与目录

### 3.1 后端（解决方案项目）

| 项目 | 路径 | 职责 |
|------|------|------|
| `SurfWeb.Core` | `Server/SurfWeb.Core/` | `Models/`、`Dtos/`、`Enums/`、`Options/`、`Constants/`（`SiteLimits`） |
| `Configurations` | `Server/Configurations/` | `DependencyInjection/*`、CORS、`Middleware`、`Common/ApiResponse`（绑定 `SurfWeb.Core.Options`，无独立 Options 项目内目录） |
| `Utils` | `Server/Utils/` | 无状态工具与读侧缓存：`TimeFormatter`、`Caching/`（`CacheKeys`、`IQueryCache`、`QueryCache` 等）、服务器地址/地图名解析；`AddSurfWebQueryCache` |
| `Repositories` | `Server/Repositories/` | `ShavitDbContext`、`IBaseRepository<T>`、`AddSurfWebRepositories`（MySql / Sqlite 按配置切换） |
| `Services` | `Server/Services/` | `IServices/*`、`Services/*`（含 `IRealtimeRecentRecordsService`、`IApiService` 直查库）、`AddSurfWeb` / `AddSurfWebData` |
| `SurfWeb.ServerStatus` | `Server/SurfWeb.ServerStatus/` | `IServices/`、`Models/`、`Services/`、`Steam/`；**不**引用 `Repositories`，Shavit 补充经 `IMapService` / `IUserService` |
| `SurfWeb.Realtime` | `Server/SurfWeb.Realtime/` | SignalR `RecordsHub`、`RealtimeRecentRecordsPushWorker` |
| `SurfWeb.Api` | `Server/SurfWeb.Api/` | Controller、组合根、`Program.cs` |

依赖方向：`SurfWeb.Core`（含 Options）← `Utils` ← `Repositories` ← `Services` ← `SurfWeb.ServerStatus` / `SurfWeb.Realtime` ← `SurfWeb.Api`；`Configurations` 引用 `Core` 负责 Options 绑定与横切中间件。`Program.cs`：`AddSurfWebWebHost` 等横切配置后，`AddSurfWeb`（仓储 + Services）、`AddSurfWebServerStatus`（Steam + 在线状态）、`AddSurfWebRealtime`（Hub + 实时推送）。

### 3.2 前端

- `Web/`：Vue 3 单页应用。
- 路由：`/`、`/maps`、`/maps/:name`、`/players/:auth`、`/servers`。
- 兼容旧链接：`/rankings`、`/records` 重定向到首页。
- **目录约定：** 跨页复用 `Web/src/components/`（`AppHeader`、`PaginationBar`、`MapPreviewImage`、`skeleton/SkeletonBar`）；各页 UI 与骨架内聚在 `views/<feature>/components/`，**统一**通过组件 prop `loading` 在同一 DOM 壳内切换 `SkeletonBar` 占位（不再维护独立 `Skeleton*.vue` 表/卡片副本）。公共仅保留 `SkeletonBar` 原子占位条。
- **样式约定：** 全站视觉与类名分层见 **§6.1.1**；新 UI **先复用** `Web/src/styles/components/*`，禁止为单页再抄一套 `px-*`。

### 3.3 文档

- 设计文档：`doc/design.md`
- 实施计划：`docs/superpowers/plans/*.md`

---

## 4. 数据来源与业务语义

当前数据主要来自以下表：
- `users`：玩家资料、积分（`points`）、游玩时长。
- `playertimes`：通关记录，包含 `style`、`track`、`map`、`time`、`date`。
- `stagetimes`：分段记录，包含 `stage`（阶段榜 `track=0` + `stage`）。
- `cptimes`：检查点用时（地图详情「检查点差异」折线图；库内为累计秒，前端 tooltip 展示相对差距；非完整 CP 排行榜页）。
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
  - 返回主线信息、WR、完成数、`bonusTracks`、`stages`（该图 `stagetimes` 且 `track=0` 的 `stage` 去重升序）。
  - **读缓存：** 按地图名缓存详情（含「不存在」）；TTL 同 `MapsMinutes`，懒刷新。

- `GET /maps/{mapName}/checkpoints`
  - 主线检查点折线图数据（只读 `cptimes`）。
  - 查询参数：`track`（默认 `0`）、`limit`（默认 `10`，上限 10）。
  - 响应 `MapCheckpointChartDto`：`checkpointLabels`（`起点` / `CPn` / `终点`）、`series[]`（`rank`、`auth`、`playerName`、`cumulativeSeconds[]`，`time` 为累计秒）。
  - `series` **固定为主线榜 TOP `limit`（默认 10，不向后补位）**；无 `cptimes` 的玩家仍返回（`cumulativeSeconds` 全 `null`），图例保留并弱化样式；`rank` 为榜内名次；有数据的玩家缺检查点为 `null`；`checkpointLabels` 取自 TOP 内或全图 `cptimes` 检查点并集。
  - **读缓存：** `CacheKeys.MapCheckpoints`；TTL 同 `LeaderboardSeconds`。

- `GET /maps/{mapName}/leaderboard`
  - 查询地图排行榜。
  - 支持 `track`、`stage`、`page`、`pageSize`。
  - 默认 `pageSize = 10`。
  - **读缓存：** 按 `map`/`track`/`stage`/`page`/`pageSize` 缓存整页结果；`SurfWeb:Cache:LeaderboardSeconds`（默认 60）过期，懒刷新。

- `GET /players/{auth}`
  - 冲浪档案摘要：`points`/`playtime` 来自 `users`；`mainCompletionCount`（主线 `track=0` 不重复地图数）、`bonusCompletionCount`（奖励 `track>0` 不重复地图+赛道数）；`wrCount` 为三类 WR 之和，`mainWrCount`/`stageWrCount`/`bonusWrCount` 语义与全站 `GET /rankings?type=wr&wrScope=` 一致（全服最快持有者，非个人 PB）。
  - 各指标 `*Rank` 为全表计数排名（同分按 `auth` 升序靠前），计数时排除本人以免浮点比较误判。

- `GET /players/{auth}/records`
  - 玩家记录分页列表 + 同筛选下图表聚合；`category`（`PlayerRecordCategory`）：`recent` / `wr` / `incomplete`；`scope`（`PlayerRecordScope`）：`main` / `stage` / `bonus`；`page`、`pageSize`（默认 **10**）。
  - **近期**（`recent`）：该玩家全部成绩条目按 `date` 降序（主线 `playertimes` `track=0`；奖励 `track>0`；阶段 `stagetimes`），非每图去重；每条附带 `worldRecordTime`、`gapFromWr`（相对全服最快之差，秒；与 WR 持平或 ≤0.001s 时 `gapFromWr=0`，供前端显示 `+0.000`）；联动图主柱 **「今年完成次数」**，按自然年 1–12 月统计（UTC）。
  - **WR**（`wr`）：该玩家为全服最快持有者的条目，按 `date` 降序；联动图主柱标题 **「WR 达成 · 今年」**，按**自然年** 1–12 月统计达成次数（UTC）。
  - **未完成**（`incomplete`）：与 `maptiers` 交集；主线为无 `track=0` 成绩的地图；奖励为全站存在该奖励赛道但玩家无成绩；阶段为全站存在该 `(map,track,stage)` 但玩家无成绩；排序 Tier 升序 → 地图名（→ 赛道/阶段）字母序；可选查询参数 **`tier`**（0–8）；不传或前端选 **「全部」** 时不按 Tier 筛选（默认 **全部**），图表与列表同步；**全图库完成率** 柱图在主线/奖励/阶段下均统计当前范围内的**已完成 vs 未完成**（非仅未完成一项）。
  - 响应 `data`：`items`（`PlayerRecordDto`：`map`、`tier`、`track`、`stage`、`time`/`timeFormatted`、`sync`、`date`、`worldRecordTime`/`gapFromWr`（仅 `recent`；持 WR 时 `gapFromWr=0`）、`status`）+ `charts`（`PlayerChartsDto`：双块柱图 `primaryBars`/`tierBars`、标题与页脚文案）；`tierBars` 固定 **T0–T8** 共 9 档（无数据为 0），与地图页 Tier 范围一致；`meta.total` 为列表总条数。

- `GET /rankings`
  - 查询全站排行榜。
  - 支持 `type`（`RankingType` 枚举，查询参数仍为小写字符串，大小写不敏感）：`points`（积分，来自 `users.points`）、`completions`（有成绩的不重复地图/赛道数）、`playtime`（在线时长，秒）、`wr`（持有 WR 条目的数量排行）；非法值返回 400；`page`、`pageSize`。
  - `type=completions` 时可选 `completionScope`（`TrackRankingScope` 枚举，大小写不敏感，缺省 `main`）：`main`（主线 `track=0` 每图计 1）、`bonus`（奖励 `track>0` 每图每赛道计 1）；两套独立缓存 key。
  - `type=wr` 时可选 `wrScope`（`WrRankingScope` 枚举，大小写不敏感，缺省 `main`）：`main`（主线 `track=0` 每图一条 WR）、`bonus`（奖励赛道 `track>0` 每图每赛道一条 WR）、`stage`（`stagetimes` 每图每赛道每阶段一条 WR）；三套独立缓存 key。
  - **读缓存：** `RankingService` 按 `type`（完成/WR 另按 scope）缓存全量 Top 100（`SiteLimits.MaxRankingsTotal`），`page`/`pageSize` 在内存切片；过期由 `SurfWeb:Cache:RankingsRefreshMinutes` 控制，**过期后下一次用户请求**触发重新查库（非后台定时任务）。

- `GET /records/recent`
  - 查询最新记录（`playertimes` + `stagetimes`）。缓存快照 `RecentRecordsSnapshot` 预计算七套列表，各最多 100 条、按**该条成绩的完成时间**降序：「全部」（主线 + 奖励 + 阶段）、「主线」（`track=0`）、「阶段」（`stagetimes`）、「奖励」（`track>0`）以及 WR 的主线 / 阶段 / 奖励三类。构建时各范围独立读取最近批次，避免主线、奖励、阶段互相挤占；玩家成绩按 `(玩家, 地图, track)` 去重，阶段成绩按 `(玩家, 地图, track, stage)` 去重，保留该玩家在对应赛道 / 阶段上的**个人最快**一条（非全服地图最快），再取 Top 100。
  - 支持 `page`、`pageSize`，兼容 `limit`；`filter`（`RecentRecordFilter` 枚举，大小写不敏感，缺省全部）：`all` / `main` / `stage`（`stagetimes`）/ `bonus` / `wr`；`filter=wr` 时可选 `wrScope`（`WrRankingScope`）：`main` / `stage` / `bonus`；非法值 400；各列表独立 Top 100、按完成时间降序，内存分页。
  - 响应项含 `tier`（地图难度，来自 `MapTier`）；阶段记录返回 `stage` 字段，首页最新记录可按阶段筛选。
  - **读缓存：** 单 key `surfweb:records:recent`；过期由 `SurfWeb:Cache:RecentRefreshMinutes` 控制，懒刷新。实时推送见 §5.1.1（**直查库**，不走该缓存）。

#### 5.1.1 SignalR：最新记录订阅（对外集成）

- **实现：** 查询在 `Services`（`IRealtimeRecentRecordsService` / `RealtimeRecentRecordsService` 直查 Shavit）；推送在 `SurfWeb.Realtime`（`RealtimeRecentRecordsPushWorker` 按 `Id` 轮询并广播）。
- **Hub 路径：** `/hubs/records`（WebSocket；协商 URL 为 `{origin}/hubs/records`）。
- **配置：** `SurfWeb:Cache:RecentPushSeconds`（默认 **30**）；为 **0** 时禁用轮询与推送（REST 仍可用）。
- **筛选 `scope`：** Hub 入参为 `RealtimeRecentRecordScope` 枚举（`All` / `Main` / `Bonus` / `Stage`）；`All` 为全部新完成（主线+奖励+阶段），`Main` 为 `track=0`，`Bonus` 为 `track>0`，`Stage` 为 `stagetimes`。
- **客户端流程：**
  1. 建立连接（.NET：`Microsoft.AspNetCore.SignalR.Client`；其他语言用对应 SignalR 客户端）。
  2. **`SubscribeRecent(scope?, snapshotPageSize?)`**（`scope` 传枚举名或整型）→ 加入组 `recent:{scope}`，并收 **`RecentSnapshot`**（`RealtimeRecentRecordsSnapshotMessage`）。
  3. 后台发现**新插入**的完成记录（按表 `Id` 游标，启动时不推历史）→ 向相关组广播 **`RecordsUpdated`**（`RealtimeRecentRecordsUpdatedMessage`，`added[]`）。
  4. **`UnsubscribeRecent(scope?)`** 退订。
- **推送项字段（`RealtimeRecentRecordDto`）：** 除地图/玩家/时间等外，含 `firstPlaceTime` / `gapFromFirst`（相对该时刻全服最快）、`personalBestTime` / `gapFromPersonalBest`（当前成绩相对该玩家此图/赛道/阶段在**该条完成时刻**的个人最快，含本条；持 PB 或差距 ≤0.001s 时为 **0**，更慢完成时为正数）；持 WR 时 `gapFromFirst` 为 **0**。
- **与 REST：** REST `GET /records/recent` 仍为缓存读侧；SignalR 为实时集成专用，断线重连后应重新 `SubscribeRecent`。
- **开发测试（Swagger）：** `POST /api/v1/realtime/push/trigger`（仅 `Development`）手动执行一轮查库+推送；需另有客户端已 `SubscribeRecent` 才能看到 `RecordsUpdated`。

#### 5.1.2 对外 REST：最新记录（时间游标）

- `GET /api/v1/api/records/latest`（`ApiController` + `IApiService` / `ApiLatestRecordsEngine`，**直查库**，无 IMemoryCache；**SignalR 推送后续拟复用本逻辑**）。
- 查询参数：
  - `token`（**必填**）：与配置 `SurfWeb:ExternalApi:LatestRecordsToken` 一致；缺失或不匹配返回 **401**、`error.code = unauthorized`。
  - `type`（可选，字符串，大小写不敏感，缺省 **全部**）：`all`（0）/ `main`（1，主线 `track=0`）/ `bonus`（2，奖励 `track>0`）/ `stage`（3，阶段）；也接受整型 0–3 或枚举名 `All`/`Main`/…；非法值 **400**。
  - `after`（可选，ISO 8601）：有值时仅返回完成时间**严格晚于**该时刻的记录，按 `recordedAt` **升序**（同秒按 `id` 升序），最多 **50** 条（`SiteLimits.ApiLatestRecordsCount`）；**省略或为空**则仅返回最新 **1** 条，按 `recordedAt` **降序**。
  - 无 `limit` 查询参数。
- 响应 `data`：`ApiLatestRecordDto[]` — `playerName`、`map`、`tier`、`type`、`track`、`stage`、`typeLabel`、`recordedAt`、`gapFromWr`（**不含** `gapFromMe`：Shavit `playertimes` / `stagetimes` 对同一玩家+图+赛道/阶段仅保留一条，无法计算有意义的个人差距）。
- `gapFromWr` 按该条记录 `recordedAt` 的历史状态计算：只纳入完成时间早于该记录的成绩，完成时间相同则仅纳入 `Id <= 当前 Id` 的成绩；因此后续产生的新 WR 不会改变历史推送记录的差距。输出为带符号三位小数（如 `+1.234`）；持 WR 或差距 ≤0.001s 时为 `+0.000`。
- 与 `GET /records/recent`（站点缓存、PB 去重）并列；当前 SignalR 仍用 `PollNewSinceAsync`（`Id` 向上增量），待后续改为复用本 API 查询。

- `GET /config/map-images`
- `GET /config/servers`（静态配置摘要，不含在线玩家；前端服务器页使用 `GET /servers` 实时接口）

- `GET /servers`
  - 返回实时服务器状态（对齐旧版 `Steam/GetServerInfo` + 玩家列表）。
  - 查询参数：`refresh`（可选，`true` 时强制立即 Steam A2S 刷新）。
  - 响应字段：`name`、`address`、`online`、`map`、`mapTier`、`players`、`maxPlayers`、`note`、`onlinePlayers[]`（`name`、`auth`、`durationSeconds`、`durationDisplay`）。
  - 实现：`SurfWeb.ServerStatus` 中 `ServerStatusRefresher` 后台每 `SurfWeb:ServerQuery:RefreshSeconds` 秒 UDP 查询；`GET` 在缓存过期时懒刷新；Steam 逻辑移植自 `Server/参考/SurfWebDefault/Utils/Steam/SteamUtil.cs`（已支持**域名 DNS 解析**，不要求配置纯 IP；`Host` 误带 `:port` 会自动剥离）；地图 Tier / 玩家 `auth` 经 `IMapService.GetMapTierByMapNameAsync`、`IUserService.GetAuthsByNamesAsync` 补充（失败时仍返回 Steam 数据）。

### 5.2 只读边界

- 对外均为只读 `GET`（及配置类读接口）；无写侧/管理端点。
- `ShavitDbContext.SaveChanges` 禁止持久化写入。

### 5.3 响应约定

统一返回（`Configurations/Common/ApiResponse`）：
- 成功：`ApiResponse<T>` → `{ data, meta? }`（Controller 使用具体 DTO 泛型，如 `MapDetailDto`）
- 失败：Controller 用 `ApiResponse<T>.Fail(ApiErrorCode, message?)`；错误码枚举 `ApiErrorCode` + `ApiErrorDescription` 特性（中文说明）；全局异常中间件直接序列化 `{ error: { code, message } }`

读 API（`/api/v1/*`，不含 `/api/v1/admin/*`）经 `MinimumResponseDelayMiddleware` 保证**最短响应时间**：`SurfWeb:MinResponseDelaySeconds`（默认 `0.2`）。实际处理快于该值时补齐等待；慢于该值则处理完立即返回。设为 `0` 可关闭。

分页响应使用：
- `page`
- `pageSize`
- `total`

### 5.4 读侧内存缓存

- 实现：`IMemoryCache` + `IQueryCache` / `QueryCache`（`Utils/Caching`），经 `AddSurfWebQueryCache` 注册（`AddSurfWeb` → `AddSurfWebServices` 内调用）。
- **全量快照（Top 100）：** `surfweb:rankings:*`、`surfweb:records:recent`；`RankingsRefreshMinutes` / `RecentRefreshMinutes`（默认 1 分钟）；内存分页。
- **按查询参数缓存（地图）：** `surfweb:maps:list:*`、`surfweb:maps:detail:*`、`surfweb:maps:lb:*`；`MapsMinutes`（默认 5 分钟）、`LeaderboardSeconds`（默认 60 秒）。
- **刷新策略：** 地图/排行榜等为绝对过期 + **过期后下一次用户请求**才重新查库；SignalR 推送由 `SurfWeb.Realtime` 按 `RecentPushSeconds` **直查库**，不更新 `surfweb:records:recent`。
- 并发：同 key 过期时 `SemaphoreSlim` 单飞，避免击穿。

---

## 6. 前端设计

### 6.1 视觉方向

采用 **像素简约**（由方向 C 演进，预览见 `docs/design-previews/ui-pixel-minimal.html`；玩家页 `ui-player-profile-v2.html`、地图详情 `ui-map-detail-v2.html`）。

主要特征：
- 暖纸色底 `#f3f1eb` + 可选 8px 淡网格；墨黑 `#121212` **外框** 2px 实线描边；**内部分隔** 1px 浅色线（不与外框同粗同色，见 v2 稿 `--px-divider`）。
- 阶梯硬阴影（4px / 2px），无模糊投影。
- 字体：**IBM Plex Sans**（中文正文）、**Silkscreen**（导航短码、区块代号、名次、Tier 芯片）、**JetBrains Mono**（时间、连接串、地图名）。
- 强调色 `#3d5afe`（第 1 名、主按钮、Logo 块）；表头反色、行 hover 填黑与方向 C 一致。
- 顶栏品牌：**站点名**（`VITE_SITE_TITLE`，默认 **地满滑翔**）+ 英文点缀 **SURF RECORD**；Logo 为 `Web/public/brand-icon.png`；导航为**中文 + 英文码**。站点 `favicon` 同图；浏览器标题 `{VITE_SITE_TITLE} · Surf Record`。
- 页内区块同为**中文主标题 + 英文像素小字**（如「最新记录」旁 RECENT）；表头等内容使用中文。

样式入口：`Web/src/style.css`（Tailwind + `Web/src/styles/` 模块）。设计令牌与分层复用见 **§6.1.1**。前端工具：`Web/src/utils/format.ts`、`Web/src/utils/display.ts`、`Web/src/utils/playerCharts.ts`（玩家柱图）。

#### 6.1.1 样式分层与复用（强制）

**原则：与全站统一，不为加而加。** 新页面/区块实现时，先对齐已有页（尤其地图详情 `/maps/:name` 排行榜表），再写 CSS；仅当共享层无法表达且确属单页独有布局时，才写入 `pages/<page>.css`。

| 层级 | 路径 | 放什么 | 不放什么 |
|------|------|--------|----------|
| 令牌 | `styles/theme.css` | 色板、字体变量 | 组件布局 |
| 基底 | `styles/base.css` | `body` 纸色底、网格 | 业务块 |
| **共享组件** | `styles/components/pixel-ui.css` | `px-panel`、`px-btn`、`px-chip`、`px-table-head`、`px-filter-main` / `px-filter-scope`、`px-filter-chip-row` 等 | 单页 bento |
| | `styles/components/table.css` | `px-table-row`、`px-table-data-cell`、`px-table-cell-content`、`px-paged-table-wrap`、`px-table-map-link` | 首页最新记录 mask 等特殊列 |
| | `styles/components/chart.css` | `px-chart-panel`、柱图条 | 玩家档案 |
| | `styles/components/pagination.css` | 分页条 | — |
| | `styles/tier.css` | Tier 色类 | — |
| **页专属** | `styles/pages/home.css` | 首页双栏、最新记录缩略图渐变 | 通用表行 |
| | `styles/pages/player.css` | 冲浪档案 `px-player-passport-*`、`px-player-records-split` | 表/筛选/柱图（应用共享类） |
| | `styles/pages/server.css` | 服务器页布局 | 通用面板 |

**表格（分页列表）统一约定：**

- 外壳：`px-panel overflow-hidden` + 内层 `overflow-x-auto` + 需要固定行数时加 `px-paged-table-wrap`（10 行，`pageSize` 与组件一致）。
- 表头/行/悬停：`px-table-head`、`px-table-head-cell`、`px-table-row` / `px-table-row-empty`、`px-table-data-cell` + `px-table-cell-content`（与 `LeaderboardTable` 相同 DOM，禁止在 `player.css` 再定义一套行色）。
- 地图名列（带缩略图）：`px-table-map-link` + `MapPreviewImage` `variant="thumb"`；缩略图左实右透明 mask 在 `table.css`（`.px-table-map-link` / `.px-home-recent-map-link` 共用，非压暗）。

**新增 CSS 前自检：**

1. `pixel-ui.css` / `table.css` / `chart.css` 是否已有同类 `px-*`？
2. 能否只改 Vue 类名指向已有类（例如玩家筛选用 `px-filter-main-btn`，而非 `px-player-filter-*`）？
3. 若与地图详情表一致，是否已对照 `LeaderboardTable.vue`？
4. 仅冲浪档案 bento、玩家页 φ 分栏等**无第二处复用**的块，才进 `pages/player.css`。

**禁止：** 为单页复制 `px-table-row` 悬停、面板描边、筛选按钮尺寸；禁止新建 `pages/players.css` 或与 `components/*` 职责重叠的文件。

### 6.2 页面

- 首页：排行榜、最新记录；双栏 `items-start`（不按较高栏拉伸，避免表与分页之间留白），网格子项与 `.px-home-list-table-wrap > table` 均为 `w-full` + `table-fixed`，表体固定 10 行（`h-14` / `--px-home-table-block-h`），表格区高度随表头+行数自适应（`max-height` 上限 10 行），无固定留白；表格区 `overflow` 裁剪且不显示滚动条，行内容 `overflow-hidden` 不撑高页面；横向裁剪 `overflow-x-hidden`。`HomeRankingTable` / `HomeRecentTable` 在 `loading` 时于**同一** `colgroup`/表头下渲染 `SkeletonBar` 占位（末页行数由 `skeletonRowsForPage` 与数据态一致），底部分页始终为真实 `PaginationBar`。**最新记录**栏标题右侧为 `RecentRecordFilter`：浏览分栏芯片（`完成·全部` / `完成·主线` / `完成·阶段` / `完成·奖励`）+ WR 分栏芯片（`WR·主线` / `WR·阶段` / `WR·奖励`），切换时重置第 1 页；`filter` + 可选 `wrScope` 请求 `/records/recent`（阶段来自 `stagetimes`，WR 分范围与排行语义一致）。排行栏标题右侧 `RankingFilter`：积分 / 时长 + **完成**、**WR** 分栏芯片（左 `完成·主线` / `WR·主线` 等，右 ▼ 弹出范围；完成仅主线/奖励，WR 含主线/阶段/奖励）；`type=completions&completionScope=` / `type=wr&wrScope=` 请求 `/rankings`，切换时重置第 1 页；表头第三列随类型显示「积分」「完成」「时长」「WR」（时长列用 `formatPlaytime`：`X天 X小时`，不显示分/秒）。列宽：排行「名次 3rem / 玩家 / 数值列 7rem（时长筛选时为 11rem，`w-44`）」；最新记录「地图 42%（行高内左贴边缩略图 `MapPreviewImage` variant=`thumb`（`mask-image` 左实右透明渐变）；地图名单独占一行 `truncate`；第二行：左 `px-chip` Tier、右赛道类型小字（主线/Bn/阶段N，与 Tier 垂直居中）/ 玩家 / 时间 10rem」；**极窄（≤480px）** 两列（地图列叠玩家名 / 时间）；**481px 起至面板 &lt;40rem** 三列无缩略图（地图 | 玩家 | 时间）；**面板 ≥40rem**（容器查询）恢复左贴边缩略图与 42% 地图列。
- 服务器页（`/servers`）：`ServerInfoPanel` 在 `loading` 时于同一面板壳内渲染单条服务器占位（地图框 + 玩家列表 + 加入按钮）；顶栏为当前地图名 + Tier + 在线/离线；`GET /servers` 每 30s 轮询。
- 地图页：`MapCard` 支持 `loading`，首屏/加载更多在同一网格内渲染占位卡片（与真实卡片同 DOM 结构）；WR 行 `min-h-4` 防高度跳动。
- 地图详情页（`/maps/:name`）：对齐视觉稿 `docs/design-previews/ui-map-detail-v2.html`。**已实现：** `MapDetailHeader` → `MapDetailCategoryTabs`（**主线** / **阶段·奖励**）→ **主线**：`MapCheckpointChartPanel`（ECharts 5，`GET .../checkpoints`）+ `MapLeaderboardCard`（`track=0`，φ 双栏 `pages/map-detail.css`）→ **阶段·奖励**：`MapDetailStageBonusPanel` 双列（`≥1280px`）；表格外 `px-filter-scope-btn` 按 `detail.stages` / `detail.bonusTracks` 动态生成（`S{n}`、`b{n}`，无「全部」，默认第一项）；各榜 `GET .../leaderboard`（阶段 `track=0&stage=`，奖励 `track=`），`pageSize=10`，`px-paged-table-wrap` 固定 10 行；时间列 `+X.XXX` 由前端相对榜一 `formatTimeGap`；**阶段记录** / **奖励记录** 两卡始终并排展示；无 `stages` / `bonusTracks` 时不显示范围小 Tab，但保留 `px-map-record-toolbar` 占位使两列表头顶对齐；表体为空（固定 10 行 + 分页「共 0 条」）。旧 `MapDetailTabs`（主线+Bonus 单行）已替换。
- 玩家页（`/players/:auth`）：视觉稿 `docs/design-previews/ui-player-profile-v2.html`。**档案区**（仅 `pages/player.css`）：`PlayerPassportCard` 冲浪档案 bento、顶栏纯白（`bg-px-surface`）+ Mono ID、水印 `auth`、上大卡渐变与 L 角标、下勋章与 WR 列容器查询。**记录区**（复用 §6.1.1 共享表/筛选/柱图，对齐地图详情榜）：`PlayerRecordFilters`（`px-filter-main` / `px-filter-scope`；**未完成**为首页同款 `FilterSplitChip`，下拉 **T0–T8**，请求 `tier=`）+ `PlayerRecordsTable`（`px-panel`、`px-paged-table-wrap`、`px-table-*`、`px-table-map-link`）+ `PlayerChartsPanel`（`px-chart-panel`）+ `PaginationBar attached`；`GET /players/{auth}/records`，切换筛选或分页重置第 1 页；**近期** Tab 时间列在 `timeFormatted` 后以 `formatTimeGap` 显示与全服 WR 差距（与首页最新记录一致，`+X.XXX` / WR 为 `+0.000`）；**未完成**列表无「状态」列（已选未完成 Tab，文案冗余）；奖励/近期/WR 的奖励范围列表列为「地图 | Tier | 赛道」（Tier 在赛道前）；阶段范围为「地图 | Tier | 阶段」（Tier 在阶段前，与奖励一致）；**近期 / WR / 未完成** 共用同一套 `PlayerRecordsTable` 与 `px-table-*`（黑表头、地图缩略图列、行 hover、列宽）；地图列 `w-[40%] min-w-[11rem]`，名称 `break-words` 换行、无 `truncate`；表 `min-w-[560px]` 超出左侧容器时底栏横向滚动条常显（`px-paged-table-wrap--scroll-x`）；未完成仅列集合不同，无单独灰显样式；`mapImageConfig` + 同壳 `SkeletonBar`。

### 6.3 前端运行约定

- **样式复用：** 遵守 **§6.1.1**；改玩家记录表/筛选/柱图时优先改 `components/*.css` 或对齐 `LeaderboardTable`，不要扩写 `pages/player.css`。
- **骨架屏约定：** 各业务组件 `loading` 时在原位置插入 `SkeletonBar`（或缩略图方框），禁止再为同一块 UI 单独复制一份 `SkeletonXxx.vue` 表/卡；改布局只改业务组件一处。
- **组件路径：** `views/home/components/`（`HomeRankingTable`、`HomeRecentTable`、`RankingFilter`、`FilterSplitChip`（完成/WR/最新记录分栏芯片 + 范围气泡）、`RecentRecordFilter`）、`ChipFilter` / 筛选条共用 `.px-filter-chip-row`（同行垂直居中，选中无 `translate` 避免高低不齐）、`views/maps/components/`（`MapCard`、`TierFilter`）、`views/map-detail/components/`（`MapDetailHeader`、`MapDetailCategoryTabs`、`MapDetailMainPanel`、`MapDetailStageBonusPanel`、`MapCheckpointChart`、`MapLeaderboardCard`、`LeaderboardTable`）、`views/players/components/`（`PlayerPassportCard`、`PlayerRecordFilters`、`PlayerRecordsTable`、`PlayerChartsPanel`）、`views/servers/components/`（`ServerInfoPanel`）。地图详情样式：`styles/pages/map-detail.css`。
- **单页应用（SPA）：** `vue-router` + `createWebHistory()`，导航统一用 `RouterLink`，仅 `joinServer` 使用 `steam://` 外链；`App.vue` 内 `RouterView` 带淡入淡出过渡；`scrollBehavior` 切换路由时平滑滚到顶部（浏览器后退恢复原滚动位置）。
- 全局布局：`App.vue` 使用 `min-h-screen` + `flex-col`，`main` 占满剩余高度，页脚 `border-t` 在内容较少时仍贴齐视口底部。
- `Web/.env.development` 默认：`VITE_API_BASE_URL=http://localhost:5240/api/v1`、`VITE_SITE_TITLE=地满滑翔`；生产/Docker 同域反代用相对路径 `/api/v1`（`client.ts` 以 `window.location.origin` 解析，避免 `new URL` 报错）
- 本地前端开发端口默认 **20011**（Vite，`vite.config.ts`），后端 CORS 允许 `localhost:20011` 与 `127.0.0.1:20011`。

---

## 7. 后端两层架构

### 7.1 调用链

```
HTTP 请求
  → SurfWeb.Api.Controllers
  → IMapService / IPlayerService / …（Shavit 只读查询 + 缓存 + DTO）
  → IServerStatusService（Steam A2S + 内存快照，可选经 IServices 补 Tier/auth）
  → IBaseRepository<T>（EF 查 Shavit，仅 Services/Queries 使用）
  → 返回 JSON（ApiResponse 在 Api 层包装）
```

- **Api 层**：路由、`/api/v1`、CORS、异常处理、读 API 最小响应延迟、Swagger（开发环境）。
- **Services 层**：`IServices` / `Services` 编排 Shavit 查询与读缓存；Controller **不**直接依赖仓储。
- **ServerStatus 层**：Steam UDP、服务器状态缓存与后台刷新；**禁止**引用 `Repositories`，仅通过 `IMapService` / `IUserService` 读 Shavit。
- **Repositories 层**：`ShavitDbContext` 与 `IBaseRepository<TEntity>`（只读 `IQueryable`）；**不**向 Controller 暴露 DbContext。

### 7.2 目录约定

| 目录 | 说明 |
|------|------|
| `Server/Services/IServices/` | `IMapService`、`IPlayerService`、`IRankingService`、`IRecordService`、`IUserService` |
| `Server/Services/Services/` | `MapService` 等；注入 `IBaseRepository<User/MapTier/PlayerTime/StageTime>` 编写查询 |
| `Server/SurfWeb.ServerStatus/IServices/` | `IServerStatusService`（读列表 + 可选 `RefreshAsync`） |
| `Server/SurfWeb.ServerStatus/Models/` | `CachedServerStatus`、`CachedOnlinePlayer` |
| `Server/SurfWeb.ServerStatus/Services/` | `ServerStatusService`、`ServerStatusRefresher`（内存快照 + Steam 刷新 + `BackgroundService`） |
| `Server/SurfWeb.ServerStatus/Steam/` | `SteamServerQuery`（静态 A2S UDP）；`DependencyInjection.cs` 在根目录 |
| `Server/Repositories/` | `IBaseRepository.cs`、`BaseRepository.cs` |
| `Server/Repositories/Persistence/` | `ShavitDbContext`（只读） |
| `Server/SurfWeb.Core/Models/` | Shavit 表映射实体（`User`、`PlayerTime` 等） |
| `Server/SurfWeb.Core/Dtos/` | API 响应 DTO（`MapDetailDto`、`RankingEntryDto` 等） |
| `Server/SurfWeb.Core/Enums/` | 如 `RankingType`、`RecentRecordFilter` |
| `Server/SurfWeb.Core/Options/` | `SurfWebOptions`、`CacheOptions`、`ServerInfoOptions` 等（`appsettings` 的 `SurfWeb` 节） |
| `Server/SurfWeb.Core/Constants/` | `SiteLimits`（Top 100 等） |
| `Server/Configurations/` | `DependencyInjection/`、`Cors/`、`Middleware/`、`Common/ApiResponse` |
| `Server/Utils/` | `Common/TimeFormatter`、`Caching/`（`CacheKeys`、`IQueryCache`、`QueryCache`、`CachedPageList`、`RecentRecordsSnapshot`）、`Servers/`（`ServerEndpointParser`、`SteamMapNameNormalizer`）；`AddSurfWebQueryCache` |
| `Server/Services/` | `DependencyInjection.AddSurfWeb` 注册仓储 + Services（含 `AddSurfWebQueryCache`） |

### 7.3 已移除内容

- DDD 聚合、值对象、写侧仓储、`RecordRunUseCase`、admin 写接口。
- 原 `SurfWeb.Application`、`SurfWeb.Domain`、`SurfWeb.Infrastructure`、`SurfWeb.Application.Web` 四个项目。

---

## 8. 配置与运行

### 8.1 后端配置

`Server/SurfWeb.Api/appsettings.json` 中只保留可公开的默认配置与模板占位配置：
- `SurfWeb:Database:Provider`：`MySql`（默认）或 `Sqlite`
- `ConnectionStrings:Shavit`：MySQL 连接串，或 SQLite 的 `Data Source=…`（须与 Shavit 表结构兼容的 `.db`）
- `SurfWeb:CorsOrigins`
- `SurfWeb:MinResponseDelaySeconds`（读 API 最小响应秒数，默认 `0.2`）
- `SurfWeb:Cache`（`MapsMinutes`、`LeaderboardSeconds`；`RankingsRefreshMinutes`、`RecentRefreshMinutes` 默认 `1`）
- `SurfWeb:Styles`（仅服务端：解析 `DefaultStyleId` 并过滤 `playertimes`/`stagetimes`；无对外 HTTP 接口）
- `SurfWeb:MapImages`：`BaseUrl` 使用 `https://example.com/surf-map-images/` 作为模板占位，真实图床目录放入本地配置；前端拼图为 `{BaseUrl}{地图名}{Extension}`
- `SurfWeb:ServerQuery`：`RefreshSeconds`（后台 A2S 刷新间隔，默认 30）、`QueryTimeoutMs`（UDP 超时，默认 8000）
- `SurfWeb:Servers[]`：公开默认值使用 `127.0.0.1:27015` 作为模板占位；真实服务器放入本地配置。字段包含 `Name`、`Address`（`connect host:port` 或 `host:port`）、可选 `Host`/`Port` 覆盖（`Host` 只写主机名或 IP，**不要**带端口；域名会在 A2S 查询前 DNS 解析）、`MaxPlayers` 占位。
- `SurfWeb:ExternalApi:LatestRecordsToken`：`GET /api/v1/api/records/latest` 查询参数 `token`；未配置或为空时该接口一律 **401**（生产环境请用 User Secrets / 环境变量，勿提交真实 token）。

本地开发（二选一，均不提交 Git）：
- **推荐：** 复制 `Server/SurfWeb.Api/appsettings.Development.local.json.example` 为 `appsettings.Development.local.json`，填入数据库、地图图床和服务器地址；`AddSurfWebApi` → `AddSurfWebLocalConfiguration` 会加载 `appsettings.local.json` 与 `appsettings.{Environment}.local.json`（见 `.gitignore`）。
- 或使用 User Secrets：`dotnet user-secrets set "ConnectionStrings:Shavit" "…"`（`UserSecretsId` 见 `SurfWeb.Api.csproj`）。

阿里云 RDS：白名单需包含本机公网 IP；本实例连接串使用 `SslMode=None`（不支持 SSL）。

生产环境推荐：
- 数据库连接串放到部署环境变量或密钥管理，勿写入仓库。

### 8.2 本地运行

后端：
- HTTP：`http://localhost:5240`
- HTTPS：`https://localhost:7182`
- 开发环境 Swagger UI：`/swagger`（仅 `ASPNETCORE_ENVIRONMENT=Development` 启用；`launchSettings.json` 的 `https` 配置会默认打开该页）

前端：
- `Web/.env.development` 指向 `http://localhost:5240/api/v1`

### 8.3 注意事项

原本地端口 `7082` 在部分 Windows 机器上可能落入系统 TCP 排除端口区间，因此已改为 `7182`，避免 Kestrel 启动绑定失败。

### 8.4 生产发布（两种模式）

| 模式 | 说明 | 文档 |
|------|------|------|
| **A · 构建发布** | `Build/surf.ps1 host`；配置 **`Build/.env`** | [`Build/README.md`](../Build/README.md)、[`doc/deploy.md`](deploy.md) |
| **B · Docker** | `Build/surf.ps1`；配置 **`Build/.env`** | [`Build/README.md`](../Build/README.md) |

前端环境变量：开发 `Web/.env.development`；**发布构建**由 `Build/.env` 同步生成 `Web/.env.production`（`VITE_API_BASE_URL=/api/v1`）。部署配置**仅** `Build/.env`；`surf.ps1` 首次运行若无该文件则从 `Build/env.example` 自动创建，交互模式下打开记事本，保存后同轮继续部署。

Docker **web** 基础镜像默认经 DaoCloud 拉取 `nginx` / `node`（`Build/.env` 的 `NGINX_IMAGE`、`NODE_IMAGE`，见 `Build/env.example`）；`api` 仍为 `mcr.microsoft.com`。

---

## 9. 测试与验证

当前已建立的验证重点：
- 单元测试项目已移除；可按需在 `Services` 或独立测试项目中补回。

本轮后端重构的基础验证命令：
- `dotnet test Server/SurfWeb.slnx`
- `dotnet build Server/SurfWeb.slnx`

前端基础验证命令：
- `npm.cmd run build`（工作目录 `Web/`）

---

## 10. 后续演进方向

计划中的后续工作：
- 玩家页 `GET /players/*` 按需接入 `IQueryCache`（当前直查库）。
- 继续打磨移动端体验与空状态。
- 已完成：`Services` 仅 Shavit 查库（`IServices`/`Services`）；Steam/在线状态迁至 `SurfWeb.ServerStatus`。

---

## 11. 文档同步约定

只要出现以下变化，本文件必须同轮更新：
- API 路径变化。
- 前端目录或运行方式变化。
- 后端项目划分（Api/Data）或 Data 层目录约定变化。
- 本地默认端口、环境变量、配置入口变化。

禁止新建第二份总设计文档；所有总设计信息统一维护在本文件。
