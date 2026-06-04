import type { PlayerChartBar } from '../api/client'

export interface PaintedChartBar extends PlayerChartBar {
  heightPct: number
  tone: 'accent' | 'muted' | 'default'
  valueClass: string
}

export function paintChartBars(bars: PlayerChartBar[]): PaintedChartBar[] {
  if (bars.length === 0) return []

  const max = Math.max(...bars.map((b) => b.value), 1)
  const min = Math.min(...bars.map((b) => b.value))

  return bars.map((bar) => {
    const heightPct = max > 0 ? Math.max(8, (bar.value / max) * 100) : 8
    let tone: PaintedChartBar['tone'] = 'default'
    if (bar.value === max && max > 0) tone = 'accent'
    else if (bar.value === min) tone = 'muted'

    return {
      ...bar,
      heightPct,
      tone,
      valueClass: tone === 'accent' ? 'is-accent' : tone === 'muted' ? 'is-muted' : '',
    }
  })
}
