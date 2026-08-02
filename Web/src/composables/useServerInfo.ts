import { onMounted, onUnmounted, ref } from 'vue'
import { apiGet } from '../api/client'

export interface ServerOnlinePlayer {
  name: string
  auth?: number | null
  durationSeconds: number
  durationDisplay: string
}

export interface ServerInfo {
  name: string
  address: string
  online: boolean
  map?: string
  mapTier?: number | null
  players: number
  maxPlayers: number
  note?: string
  /** Steam AppID；未填 / 0 时「加入」用 steam://connect */
  steamAppId: number
  onlinePlayers: ServerOnlinePlayer[]
}

const POLL_MS = 30_000

async function fetchServers(): Promise<ServerInfo[]> {
  const res = await apiGet<ServerInfo[]>('/servers')
  return (res.data ?? []).map((s) => ({
    name: s.name,
    address: s.address,
    online: s.online ?? false,
    map: s.map,
    mapTier: s.mapTier,
    players: s.players ?? 0,
    maxPlayers: s.maxPlayers ?? 0,
    note: s.note,
    steamAppId: s.steamAppId ?? 0,
    onlinePlayers: (s.onlinePlayers ?? []).map((p) => ({
      name: p.name,
      auth: p.auth,
      durationSeconds: p.durationSeconds,
      durationDisplay: p.durationDisplay,
    })),
  }))
}

export function useServerInfo() {
  const servers = ref<ServerInfo[]>([])
  const loading = ref(true)
  const error = ref('')
  let pollTimer: ReturnType<typeof setInterval> | undefined

  async function refresh(silent = false) {
    if (!silent) {
      loading.value = true
      error.value = ''
    }
    try {
      servers.value = await fetchServers()
    } catch (e) {
      if (!silent) {
        error.value = e instanceof Error ? e.message : '加载失败'
        servers.value = []
      }
    } finally {
      if (!silent) loading.value = false
    }
  }

  onMounted(async () => {
    await refresh(false)
    pollTimer = setInterval(() => void refresh(true), POLL_MS)
  })

  onUnmounted(() => {
    if (pollTimer !== undefined) clearInterval(pollTimer)
  })

  return { servers, loading, error }
}
