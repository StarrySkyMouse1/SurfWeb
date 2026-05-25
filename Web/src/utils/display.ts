export interface MapImageConfig {
  baseUrl: string | null
  extension: string
}

/** 图床 BaseUrl + 地图名 + Extension；未配置 baseUrl 时返回 null */
export function buildMapImageUrl(map: string, config: MapImageConfig | null | undefined): string | null {
  const base = config?.baseUrl?.trim()
  if (!base) return null
  const prefix = base.endsWith('/') ? base : `${base}/`
  const ext = config?.extension ?? ''
  return `${prefix}${map}${ext}`
}

/** 地图 Tier 芯片颜色类（定义见 styles/tier.css `.tier-chip-*`） */
export function tierChipColorClass(tier: number): string {
  return tier >= 0 && tier <= 8 ? `tier-chip-${tier}` : 'tier-chip-default'
}

/** 分页表格骨架行数：与当前页实际条数一致 */
export function skeletonRowsForPage(
  page: number,
  pageSize: number,
  options: { total?: number | null; fallback?: number } = {},
): number {
  const fallback = options.fallback ?? pageSize
  const total = options.total
  if (total == null || total < 0) return fallback
  if (total === 0) return 0

  const remaining = total - (page - 1) * pageSize
  if (remaining <= 0) return 0
  return Math.min(pageSize, remaining)
}

/** 无限滚动网格：按剩余未加载条数 */
export function skeletonGridCount(loadedCount: number, total: number, batchSize: number): number {
  if (total <= 0) return batchSize
  const remaining = total - loadedCount
  if (remaining <= 0) return 0
  return Math.min(batchSize, remaining)
}

/** 从 `connect host:port` 或 `host:port` 解析端点 */
export function parseServerEndpoint(address: string): { host: string; port: number } | null {
  let text = address.trim()
  if (text.toLowerCase().startsWith('connect ')) {
    text = text.slice('connect '.length).trim()
  }

  const colon = text.lastIndexOf(':')
  if (colon <= 0 || colon >= text.length - 1) return null

  const host = text.slice(0, colon).trim()
  const port = Number.parseInt(text.slice(colon + 1).trim(), 10)
  if (!host || !Number.isFinite(port) || port <= 0 || port > 65535) return null

  return { host, port }
}

/** Steam 协议连接串（需本机已安装 Steam 客户端） */
export function buildSteamConnectUrl(address: string): string | null {
  const ep = parseServerEndpoint(address)
  if (!ep) return null
  return `steam://connect/${ep.host}:${ep.port}`
}

export function joinServer(address: string): void {
  const url = buildSteamConnectUrl(address)
  if (!url) return
  window.location.href = url
}
