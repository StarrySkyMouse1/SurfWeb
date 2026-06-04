<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import MapDetailHeader from './components/MapDetailHeader.vue'
import MapDetailCategoryTabs, { type MapDetailCategory } from './components/MapDetailCategoryTabs.vue'
import MapDetailMainPanel from './components/MapDetailMainPanel.vue'
import MapDetailStageBonusPanel from './components/MapDetailStageBonusPanel.vue'
import { useMapImageConfig } from '../../composables/useMapImageConfig'
import {
  apiGet,
  type LeaderboardEntry,
  type MapCheckpointChart,
  type MapDetail,
} from '../../api/client'

const { config: mapImageConfig } = useMapImageConfig()

const props = defineProps<{ name: string }>()

const pageSize = 10

const detail = ref<MapDetail | null>(null)
const categoryTab = ref<MapDetailCategory>('main')

const mainRows = ref<LeaderboardEntry[]>([])
const mainTotal = ref(0)
const mainPage = ref(1)
const mainLeaderTime = ref<number | null>(null)
const mainLoading = ref(false)
const mainError = ref('')

const checkpointChart = ref<MapCheckpointChart | null>(null)
const checkpointLoading = ref(false)

const stageScope = ref<number | null>(null)
const stageRows = ref<LeaderboardEntry[]>([])
const stageTotal = ref(0)
const stagePage = ref(1)
const stageLeaderTime = ref<number | null>(null)
const stageLoading = ref(false)
const stageError = ref('')

const bonusScope = ref<number | null>(null)
const bonusRows = ref<LeaderboardEntry[]>([])
const bonusTotal = ref(0)
const bonusPage = ref(1)
const bonusLeaderTime = ref<number | null>(null)
const bonusLoading = ref(false)
const bonusError = ref('')

const detailLoading = ref(true)
const error = ref('')
const hasShownMain = ref(false)

const mapName = computed(() => decodeURIComponent(props.name))

const showHeaderSkeleton = computed(
  () => detailLoading.value || (mainLoading.value && !hasShownMain.value),
)

function mapPath(suffix: string) {
  return `/maps/${encodeURIComponent(mapName.value)}${suffix}`
}

async function loadLeaderboard(
  params: { track: number; stage?: number; page: number },
): Promise<{ rows: LeaderboardEntry[]; total: number; leaderTime: number | null }> {
  const query: Record<string, string | number> = {
    track: params.track,
    page: params.page,
    pageSize,
  }
  if (params.stage != null) query.stage = params.stage

  const lb = await apiGet<LeaderboardEntry[]>(mapPath('/leaderboard'), query)
  const rows = lb.data ?? []
  const total = lb.meta?.total ?? 0

  let leaderTime: number | null = rows.find((r) => r.rank === 1)?.time ?? null
  if (params.page > 1) {
    const first = await apiGet<LeaderboardEntry[]>(mapPath('/leaderboard'), {
      track: params.track,
      stage: params.stage,
      page: 1,
      pageSize: 1,
    })
    leaderTime = first.data?.[0]?.time ?? leaderTime
  }

  return { rows, total, leaderTime }
}

async function loadDetail() {
  const res = await apiGet<MapDetail>(mapPath(''))
  detail.value = res.data ?? null
  if (!detail.value) {
    error.value = '地图不存在'
    return
  }

  const stages = detail.value.stages ?? []
  if (!stages.length) stageScope.value = null
  else if (stageScope.value == null || !stages.includes(stageScope.value))
    stageScope.value = stages[0]

  const bonuses = detail.value.bonusTracks ?? []
  if (!bonuses.length) bonusScope.value = null
  else if (bonusScope.value == null || !bonuses.includes(bonusScope.value))
    bonusScope.value = bonuses[0]
}

async function loadCheckpoints() {
  checkpointLoading.value = true
  try {
    const res = await apiGet<MapCheckpointChart>(mapPath('/checkpoints'), { limit: 10 })
    checkpointChart.value = res.data ?? null
  } catch {
    checkpointChart.value = null
  } finally {
    checkpointLoading.value = false
  }
}

async function loadMain() {
  mainLoading.value = true
  mainError.value = ''
  try {
    const lb = await loadLeaderboard({ track: 0, page: mainPage.value })
    mainRows.value = lb.rows
    mainTotal.value = lb.total
    mainLeaderTime.value = lb.leaderTime
    hasShownMain.value = true
  } catch (e) {
    mainRows.value = []
    mainTotal.value = 0
    mainLeaderTime.value = null
    mainError.value = e instanceof Error ? e.message : '排行榜加载失败'
  } finally {
    mainLoading.value = false
  }
}

async function loadStage() {
  if (stageScope.value == null) {
    stageRows.value = []
    stageTotal.value = 0
    return
  }
  stageLoading.value = true
  stageError.value = ''
  try {
    const lb = await loadLeaderboard({
      track: 0,
      stage: stageScope.value,
      page: stagePage.value,
    })
    stageRows.value = lb.rows
    stageTotal.value = lb.total
    stageLeaderTime.value = lb.leaderTime
  } catch (e) {
    stageRows.value = []
    stageTotal.value = 0
    stageLeaderTime.value = null
    stageError.value = e instanceof Error ? e.message : '阶段榜加载失败'
  } finally {
    stageLoading.value = false
  }
}

async function loadBonus() {
  if (bonusScope.value == null) {
    bonusRows.value = []
    bonusTotal.value = 0
    return
  }
  bonusLoading.value = true
  bonusError.value = ''
  try {
    const lb = await loadLeaderboard({
      track: bonusScope.value,
      page: bonusPage.value,
    })
    bonusRows.value = lb.rows
    bonusTotal.value = lb.total
    bonusLeaderTime.value = lb.leaderTime
  } catch (e) {
    bonusRows.value = []
    bonusTotal.value = 0
    bonusLeaderTime.value = null
    bonusError.value = e instanceof Error ? e.message : '奖励榜加载失败'
  } finally {
    bonusLoading.value = false
  }
}

async function loadStageBonus() {
  await Promise.all([loadStage(), loadBonus()])
}

async function load() {
  detailLoading.value = true
  error.value = ''
  hasShownMain.value = false
  try {
    await loadDetail()
    detailLoading.value = false
    if (!detail.value) return
    await Promise.all([loadCheckpoints(), loadMain()])
    if (categoryTab.value === 'stage-bonus') await loadStageBonus()
  } catch (e) {
    error.value = e instanceof Error ? e.message : '加载失败'
    detailLoading.value = false
  }
}

watch(() => props.name, () => {
  mainPage.value = 1
  stagePage.value = 1
  bonusPage.value = 1
  categoryTab.value = 'main'
  stageScope.value = null
  bonusScope.value = null
  load()
})

watch(mainPage, () => {
  if (detail.value && !detailLoading.value) loadMain()
})

watch([stagePage, stageScope], () => {
  if (detail.value && !detailLoading.value && categoryTab.value === 'stage-bonus') loadStage()
})

watch([bonusPage, bonusScope], () => {
  if (detail.value && !detailLoading.value && categoryTab.value === 'stage-bonus') loadBonus()
})

watch(categoryTab, (tab) => {
  if (tab === 'stage-bonus' && detail.value && !detailLoading.value) loadStageBonus()
})

onMounted(load)
</script>

<template>
  <section class="space-y-6">
    <p v-if="error && !detailLoading" class="px-panel-sm p-4">{{ error }}</p>

    <template v-else>
      <MapDetailHeader
        :loading="showHeaderSkeleton"
        :detail="detail"
        :map-image-config="mapImageConfig"
      />

      <MapDetailCategoryTabs v-model="categoryTab" :loading="detailLoading" />

      <MapDetailMainPanel
        v-show="categoryTab === 'main'"
        v-model:page="mainPage"
        :checkpoint-chart="checkpointChart"
        :checkpoint-loading="checkpointLoading || detailLoading"
        :rows="mainRows"
        :total="mainTotal"
        :leader-time="mainLeaderTime"
        :main-loading="mainLoading || detailLoading"
        :main-error="mainError"
      />

      <MapDetailStageBonusPanel
        v-show="categoryTab === 'stage-bonus'"
        v-model:stage-scope="stageScope"
        v-model:bonus-scope="bonusScope"
        v-model:stage-page="stagePage"
        v-model:bonus-page="bonusPage"
        :stages="detail?.stages ?? []"
        :bonus-tracks="detail?.bonusTracks ?? []"
        :stage-rows="stageRows"
        :stage-total="stageTotal"
        :stage-leader-time="stageLeaderTime"
        :stage-loading="stageLoading"
        :stage-error="stageError"
        :bonus-rows="bonusRows"
        :bonus-total="bonusTotal"
        :bonus-leader-time="bonusLeaderTime"
        :bonus-loading="bonusLoading"
        :bonus-error="bonusError"
      />
    </template>
  </section>
</template>
