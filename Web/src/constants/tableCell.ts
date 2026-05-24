export const TABLE_DATA_CELL_CLASS = 'py-2 px-3 align-middle' as const

export const TABLE_CELL_CONTENT_CLASS = 'flex min-h-5 flex-col justify-center leading-tight' as const

export const TABLE_CELL_SECONDARY_LINE_CLASS = 'mt-0.5 text-xs leading-tight' as const

export const TABLE_HEAD_CELL_CLASS = 'py-2 px-3 font-semibold text-xs' as const

export const TABLE_HEAD_PIXEL_CLASS = 'py-2 font-pixel text-[10px] font-normal' as const

export const TABLE_BODY_ROW_CLASS = 'px-row-hover' as const

export const TABLE_SKELETON_BODY_ROW_CLASS = 'border-t border-px-ink' as const

/** 地图详情排行榜：固定每页行数，骨架屏与数据表同高 */
export const LEADERBOARD_PAGE_SIZE = 10 as const

export const LEADERBOARD_ROW_HEIGHT_CLASS = 'h-14' as const

export const LEADERBOARD_ROW_CLASS =
  `${LEADERBOARD_ROW_HEIGHT_CLASS} ${TABLE_BODY_ROW_CLASS}` as const

export const LEADERBOARD_ROW_EMPTY_CLASS =
  `${LEADERBOARD_ROW_HEIGHT_CLASS} border-t border-px-ink` as const

/** 玩家页完赛地图列表：与排行榜相同固定行数/行高 */
export const COMPLETIONS_PAGE_SIZE = LEADERBOARD_PAGE_SIZE

export const COMPLETIONS_ROW_CLASS = LEADERBOARD_ROW_CLASS

export const COMPLETIONS_ROW_EMPTY_CLASS = LEADERBOARD_ROW_EMPTY_CLASS
