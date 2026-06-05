import type { LeaderboardEntry, MapCheckpointChart, MapCheckpointSeries } from '../api/client'

/** 色轮：红(暖) → 冷；名次越高越亮 */
export const MAP_CP_RANK_COLORS = [
  '#fa5252', '#ff922b', '#fcc419', '#ced94d', '#82c91e',
  '#37b24d', '#15aabf', '#339af0', '#4c6ef5', '#364fc7',
] as const

function cpSecAt(series: MapCheckpointSeries, cpIndex: number): number | null {
  const v = series.cumulativeSeconds[cpIndex]
  return v == null || Number.isNaN(v) ? null : v
}

export function fastestSecAtCp(series: MapCheckpointSeries[], cpIndex: number): number {
  let min = Infinity
  for (const s of series) {
    const v = cpSecAt(s, cpIndex)
    if (v != null && v < min) min = v
  }
  return min === Infinity ? 0 : min
}

export function formatCpAxisTick(sec: number): string {
  const m = Math.floor(sec / 60)
  const s = Math.round(sec % 60)
  return `${m}:${String(s).padStart(2, '0')}`
}

export function computeCpYMax(chart: MapCheckpointChart): number {
  let maxSec = 0
  for (const s of chart.series) {
    for (const sec of s.cumulativeSeconds) {
      if (sec == null || Number.isNaN(sec)) continue
      maxSec = Math.max(maxSec, sec)
    }
  }
  const padded = maxSec * 1.04
  return Math.max(Math.ceil(padded / 15) * 15, 60)
}

export function bonusTrackLabel(track: number): string {
  return `b${track}`
}

export function stageLabel(stage: number): string {
  return `S${stage}`
}

export function hasCheckpointSeriesData(series: MapCheckpointSeries): boolean {
  return series.cumulativeSeconds.some((v) => v != null && !Number.isNaN(v))
}

export function leaderboardTimeByAuth(rows: readonly LeaderboardEntry[]): Map<number, number> {
  const map = new Map<number, number>()
  for (const row of rows) map.set(row.auth, row.time)
  return map
}

/** 检查点 tooltip 差距：中间 CP 用 cptimes 当场最快；终点与主线榜一致用 playertimes。 */
export function cpTooltipGapSeconds(
  cpLabel: string,
  seriesIndex: number,
  cpIndex: number,
  cpValue: number,
  chartSeries: MapCheckpointSeries[],
  leaderTime: number | null,
  finishTimeByAuth: ReadonlyMap<number, number>,
): number {
  if (cpLabel === '终点' && leaderTime != null) {
    const auth = chartSeries[seriesIndex]?.auth
    const finishTime = auth != null ? finishTimeByAuth.get(auth) : undefined
    if (finishTime != null) {
      if (chartSeries[seriesIndex]?.rank === 1) return 0
      return Math.max(0, finishTime - leaderTime)
    }
  }
  return cpValue - fastestSecAtCp(chartSeries, cpIndex)
}

export function cpSeriesDisplayName(
  series: MapCheckpointSeries,
  finishTimeByAuth: ReadonlyMap<number, number>,
  nameByAuth: ReadonlyMap<number, string>,
): string {
  const fromRow = nameByAuth.get(series.auth)
  if (fromRow) return fromRow
  if (series.playerName) return series.playerName
  return `#${series.auth}`
}

/** 与 map-detail 主线双栏断点一致 */
export const MAP_CP_CHART_MOBILE_MQ = '(max-width: 1023px)'

type TooltipPositionSize = {
  contentSize: [number, number]
  viewSize: [number, number]
}

/** 窄屏时把 tooltip 压在十字线下方，避免顶到页面 sticky 顶栏 */
export function cpChartTooltipPosition(
  point: [number, number],
  _params: unknown,
  _el: HTMLElement | null,
  _rect: unknown,
  size: TooltipPositionSize,
): [number, number] {
  const mobile = typeof window !== 'undefined'
    && window.matchMedia(MAP_CP_CHART_MOBILE_MQ).matches
  if (!mobile) return point

  const [cw, ch] = size.contentSize
  const [vw, vh] = size.viewSize
  const pad = 8
  let left = point[0] - cw / 2
  left = Math.max(pad, Math.min(left, vw - cw - pad))

  let top = point[1] + 14
  if (top + ch > vh - pad) top = Math.max(pad, point[1] - ch - 14)
  top = Math.max(pad, Math.min(top, vh - ch - pad))
  return [left, top]
}
