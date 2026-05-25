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

export interface PlayerSummary {
  auth: number
  name?: string
  points: number
  playtime: number
  completionCount: number
  pointsRank: number
  playtimeRank: number
  completionRank: number
}

export interface PlayerCompletion {
  map: string
  tier?: number
  time: number
  timeFormatted: string
  style: number
  sync?: number
  date?: string
  worldRecordTime?: number
  gapFromWr?: number
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
