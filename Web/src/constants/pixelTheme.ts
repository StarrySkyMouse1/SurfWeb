/** 像素简约主题 — 与 docs/design-previews/ui-pixel-minimal.html 对齐 */

export const PX_PANEL_CLASS = 'px-panel' as const
export const PX_PANEL_SM_CLASS = 'px-panel-sm' as const
export const PX_TABLE_HEAD_CLASS = 'px-table-head' as const
export const PX_ROW_HOVER_CLASS = 'px-row-hover' as const
export const PX_BTN_CLASS = 'px-btn' as const
export const PX_BTN_ACTIVE_CLASS = 'px-btn-active' as const
export const PX_CHIP_CLASS = 'px-chip' as const
export const PX_CHIP_ACTIVE_CLASS = 'px-chip-active' as const
export const PX_INPUT_CLASS = 'px-input' as const
export const PX_PAGE_TITLE_CLASS = 'px-page-title' as const
export const PX_PAGE_HEADING_CLASS = 'px-page-heading' as const
/** 中文标题旁的英文像素小字 */
export const PX_EN_SUBTITLE_CLASS = 'font-pixel text-[9px] text-px-muted' as const
export const PX_SECTION_TITLE_CLASS = 'px-section-title' as const
export const PX_RANK_CELL_CLASS = 'px-rank-cell' as const
export const PX_ERROR_BOX_CLASS = 'px-panel-sm p-4 text-sm' as const
export const PX_CARD_INTERACTIVE_CLASS = 'px-panel-sm px-card-interactive' as const

const TIER_CHIP_COLOR: Record<number, string> = {
  0: 'text-neutral-600 border-neutral-600',
  1: 'text-sky-700 border-sky-700',
  2: 'text-emerald-700 border-emerald-700',
  3: 'text-lime-700 border-lime-700',
  4: 'text-violet-700 border-violet-700',
  5: 'text-amber-700 border-amber-700',
  6: 'text-red-700 border-red-700',
  7: 'text-rose-800 border-rose-800',
  8: 'text-fuchsia-800 border-fuchsia-800',
}

export function tierChipColorClass(tier: number): string {
  return TIER_CHIP_COLOR[tier] ?? 'text-px-muted border-px-ink'
}
