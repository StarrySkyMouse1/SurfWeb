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
