/** 与后端 RecordQueryService / PlayerQueryService 一致的 WR 容差（秒） */
export const WR_GAP_EPSILON = 0.001

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
