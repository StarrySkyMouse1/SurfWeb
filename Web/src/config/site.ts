/** 站点展示名；构建时由 `VITE_SITE_TITLE` 注入，缺省为「地满滑翔」。 */
export const siteTitle = import.meta.env.VITE_SITE_TITLE?.trim() || '地满滑翔'

/** 网站运维署名；由 `VITE_SITE_OPS` 注入，空则不显示页脚运维行。 */
export const siteOps = import.meta.env.VITE_SITE_OPS?.trim() || ''

export const documentTitle = `${siteTitle} · Surf Record`
