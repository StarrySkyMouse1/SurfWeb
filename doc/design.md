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
- 不在 v1 中实现检查点榜（`cptimes` / `cpwrs`）。

---

## 2. 当前实现摘要

- 后端：.NET 10，**Api + 五项目**（`Configurations`、`Utils`、`Repositories`、`Services`、`SurfWeb.ServerStatus`）。
- 前端：Vue 3 + Vite + Tailwind，目录位于 `Web/`。
- 数据源：Shavit MySQL，只读查询。
- 本地 API 开发地址：`http://localhost:5240`，HTTPS 地址：`https://localhost:7182`；开发环境提供 Swagger UI（`/swagger`）。
- 前端默认 API 地址：`http://localhost:5240/api/v1`。

当前核心能力：
- 地图列表、地图详情、排行榜分页。
- 玩家摘要、玩家成绩、玩家完赛地图。
- 全站排行榜、最新记录。
- 配置接口：样式、地图图床；服务器实时状态（Steam A2S + 后台刷新）。

---

## 3. 代码范围与目录

### 3.1 后端（解决方案项目）

| 项目 | 路径 | 职责 |
|------|------|------|
| `SurfWeb.Core` | `Server/SurfWeb.Core/` | `Models/`、`Dtos/`、`Enums/`、`Options/`、`Constants/`（`SiteLimits`） |
| `Configurations` | `Server/Configurations/` | `DependencyInjection/*`、CORS、`Middleware`、`Common/ApiResponse`（绑定 `SurfWeb.Core.Options`，无独立 Options 项目内目录） |
| `Utils` | `Server/Utils/` | 无状态工具与读侧缓存：`TimeFormatter`、`Caching/`（`CacheKeys`、`IQueryCache`、`QueryCache` 等）、服务器地址/地图名解析；`AddSurfWebQueryCache` |
| `Repositories` | `Server/Repositories/` | `ShavitDbContext`、`IBaseRepository<T>`、`AddSurfWebRepositories` |
| `Services` | `Server/Services/` | `IServices/*`、`Services/*`、`AddSurfWeb` / `AddSurfWebData` |
| `SurfWeb.ServerStatus` | `Server/SurfWeb.ServerStatus/` | `IServices/`、`Models/`、`Services/`、`Steam/`；**不**引用 `Repositories`，Shavit 补充经 `IMapService` / `IUserService` |
| `SurfWeb.Api` | `Server/SurfWeb.Api/` | Controller、组合根、`Program.cs` |

依赖方向：`SurfWeb.Core`（含 Options）← `Utils` ← `Repositories` ← `Services` ← `SurfWeb.ServerStatus` ← `SurfWeb.Api`；`Configurations` 引用 `Core` 负责 Options 绑定与横切中间件。`Program.cs`：`AddSurfWebWebHost` 等横切配置后，`AddSurfWeb`（仓储 + Services）、`AddSurfWebServerStatus`（Steam + 在线状态）。

### 3.2 前端

- `Web/`：Vue 3 单页应用。
- 路由：`/`、`/maps`、`/maps/:name`、`/players/:auth`、`/servers`。
- 兼容旧链接：`/rankings`、`/records` 重定向到首页。
- **目录约定：** 跨页复用 `Web/src/components/`（`AppHeader`、`PaginationBar`、`MapPreviewImage`、`skeleton/SkeletonBar`）；各页 UI 与骨架内聚在 `views/<feature>/components/`，**统一**通过组件 prop `loading` 在同一 DOM 壳内切换 `SkeletonBar` 占位（不再维护独立 `Skeleton*.vue` 表/卡片副本）。公共仅保留 `SkeletonBar` 原子占位条。

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
  - 支持 `type`（`RankingType` 枚举，查询参数仍为小写字符串，大小写不敏感）：`points`（积分）、`completions`（有成绩的不重复地图/赛道数）、`playtime`（在线时长，秒）、`wr`（持有 WR 条目的数量排行）；非法值返回 400；`page`、`pageSize`。
  - `type=completions` 时可选 `completionScope`（`TrackRankingScope` 枚举，大小写不敏感，缺省 `main`）：`main`（主线 `track=0` 每图计 1）、`bonus`（奖励 `track>0` 每图每赛道计 1）；两套独立缓存 key。
  - `type=wr` 时可选 `wrScope`（`WrRankingScope` 枚举，大小写不敏感，缺省 `main`）：`main`（主线 `track=0` 每图一条 WR）、`bonus`（奖励赛道 `track>0` 每图每赛道一条 WR）、`stage`（`stagetimes` 每图每赛道每阶段一条 WR）；三套独立缓存 key。
  - **读缓存：** `RankingService` 按 `type`（完成/WR 另按 scope）缓存全量 Top 100（`SiteLimits.MaxRankingsTotal`），`page`/`pageSize` 在内存切片；过期由 `SurfWeb:Cache:RankingsRefreshMinutes` 控制，**过期后下一次用户请求**触发重新查库（非后台定时任务）。

- `GET /records/recent`
  - 查询最新记录（仅 `playertimes`；不含 `stagetimes`）。缓存快照 `RecentRecordsSnapshot` 预计算四套列表，各最多 100 条、按**该条成绩的完成时间**降序：「全部」「主线」（`track=0`）、「奖励」（`track>0`）、「WR」（玩家打破 WR 的条目）。构建时在最近批次内按 `(玩家, 地图, track)` 去重，保留该玩家在该地图赛道上的**个人最快**一条（非全服地图最快），再取 Top 100。
  - 支持 `page`、`pageSize`，兼容 `limit`；`filter`（`RecentRecordFilter` 枚举，大小写不敏感，缺省全部）：`all` / `main` / `stage`（`stagetimes`）/ `bonus` / `wr`；`filter=wr` 时可选 `wrScope`（`WrRankingScope`）：`main` / `stage` / `bonus`；非法值 400；各列表独立 Top 100、按完成时间降序，内存分页。
  - 响应项含 `tier`（地图难度，来自 `MapTier`）；`stage` 字段保留于 DTO 但首页不返回阶段条目。
  - **读缓存：** 单 key `surfweb:records:recent`；过期由 `SurfWeb:Cache:RecentRefreshMinutes` 控制，懒刷新。

- `GET /config/map-images`
- `GET /config/servers`（静态配置摘要，不含在线玩家；前端服务器页使用 `GET /servers` 实时接口）

- `GET /servers`
  - 返回实时服务器状态（对齐旧版 `Steam/GetServerInfo` + 玩家列表）。
  - 查询参数：`refresh`（可选，`true` 时强制立即 Steam A2S 刷新）。
  - 响应字段：`name`、`address`、`online`、`map`、`mapTier`、`players`、`maxPlayers`、`note`、`onlinePlayers[]`（`name`、`auth`、`durationSeconds`、`durationDisplay`）。
  - 实现：`SurfWeb.ServerStatus` 中 `ServerStatusRefresher` 后台每 `SurfWeb:ServerQuery:RefreshSeconds` 秒 UDP 查询；`GET` 在缓存过期时懒刷新；Steam 逻辑移植自 `Server/参考/SurfWebDefault/Utils/Steam/SteamUtil.cs`；地图 Tier / 玩家 `auth` 经 `IMapService.GetMapTierByMapNameAsync`、`IUserService.GetAuthsByNamesAsync` 补充（失败时仍返回 Steam 数据）。

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

主题与布局样式：入口 `Web/src/style.css`（`@import "tailwindcss"` 后按模块引入 `Web/src/styles/`）；`theme.css` 设计令牌、`base.css` 页面底、`components/pixel-ui.css` 面板/按钮/芯片、`components/table.css` 表格、`components/pagination.css` 分页、`pages/home.css` 首页双栏、`pages/player.css` 玩家完赛表、`pages/server.css` 服务器页、`tier.css` Tier 色类；固定表格每页 10 行在相关 View/组件内写死。前端工具：`Web/src/utils/format.ts`（时间/成绩/WR 格式化）、`Web/src/utils/display.ts`（图床 URL、Tier 色类、骨架行数、Steam 进服）。

### 6.2 页面

- 首页：排行榜、最新记录；双栏 `items-start`（不按较高栏拉伸，避免表与分页之间留白），网格子项与 `.px-home-list-table-wrap > table` 均为 `w-full` + `table-fixed`，表体固定 10 行（`h-14` / `--px-home-table-block-h`），表格区高度随表头+行数自适应（`max-height` 上限 10 行），无固定留白；表格区 `overflow` 裁剪且不显示滚动条，行内容 `overflow-hidden` 不撑高页面；横向裁剪 `overflow-x-hidden`。`HomeRankingTable` / `HomeRecentTable` 在 `loading` 时于**同一** `colgroup`/表头下渲染 `SkeletonBar` 占位（末页行数由 `skeletonRowsForPage` 与数据态一致），底部分页始终为真实 `PaginationBar`。**最新记录**栏标题右侧为 `RecentRecordFilter`：浏览分栏芯片（`完成·全部` / `完成·主线` / `完成·阶段` / `完成·奖励`）+ WR 分栏芯片（`WR·主线` / `WR·阶段` / `WR·奖励`），切换时重置第 1 页；`filter` + 可选 `wrScope` 请求 `/records/recent`（阶段来自 `stagetimes`，WR 分范围与排行语义一致）。排行栏标题右侧 `RankingFilter`：积分 / 时长 + **完成**、**WR** 分栏芯片（左 `完成·主线` / `WR·主线` 等，右 ▼ 弹出范围；完成仅主线/奖励，WR 含主线/阶段/奖励）；`type=completions&completionScope=` / `type=wr&wrScope=` 请求 `/rankings`，切换时重置第 1 页；表头第三列随类型显示「积分」「完成」「时长」「WR」（时长列用 `formatPlaytime`：`X天 X小时`，不显示分/秒）。列宽：排行「名次 3rem / 玩家 / 数值列 7rem（时长筛选时为 11rem，`w-44`）」；最新记录「地图 42%（行高内左贴边缩略图 `MapPreviewImage` variant=`thumb`（`mask-image` 左实右透明渐变）；地图名单独占一行 `truncate`；第二行：左 `px-chip` Tier、右赛道类型小字（主线/Bn/阶段N，与 Tier 垂直居中）/ 玩家 / 时间 10rem」；**窄屏（≤640px）** 隐藏地图缩略图。
- 服务器页（`/servers`）：`ServerInfoPanel` 在 `loading` 时于同一面板壳内渲染单条服务器占位（地图框 + 玩家列表 + 加入按钮）；顶栏为当前地图名 + Tier + 在线/离线；`GET /servers` 每 30s 轮询。
- 地图页：`MapCard` 支持 `loading`，首屏/加载更多在同一网格内渲染占位卡片（与真实卡片同 DOM 结构）；WR 行 `min-h-4` 防高度跳动。
- 地图详情页：`MapDetailHeader`、`MapDetailTabs`、`LeaderboardTable` 均 `loading` 同壳切换；首次进入头图/Tab/表一并占位；Tab 预留「主线 + 最多 6 个 Bonus」。
- 玩家页：`PlayerProfileCard`、`PlayerCompletionsTable` 同壳 `loading`；完赛表 `table-fixed` + `colgroup`（地图 36% 含左贴边 `MapPreviewImage` thumb、Tier `px-chip`+`tierChipColorClass` 同 `MapCard` / 时间 / 同步 / 日期），`mapImageConfig` 来自 `useMapImageConfig`；末页行数由 `skeletonRowsForPage` 与数据态一致。

### 6.3 前端运行约定

- **骨架屏约定：** 各业务组件 `loading` 时在原位置插入 `SkeletonBar`（或缩略图方框），禁止再为同一块 UI 单独复制一份 `SkeletonXxx.vue` 表/卡；改布局只改业务组件一处。
- **组件路径：** `views/home/components/`（`HomeRankingTable`、`HomeRecentTable`、`RankingFilter`、`FilterSplitChip`（完成/WR/最新记录分栏芯片 + 范围气泡）、`RecentRecordFilter`）、`ChipFilter` / 筛选条共用 `.px-filter-chip-row`（同行垂直居中，选中无 `translate` 避免高低不齐）、`views/maps/components/`（`MapCard`、`TierFilter`）、`views/map-detail/components/`（`MapDetailHeader`、`MapDetailTabs`、`LeaderboardTable`）、`views/players/components/`（`PlayerProfileCard`、`PlayerCompletionsTable`）、`views/servers/components/`（`ServerInfoPanel`）。
- **单页应用（SPA）：** `vue-router` + `createWebHistory()`，导航统一用 `RouterLink`，仅 `joinServer` 使用 `steam://` 外链；`App.vue` 内 `RouterView` 带淡入淡出过渡；`scrollBehavior` 切换路由时平滑滚到顶部（浏览器后退恢复原滚动位置）。
- 全局布局：`App.vue` 使用 `min-h-screen` + `flex-col`，`main` 占满剩余高度，页脚 `border-t` 在内容较少时仍贴齐视口底部。
- `Web/.env.development` 默认：`VITE_API_BASE_URL=http://localhost:5240/api/v1`
- 本地前端开发端口默认由 Vite 管理，后端 CORS 允许 `localhost:5173` 与 `127.0.0.1:5173`。

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
- `ConnectionStrings:Shavit`
- `SurfWeb:CorsOrigins`
- `SurfWeb:MinResponseDelaySeconds`（读 API 最小响应秒数，默认 `0.2`）
- `SurfWeb:Cache`（`MapsMinutes`、`LeaderboardSeconds`；`RankingsRefreshMinutes`、`RecentRefreshMinutes` 默认 `1`）
- `SurfWeb:Styles`（仅服务端：解析 `DefaultStyleId` 并过滤 `playertimes`/`stagetimes`；无对外 HTTP 接口）
- `SurfWeb:MapImages`：`BaseUrl` 使用 `https://example.com/surf-map-images/` 作为模板占位，真实图床目录放入本地配置；前端拼图为 `{BaseUrl}{地图名}{Extension}`
- `SurfWeb:ServerQuery`：`RefreshSeconds`（后台 A2S 刷新间隔，默认 30）、`QueryTimeoutMs`（UDP 超时，默认 8000）
- `SurfWeb:Servers[]`：公开默认值使用 `127.0.0.1:27015` 作为模板占位；真实服务器放入本地配置。字段包含 `Name`、`Address`（`connect host:port` 或 `host:port`）、可选 `Host`/`Port` 覆盖、`MaxPlayers` 占位。

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
- 玩家页等其余读接口按需接入 `IQueryCache`。
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
