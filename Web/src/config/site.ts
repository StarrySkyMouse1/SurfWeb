/** 站点展示名；构建时由 `VITE_SITE_TITLE` 注入，缺省为「地满滑翔」。 */
export const siteTitle = import.meta.env.VITE_SITE_TITLE?.trim() || '地满滑翔'

export const documentTitle = `${siteTitle} · Surf Record`
