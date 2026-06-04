import type { MapCheckpointChart, MapCheckpointSeries } from '../api/client'

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
