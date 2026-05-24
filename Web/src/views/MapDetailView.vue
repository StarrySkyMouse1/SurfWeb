<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import LeaderboardTable from '../components/LeaderboardTable.vue'
import PaginationBar from '../components/PaginationBar.vue'
import SkeletonMapDetailHeader from '../components/skeleton/SkeletonMapDetailHeader.vue'
import SkeletonMapDetailTabs from '../components/skeleton/SkeletonMapDetailTabs.vue'
import MapPreviewImage from '../components/MapPreviewImage.vue'
import { useMapImageConfig } from '../composables/useMapImageConfig'
import { apiGet, type LeaderboardEntry, type MapDetail } from '../api/client'
import { tierChipColorClass } from '../constants/pixelTheme'
import { LEADERBOARD_PAGE_SIZE } from '../constants/tableCell'

const { config: mapImageConfig } = useMapImageConfig()

const props = defineProps<{ name: string }>()

const pageSize = LEADERBOARD_PAGE_SIZE

const detail = ref<MapDetail | null>(null)
const rows = ref<LeaderboardEntry[]>([])
const leaderTime = ref<number | null>(null)
const total = ref(0)
const page = ref(1)
const track = ref(0)
const detailLoading = ref(true)
const leaderboardLoading = ref(false)
const error = ref('')
const leaderboardError = ref('')
/** 首次进入本图时已展示过完整内容；换图重置 */
const hasShownLeaderboard = ref(false)

const showLeaderboardSkeleton = () => detailLoading.value || leaderboardLoading.value
const showHeaderSkeleton = () =>
  detailLoading.value || (leaderboardLoading.value && !hasShownLeaderboard.value)

async function loadDetail() {
  const mapName = decodeURIComponent(props.name)
  const d = await apiGet<MapDetail>(`/maps/${encodeURIComponent(mapName)}`)
  detail.value = d.data ?? null
  if (!detail.value) {
    error.value = '地图不存在'
    return
  }
  const tracks = detail.value.bonusTracks ?? []
  if (track.value !== 0 && !tracks.includes(track.value)) {
    track.value = 0
  }
}

async function loadLeaderboard() {
  leaderboardLoading.value = true
  leaderboardError.value = ''
  try {
    const mapName = decodeURIComponent(props.name)
    const path = `/maps/${encodeURIComponent(mapName)}/leaderboard`
    const params = { track: track.value, page: page.value, pageSize }

    if (page.value === 1) {
      const lb = await apiGet<LeaderboardEntry[]>(path, params)
      rows.value = lb.data ?? []
      total.value = lb.meta?.total ?? 0
      leaderTime.value = rows.value.find((r) => r.rank === 1)?.time ?? null
    } else {
      const [lb, first] = await Promise.all([
        apiGet<LeaderboardEntry[]>(path, params),
        apiGet<LeaderboardEntry[]>(path, { track: track.value, page: 1, pageSize: 1 }),
      ])
      rows.value = lb.data ?? []
      total.value = lb.meta?.total ?? 0
      leaderTime.value = first.data?.[0]?.time ?? null
    }
  } catch (lbErr) {
    rows.value = []
    total.value = 0
    leaderTime.value = null
    leaderboardError.value = lbErr instanceof Error ? lbErr.message : '排行榜加载失败'
  } finally {
    leaderboardLoading.value = false
    if (!leaderboardError.value) hasShownLeaderboard.value = true
  }
}

async function load() {
  detailLoading.value = true
  leaderboardLoading.value = false
  error.value = ''
  leaderboardError.value = ''
  try {
    await loadDetail()
    detailLoading.value = false
    if (detail.value) {
      leaderboardLoading.value = true
      await loadLeaderboard()
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : '加载失败'
    detailLoading.value = false
    leaderboardLoading.value = false
  }
}

watch(() => props.name, () => {
  page.value = 1
  track.value = 0
  hasShownLeaderboard.value = false
  load()
})

watch(track, () => {
  page.value = 1
  rows.value = []
  if (detail.value && !detailLoading.value) {
    loadLeaderboard()
  }
})

watch(page, () => {
  if (detail.value && !detailLoading.value) {
    loadLeaderboard()
  }
})

onMounted(load)
</script>

<template>
  <section class="space-y-6">
    <p v-if="error && !detailLoading" class="px-panel-sm p-4">{{ error }}</p>

    <template v-else>
      <div class="px-panel overflow-hidden">
        <SkeletonMapDetailHeader v-if="showHeaderSkeleton()" />
        <div v-else-if="detail" class="flex flex-col lg:flex-row lg:items-stretch">
          <div class="min-w-0 flex-1 p-6">
            <div class="flex flex-wrap items-center gap-3">
              <span :class="['px-chip bg-px-surface', tierChipColorClass(detail.tier)]">
                T{{ detail.tier }}
              </span>
              <h1 class="text-2xl font-black break-all">{{ detail.map }}</h1>
            </div>
            <p class="mt-4 font-mono text-sm">
              WR {{ detail.worldRecordTimeFormatted ?? '—' }}
              <span v-if="detail.worldRecordPlayer"> · {{ detail.worldRecordPlayer }}</span>
            </p>
            <p class="mt-1 text-sm text-px-muted">{{ detail.completions }} 人完赛</p>
          </div>
          <MapPreviewImage
            :map="String(detail.map)"
            :image-config="mapImageConfig"
            variant="detail"
          />
        </div>
      </div>

      <div class="flex min-h-[2.375rem] flex-wrap items-center gap-2">
        <SkeletonMapDetailTabs v-if="showHeaderSkeleton()" />
        <template v-else-if="detail">
          <button
            type="button"
            class="px-chip px-4 py-1.5"
            :class="track === 0 ? 'px-chip-active' : ''"
            @click="track = 0"
          >
            主线
          </button>
          <button
            v-for="bonusTrack in detail.bonusTracks ?? []"
            :key="bonusTrack"
            type="button"
            class="px-chip px-4 py-1.5"
            :class="track === bonusTrack ? 'px-chip-active' : ''"
            @click="track = bonusTrack"
          >
            Bonus {{ bonusTrack }}
          </button>
        </template>
      </div>

      <p v-if="leaderboardError && !showLeaderboardSkeleton()" class="px-panel-sm p-4 text-sm">
        {{ leaderboardError }}
      </p>
      <div v-else class="px-panel overflow-hidden">
        <LeaderboardTable
          v-if="detail || showLeaderboardSkeleton()"
          bare
          :rows="rows"
          :page-size="pageSize"
          :leader-time="leaderTime"
          :loading="showLeaderboardSkeleton()"
        />
        <PaginationBar
          v-model:page="page"
          attached
          :page-size="pageSize"
          :total="total"
          :loading="showLeaderboardSkeleton()"
        />
      </div>
    </template>
  </section>
</template>
