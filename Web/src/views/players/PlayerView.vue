<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import PaginationBar from '../../components/PaginationBar.vue'
import { apiGet, type PlayerCompletion, type PlayerSummary } from '../../api/client'
import PlayerProfileCard from './components/PlayerProfileCard.vue'
import PlayerCompletionsTable from './components/PlayerCompletionsTable.vue'
import { skeletonRowsForPage } from '../../utils/display'
import { useMapImageConfig } from '../../composables/useMapImageConfig'

const { config: mapImageConfig } = useMapImageConfig()

const props = defineProps<{ auth: string }>()

const pageSize = 10

const player = ref<PlayerSummary | null>(null)
const completions = ref<PlayerCompletion[]>([])
const total = ref(0)
const page = ref(1)
const error = ref('')
const completionsError = ref('')
const profileLoading = ref(true)
const completionsLoading = ref(false)
const totalKnown = ref(false)

const completionSlots = computed(() =>
  Array.from({ length: pageSize }, (_, i) => completions.value[i] ?? null),
)

const completionSkeletonRows = computed(() =>
  skeletonRowsForPage(page.value, pageSize, {
    total: totalKnown.value ? total.value : null,
    fallback: pageSize,
  }),
)

const completionsTableLoading = computed(
  () => profileLoading.value || completionsLoading.value,
)

async function loadPlayer() {
  const res = await apiGet<PlayerSummary>(`/players/${props.auth}`)
  player.value = res.data ?? null
  if (!player.value) {
    error.value = '玩家不存在'
  }
}

async function loadCompletions() {
  completionsLoading.value = true
  completionsError.value = ''
  try {
    const res = await apiGet<PlayerCompletion[]>(`/players/${props.auth}/completions`, {
      page: page.value,
      pageSize,
    })
    completions.value = res.data ?? []
    total.value = res.meta?.total ?? 0
    totalKnown.value = true
  } catch (e) {
    completions.value = []
    total.value = 0
    completionsError.value = e instanceof Error ? e.message : '加载失败'
  } finally {
    completionsLoading.value = false
  }
}

async function load() {
  profileLoading.value = true
  error.value = ''
  completionsError.value = ''
  totalKnown.value = false
  try {
    await loadPlayer()
    profileLoading.value = false
    if (player.value) {
      await loadCompletions()
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

watch(page, () => {
  if (player.value && !profileLoading.value) {
    loadCompletions()
  }
})

onMounted(load)
</script>

<template>
  <section class="space-y-6">
    <p v-if="error && !profileLoading" class="px-panel-sm p-4">{{ error }}</p>
    <template v-else>
      <PlayerProfileCard :loading="profileLoading" :player="player" />

      <div v-if="profileLoading || player">
        <h2 class="mb-4 text-lg font-bold">完成地图</h2>
        <p v-if="completionsError && !profileLoading" class="px-panel-sm p-4 text-sm">
          {{ completionsError }}
        </p>
        <div v-else class="px-panel overflow-hidden">
          <PlayerCompletionsTable
            :loading="completionsTableLoading"
            :rows="completionSlots"
            :skeleton-rows="completionSkeletonRows"
            :map-image-config="mapImageConfig"
          />
          <PaginationBar
            v-model:page="page"
            attached
            :page-size="pageSize"
            :total="total"
            :loading="completionsTableLoading"
          />
        </div>
      </div>
    </template>
  </section>
</template>
