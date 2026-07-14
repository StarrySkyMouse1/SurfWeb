/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string
  readonly VITE_SITE_TITLE?: string
  readonly VITE_SITE_OPS?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
