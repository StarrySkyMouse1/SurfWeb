/** 从 `connect host:port` 或 `host:port` 解析端点 */
export function parseServerEndpoint(address: string): { host: string; port: number } | null {
  let text = address.trim()
  if (text.toLowerCase().startsWith('connect ')) {
    text = text.slice('connect '.length).trim()
  }

  const colon = text.lastIndexOf(':')
  if (colon <= 0 || colon >= text.length - 1) return null

  const host = text.slice(0, colon).trim()
  const port = Number.parseInt(text.slice(colon + 1).trim(), 10)
  if (!host || !Number.isFinite(port) || port <= 0 || port > 65535) return null

  return { host, port }
}

/** Steam 协议连接串（需本机已安装 Steam 客户端） */
export function buildSteamConnectUrl(address: string): string | null {
  const ep = parseServerEndpoint(address)
  if (!ep) return null
  return `steam://connect/${ep.host}:${ep.port}`
}

export function joinServer(address: string): void {
  const url = buildSteamConnectUrl(address)
  if (!url) return
  window.location.href = url
}
