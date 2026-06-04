const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api/v1'

export interface ApiMeta {
  page: number
  pageSize: number
  total: number
}

export interface ApiResponse<T> {
  data?: T
  meta?: ApiMeta
  error?: { code: string; message: string }
}

export async function apiGet<T>(path: string, params?: Record<string, string | number | undefined>): Promise<ApiResponse<T>> {
  const url = new URL(`${baseUrl}${path}`)
  if (params) {
    for (const [k, v] of Object.entries(params)) {
      if (v !== undefined && v !== '') url.searchParams.set(k, String(v))
    }
  }
  const res = await fetch(url.toString())
  const raw = (await res.json()) as ApiResponse<T> & {
    Data?: T
    Meta?: ApiMeta
    Error?: ApiResponse<T>['error']
  }
  const err = raw.error ?? raw.Error
  if (!res.ok && !err) {
    throw new Error(res.statusText)
  }
  if (err) throw new Error(err.message)
  return {
    data: raw.data ?? raw.Data,
    meta: raw.meta ?? raw.Meta,
    error: err,
  }
}

export interface MapListItem {
  map: string
  tier: number
  completions: number
  worldRecordTime?: number
  worldRecordTimeFormatted?: string
  worldRecordPlayer?: string
}

export interface MapDetail {
  map: string
  tier: number
  maxVelocity: number
  completions: number
  worldRecordTime?: number
  worldRecordTimeFormatted?: string
  worldRecordPlayer?: string
  worldRecordAuth?: number
  bonusTracks: number[]
  stages: number[]
}

export interface MapCheckpointSeries {
  rank: number
  auth: number
  playerName?: string
  cumulativeSeconds: (number | null)[]
}

export interface MapCheckpointChart {
  checkpointLabels: string[]
  series: MapCheckpointSeries[]
}

export interface LeaderboardEntry {
  rank: number
  auth: number
  playerName?: string
  time: number
  timeFormatted: string
  sync?: number
  jumps?: number
  date?: string
}

export type PlayerRecordCategory = 'recent' | 'wr' | 'incomplete'
export type PlayerRecordScope = 'main' | 'stage' | 'bonus'

export interface PlayerSummary {
  auth: number
  name?: string
  points: number
  pointsRank: number
  playtime: number
  playtimeRank: number
  mainCompletionCount: number
  mainCompletionRank: number
  bonusCompletionCount: number
  bonusCompletionRank: number
  wrCount: number
  wrRank: number
  mainWrCount: number
  mainWrRank: number
  stageWrCount: number
  stageWrRank: number
  bonusWrCount: number
  bonusWrRank: number
}

export interface PlayerRecord {
  map: string
  tier?: number
  track?: number
  stage?: number
  time?: number
  timeFormatted?: string
  sync?: number
  date?: string
  /** 全服该图/赛道/阶段最快时间；仅近期记录 */
  worldRecordTime?: number
  /** 与全服 WR 的秒差；近期记录 Tab 由后端填充；持 WR 时为 0 */
  gapFromWr?: number
  status?: string
}

export interface PlayerChartBar {
  label: string
  value: number
}

export interface PlayerCharts {
  primaryTitle: string
  tierTitle: string
  primaryBars: PlayerChartBar[]
  tierBars: PlayerChartBar[]
  rangeTotal: number
  topTierLabel?: string
  primaryFooterLeft?: string
  primaryFooterRight?: string
}

export interface PlayerRecordsPage {
  items: PlayerRecord[]
  charts: PlayerCharts
}

export interface RecentRecord {
  id: number
  auth: number
  playerName?: string
  map: string
  tier?: number
  style: number
  track: number
  stage?: number
  time: number
  timeFormatted: string
  date?: string
  worldRecordTime?: number
  gapFromWr?: number
}

export interface RankingEntry {
  rank: number
  auth: number
  name?: string
  value: number
}
