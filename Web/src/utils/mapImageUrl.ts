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
