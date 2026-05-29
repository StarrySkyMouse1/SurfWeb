<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import PaginationBar from '../components/PaginationBar.vue'
import { apiGet, type RankingEntry, type RecentRecord } from '../api/client'
import HomeRankingTable from './home/components/HomeRankingTable.vue'
import HomeRecentTable from './home/components/HomeRecentTable.vue'
import RankingFilter, {
  type CompletionRankingScope,
  type RankingFilterValue,
  type WrRankingScope,
} from './home/components/RankingFilter.vue'
import RecentRecordFilter, {
  type RecentBrowseFilter,
  type RecentFilterMode,
  type RecentWrScope,
} from './home/components/RecentRecordFilter.vue'
import { useMapImageConfig } from '../composables/useMapImageConfig'
import { skeletonRowsForPage } from '../utils/display'

const { config: mapImageConfig } = useMapImageConfig()

const pageSize = 10
const maxListTotal = 100

const recentMode = ref<RecentFilterMode>('browse')
const recentBrowseScope = ref<RecentBrowseFilter>('')
const recentWrScope = ref<RecentWrScope>('main')
const recent = ref<RecentRecord[]>([])
const recentTotal = ref(0)
const recentPage = ref(1)
const recentError = ref('')
const recentLoading = ref(true)
let recentLoadId = 0

const rankingsType = ref<RankingFilterValue>('points')
const rankingsWrScope = ref<WrRankingScope>('main')
const rankingsCompletionScope = ref<CompletionRankingScope>('main')
const rankings = ref<RankingEntry[]>([])
const rankingsTotal = ref(0)
const rankingsPage = ref(1)
const rankingsError = ref('')
const rankingsLoading = ref(true)
const rankingsTotalKnown = ref(false)
let rankingsLoadId = 0

const rankingsSkeletonRows = computed(() =>
  skeletonRowsForPage(rankingsPage.value, pageSize, {
    total: rankingsTotalKnown.value ? rankingsTotal.value : null,
    fallback: pageSize,
  }),
)

const recentTotalKnown = ref(false)

const recentSkeletonRows = computed(() =>
  skeletonRowsForPage(recentPage.value, pageSize, {
    total: recentTotalKnown.value ? recentTotal.value : null,
    fallback: pageSize,
  }),
)

const rankingSlots = computed(() =>
  Array.from({ length: pageSize }, (_, i) => rankings.value[i] ?? null),
)

const recentSlots = computed(() =>
  Array.from({ length: pageSize }, (_, i) => recent.value[i] ?? null),
)

async function loadRankings() {
  const id = ++rankingsLoadId
  rankingsLoading.value = true
  rankingsError.value = ''
  const page = rankingsPage.value
  try {
    const res = await apiGet<RankingEntry[]>('/rankings', {
      type: rankingsType.value,
      page,
      pageSize,
      ...(rankingsType.value === 'wr' ? { wrScope: rankingsWrScope.value } : {}),
      ...(rankingsType.value === 'completions'
        ? { completionScope: rankingsCompletionScope.value }
        : {}),
    })
    if (id !== rankingsLoadId) return

    rankings.value = res.data ?? []
    rankingsTotal.value = Math.min(res.meta?.total ?? 0, maxListTotal)
    rankingsTotalKnown.value = true

    const maxPage = Math.max(1, Math.ceil(rankingsTotal.value / pageSize))
    if (page > maxPage) {
      rankingsPage.value = maxPage
      return
    }
  } catch (e) {
    if (id !== rankingsLoadId) return
    rankings.value = []
    rankingsTotal.value = 0
    rankingsError.value = e instanceof Error ? e.message : '加载失败'
  } finally {
    if (id === rankingsLoadId) rankingsLoading.value = false
  }
}

async function loadRecent() {
  const id = ++recentLoadId
  recentLoading.value = true
  recentError.value = ''
  const page = recentPage.value
  try {
    const res = await apiGet<RecentRecord[]>('/records/recent', {
      page,
      pageSize,
      ...(recentMode.value === 'wr'
        ? { filter: 'wr', wrScope: recentWrScope.value }
        : recentBrowseScope.value
          ? { filter: recentBrowseScope.value }
          : {}),
    })
    if (id !== recentLoadId) return

    recent.value = res.data ?? []
    recentTotal.value = Math.min(res.meta?.total ?? 0, maxListTotal)
    recentTotalKnown.value = true

    const maxPage = Math.max(1, Math.ceil(recentTotal.value / pageSize))
    if (page > maxPage) {
      recentPage.value = maxPage
      return
    }
  } catch (e) {
    if (id !== recentLoadId) return
    recent.value = []
    recentTotal.value = 0
    recentError.value = e instanceof Error ? e.message : '加载失败'
  } finally {
    if (id === recentLoadId) recentLoading.value = false
  }
}

watch(rankingsPage, loadRankings)
watch([rankingsType, rankingsWrScope, rankingsCompletionScope], () => {
  rankingsPage.value = 1
  rankingsTotalKnown.value = false
  loadRankings()
})
watch(recentPage, loadRecent)
watch([recentMode, recentBrowseScope, recentWrScope], () => {
  recentPage.value = 1
  recentTotalKnown.value = false
  loadRecent()
})

loadRankings()
loadRecent()
</script>

<template>
  <section>
    <div class="px-home-tables-grid">
      <div class="flex w-full min-w-0 flex-col">
        <div class="px-home-column-header">
          <div class="flex min-w-0 items-end gap-2">
            <h2 class="px-home-column-title">排行</h2>
            <span class="px-en-subtitle">RANKING</span>
          </div>
          <RankingFilter
            v-model="rankingsType"
            v-model:completion-scope="rankingsCompletionScope"
            v-model:wr-scope="rankingsWrScope"
            class="shrink-0"
          />
        </div>
        <p v-if="rankingsError" class="px-error-box">{{ rankingsError }}</p>
        <div v-else class="px-home-list-panel">
          <HomeRankingTable
            :loading="rankingsLoading"
            :rows="rankingSlots"
            :ranking-type="rankingsType"
            :skeleton-rows="rankingsSkeletonRows"
            :page="rankingsPage"
            :page-size="pageSize"
          />
          <PaginationBar
            v-model:page="rankingsPage"
            attached
            :page-size="pageSize"
            :total="rankingsTotal"
            :loading="rankingsLoading"
          />
        </div>
      </div>

      <div class="flex w-full min-w-0 flex-col">
        <div class="px-home-column-header">
          <div class="flex min-w-0 items-end gap-2">
            <h2 class="px-home-column-title">最新记录</h2>
            <span class="px-en-subtitle">RECENT</span>
          </div>
          <RecentRecordFilter
            v-model:mode="recentMode"
            v-model:browse-scope="recentBrowseScope"
            v-model:wr-scope="recentWrScope"
            class="shrink-0"
          />
        </div>
        <p v-if="recentError" class="px-error-box">{{ recentError }}</p>
        <div v-else class="px-home-list-panel">
          <HomeRecentTable
            :loading="recentLoading"
            :rows="recentSlots"
            :skeleton-rows="recentSkeletonRows"
            :map-image-config="mapImageConfig"
          />
          <PaginationBar
            v-model:page="recentPage"
            attached
            :page-size="pageSize"
            :total="recentTotal"
            :loading="recentLoading"
          />
        </div>
      </div>
    </div>
  </section>
</template>
