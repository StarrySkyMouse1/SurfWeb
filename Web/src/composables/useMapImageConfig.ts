import { onMounted, ref } from 'vue'
import { apiGet } from '../api/client'
import { buildMapImageUrl, type MapImageConfig } from '../utils/display'

let cached: MapImageConfig | null | undefined

async function loadMapImageConfig(): Promise<MapImageConfig | null> {
  if (cached !== undefined) return cached
  try {
    const res = await apiGet<MapImageConfig>('/config/map-images')
    cached = res.data ?? null
  } catch {
    cached = null
  }
  return cached
}

export function useMapImageConfig() {
  const config = ref<MapImageConfig | null>(null)

  onMounted(async () => {
    config.value = await loadMapImageConfig()
  })

  function mapImageUrl(map: string): string | null {
    return buildMapImageUrl(map, config.value)
  }

  return { config, mapImageUrl }
}
