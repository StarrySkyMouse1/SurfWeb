/** 与 WR / 第一名的时间差（秒），如 +0.27、+0.012 */
export function formatTimeGap(deltaSeconds: number, decimals = 2): string {
  const d = Math.max(0, deltaSeconds)
  return `+${d.toFixed(decimals)}`
}
