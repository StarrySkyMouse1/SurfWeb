/** 首页左右列表：固定 10 行槽位，保证外框与分页栏对齐 */
export const HOME_LIST_PAGE_SIZE = 10 as const

/** 单行高度（10 行 ≈ 35rem，不含表头；容纳最新记录双行时间/地图） */
export const HOME_LIST_ROW_HEIGHT_CLASS = 'h-14' as const

export const HOME_LIST_ROW_CLASS =
  `${HOME_LIST_ROW_HEIGHT_CLASS} ${'px-row-hover'}` as const

export const HOME_LIST_ROW_EMPTY_CLASS =
  `${HOME_LIST_ROW_HEIGHT_CLASS} border-t border-px-ink` as const

export const HOME_LIST_PANEL_CLASS = 'px-panel flex flex-col overflow-hidden' as const

/** 表体区：裁剪横向溢出，分页栏下方不出现横向滚动条 */
export const HOME_LIST_TABLE_WRAP_CLASS = 'min-w-0 overflow-x-hidden' as const

export const HOME_LIST_HEAD_CLASS = 'py-2 px-3 font-semibold text-xs' as const

export const HOME_LIST_HEAD_PIXEL_CLASS = 'py-2 font-pixel text-[10px] font-normal' as const

export const HOME_LIST_CELL_CLASS = 'px-3 align-middle' as const

export const HOME_RANK_CELL_CLASS = 'px-rank-cell align-middle' as const

/** 与 AppHeader sticky 顶栏错开（约 4.25rem） */
export const SITE_HEADER_STICKY_OFFSET_CLASS = 'top-[4.25rem]' as const

/** 首页栏目标题（排行 / 最新记录） */
export const HOME_COLUMN_TITLE_CLASS = 'text-lg font-bold leading-none shrink-0' as const

export const HOME_COLUMN_HEADER_CLASS =
  'mb-3 flex items-end justify-between gap-2' as const

/** 首页双栏表格区 */
export const HOME_TABLES_GRID_CLASS =
  'grid grid-cols-1 items-stretch gap-6 lg:grid-cols-2 lg:gap-8' as const

export const SERVER_PANEL_HEAD_CLASS =
  `shrink-0 px-table-head sticky ${SITE_HEADER_STICKY_OFFSET_CLASS} z-40 lg:static lg:z-auto` as const

export const SERVER_PANEL_TITLE_CLASS =
  'px-3 py-2 text-left text-sm font-bold leading-none' as const

export const SERVER_PANEL_BODY_HEIGHT_CLASS = 'h-[240px] shrink-0 overflow-y-auto' as const

export const SERVER_PANEL_FIXED_CLASS = 'lg:border-t-0' as const

/** 服务器页：左右两张独立卡片（2/3 地图 + 1/3 玩家列表） */
export const SERVER_MAP_PLAYERS_GRID_CLASS =
  'grid grid-cols-1 gap-4 sm:grid-cols-3 sm:items-stretch' as const

export const SERVER_MAP_CARD_CLASS =
  'px-panel-sm col-span-1 flex min-h-0 flex-col overflow-hidden sm:col-span-2' as const

export const SERVER_PLAYERS_CARD_CLASS =
  'px-panel-sm col-span-1 flex min-h-0 min-w-0 flex-col overflow-hidden' as const

export const SERVER_MAP_FRAME_CLASS =
  'relative aspect-[16/9] w-full px-map-placeholder' as const

export const SERVER_PLAYERS_HEAD_CLASS =
  'shrink-0 border-b-2 border-px-ink bg-px-ink px-3 py-2 text-xs font-semibold text-px-surface' as const

export const SERVER_PLAYERS_LIST_CLASS =
  'max-h-36 min-h-[8rem] flex-1 overflow-y-auto divide-y border-px-ink sm:max-h-40' as const

export const SERVER_PLAYER_ROW_CLASS =
  'flex items-center justify-between gap-2 px-3 py-2 text-sm' as const
