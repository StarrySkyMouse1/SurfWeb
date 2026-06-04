<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import PaginationBar from '../../components/PaginationBar.vue'
import {
  apiGet,
  type PlayerRecordCategory,
  type PlayerRecordScope,
  type PlayerRecordsPage,
  type PlayerSummary,
} from '../../api/client'
import PlayerPassportCard from './components/PlayerPassportCard.vue'
import PlayerRecordFilters from './components/PlayerRecordFilters.vue'
import PlayerRecordsTable from './components/PlayerRecordsTable.vue'
import PlayerChartsPanel from './components/PlayerChartsPanel.vue'
import { skeletonRowsForPage } from '../../utils/display'
import { useMapImageConfig } from '../../composables/useMapImageConfig'

const { config: mapImageConfig } = useMapImageConfig()

const props = defineProps<{ auth: string }>()

const pageSize = 10

const player = ref<PlayerSummary | null>(null)
const recordsData = ref<PlayerRecordsPage | null>(null)
const total = ref(0)
const page = ref(1)
const category = ref<PlayerRecordCategory>('recent')
const scope = ref<PlayerRecordScope>('main')
const incompleteTier = ref('')

const error = ref('')
const recordsError = ref('')
const profileLoading = ref(true)
const recordsLoading = ref(false)
const totalKnown = ref(false)

const categoryLabels: Record<PlayerRecordCategory, string> = {
  recent: '近期记录',
  wr: 'WR',
  incomplete: '未完成',
}

const scopeLabels: Record<PlayerRecordScope, string> = {
  main: '主线',
  stage: '阶段',
  bonus: '奖励',
}

const chartPanelTitle = computed(() => {
  const base = `${categoryLabels[category.value]} · ${scopeLabels[scope.value]}`
  if (category.value === 'incomplete') {
    const tierLabel =
      incompleteTier.value === '' ? '全部' : `T${incompleteTier.value}`
    return `${base} · ${tierLabel}`
  }
  return base
})

const recordSlots = computed(() =>
  Array.from({ length: pageSize }, (_, i) => recordsData.value?.items[i] ?? null),
)

const recordSkeletonRows = computed(() =>
  skeletonRowsForPage(page.value, pageSize, {
    total: totalKnown.value ? total.value : null,
    fallback: pageSize,
  }),
)

const recordsTableLoading = computed(() => profileLoading.value || recordsLoading.value)

async function loadPlayer() {
  const res = await apiGet<PlayerSummary>(`/players/${props.auth}`)
  player.value = res.data ?? null
  if (!player.value) {
    error.value = '玩家不存在'
  }
}

async function loadRecords() {
  recordsLoading.value = true
  recordsError.value = ''
  try {
    const params: Record<string, string | number | undefined> = {
      category: category.value,
      scope: scope.value,
      page: page.value,
      pageSize,
    }
    if (category.value === 'incomplete' && incompleteTier.value !== '') {
      params.tier = Number(incompleteTier.value)
    }
    const res = await apiGet<PlayerRecordsPage>(`/players/${props.auth}/records`, params)
    recordsData.value = res.data ?? null
    total.value = res.meta?.total ?? 0
    totalKnown.value = true
  } catch (e) {
    recordsData.value = null
    total.value = 0
    recordsError.value = e instanceof Error ? e.message : '加载失败'
  } finally {
    recordsLoading.value = false
  }
}

async function load() {
  profileLoading.value = true
  error.value = ''
  recordsError.value = ''
  totalKnown.value = false
  try {
    await loadPlayer()
    profileLoading.value = false
    if (player.value) {
      await loadRecords()
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : '加载失败'
    profileLoading.value = false
  }
}

watch(() => props.auth, () => {
  page.value = 1
  load()
})

watch([category, scope, incompleteTier], () => {
  page.value = 1
  if (player.value && !profileLoading.value) {
    loadRecords()
  }
})

watch(page, () => {
  if (player.value && !profileLoading.value) {
    loadRecords()
  }
})

onMounted(load)
</script>

<template>
  <section class="space-y-6">
    <p v-if="error && !profileLoading" class="px-panel-sm p-4">{{ error }}</p>
    <template v-else>
      <PlayerPassportCard :loading="profileLoading" :player="player" />

      <div v-if="profileLoading || player">
        <h2 class="mb-4 text-lg font-bold">记录与进度</h2>

        <PlayerRecordFilters
          v-model:category="category"
          v-model:scope="scope"
          v-model:incomplete-tier="incompleteTier"
        />

        <p v-if="recordsError && !profileLoading" class="px-panel-sm mb-4 p-4 text-sm">
          {{ recordsError }}
        </p>

        <div v-else class="px-player-records-split">
          <div class="min-w-0">
            <div class="px-panel overflow-hidden">
              <PlayerRecordsTable
                :loading="recordsTableLoading"
                :rows="recordSlots"
                :skeleton-rows="recordSkeletonRows"
                :category="category"
                :scope="scope"
                :map-image-config="mapImageConfig"
              />
              <PaginationBar
                v-model:page="page"
                attached
                :page-size="pageSize"
                :total="total"
                :loading="recordsTableLoading"
              />
            </div>
          </div>

          <div class="min-w-0">
            <PlayerChartsPanel
              :loading="recordsTableLoading"
              :charts="recordsData?.charts"
              :panel-title="chartPanelTitle"
            />
          </div>
        </div>
      </div>
    </template>
  </section>
</template>
