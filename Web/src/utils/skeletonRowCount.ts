/** 分页表格骨架行数：与当前页实际条数一致 */
export function skeletonRowsForPage(
  page: number,
  pageSize: number,
  options: { total?: number | null; fallback?: number } = {},
): number {
  const fallback = options.fallback ?? pageSize
  const total = options.total
  if (total == null || total < 0) return fallback
  if (total === 0) return 0

  const remaining = total - (page - 1) * pageSize
  if (remaining <= 0) return 0
  return Math.min(pageSize, remaining)
}

/** 无限滚动网格：按剩余未加载条数 */
export function skeletonGridCount(loadedCount: number, total: number, batchSize: number): number {
  if (total <= 0) return batchSize
  const remaining = total - loadedCount
  if (remaining <= 0) return 0
  return Math.min(batchSize, remaining)
}
