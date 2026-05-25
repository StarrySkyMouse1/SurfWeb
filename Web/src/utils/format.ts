/** 与后端 RecordQueryService / PlayerQueryService 一致的 WR 容差（秒） */
export const WR_GAP_EPSILON = 0.001

/** API `date`（ISO 8601）→ 列表用本地时间文案 */
export function formatRecordDate(iso?: string): string {
  if (!iso) return ''
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  return d.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
}

/** 与 WR / 第一名的时间差（秒），如 +0.27、+0.012 */
export function formatTimeGap(deltaSeconds: number, decimals = 2): string {
  const d = Math.max(0, deltaSeconds)
  return `+${d.toFixed(decimals)}`
}

/** Shavit `playtime`（秒）→ `X天 X小时 X分 X秒`，省略为 0 的高位单位 */
export function formatPlaytime(totalSeconds: number): string {
  let s = Math.max(0, Math.floor(totalSeconds))
  const days = Math.floor(s / 86400)
  s %= 86400
  const hours = Math.floor(s / 3600)
  s %= 3600
  const minutes = Math.floor(s / 60)
  const seconds = s % 60

  const parts: string[] = []
  if (days > 0) parts.push(`${days}天`)
  if (hours > 0) parts.push(`${hours}小时`)
  if (minutes > 0) parts.push(`${minutes}分`)
  if (seconds > 0 || parts.length === 0) parts.push(`${seconds}秒`)
  return parts.join(' ')
}

/** 该条成绩是否为当前地图/赛道 WR */
export function isRecordWr(record: {
  time: number
  worldRecordTime?: number
  gapFromWr?: number
}): boolean {
  if (record.gapFromWr != null) return record.gapFromWr <= WR_GAP_EPSILON

  const wr = record.worldRecordTime
  if (wr == null) return false
  return Math.abs(record.time - wr) <= WR_GAP_EPSILON
}

/** 是否展示与 WR 的时间差（排除 WR 及 +0.000 级噪声） */
export function shouldShowGapFromWr(record: { gapFromWr?: number }): boolean {
  const gap = record.gapFromWr
  return gap != null && gap > WR_GAP_EPSILON
}
