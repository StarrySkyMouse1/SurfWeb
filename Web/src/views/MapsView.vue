<script setup lang="ts">
import { ref, watch, computed, onMounted } from 'vue'
import TierFilter from '../components/TierFilter.vue'
import MapCard from '../components/MapCard.vue'
import SkeletonMapGrid from '../components/skeleton/SkeletonMapGrid.vue'
import { useInfiniteScroll } from '../composables/useInfiniteScroll'
import { useMapImageConfig } from '../composables/useMapImageConfig'
import { apiGet, type MapListItem } from '../api/client'
import { skeletonGridCount } from '../utils/skeletonRowCount'

const { config: mapImageConfig } = useMapImageConfig()

const PAGE_SIZE = 24

const tier = ref('')
const search = ref('')
const page = ref(1)
const maps = ref<MapListItem[]>([])
const total = ref(0)
const loading = ref(false)
const loadingMore = ref(false)
const error = ref('')
const loadMoreRef = ref<HTMLElement | null>(null)

const hasMore = computed(() => maps.value.length < total.value)

const initialSkeletonCount = computed(() => {
  if (total.value > 0) return skeletonGridCount(0, total.value, PAGE_SIZE)
  return PAGE_SIZE
})

const loadMoreSkeletonCount = computed(() => skeletonGridCount(maps.value.length, total.value, 6))

let loadGeneration = 0

function loadMore() {
  if (!hasMore.value || loading.value || loadingMore.value) return
  void load(false)
}

const { checkAfterLoad } = useInfiniteScroll(loadMoreRef, loadMore)

async function load(reset = false) {
  if (!reset) {
    if (loading.value || loadingMore.value || !hasMore.value) return
  } else {
    loadGeneration += 1
    page.value = 1
    maps.value = []
    total.value = 0
    error.value = ''
  }

  const generation = loadGeneration
  const isInitial = maps.value.length === 0
  if (isInitial) loading.value = true
  else loadingMore.value = true

  try {
    const res = await apiGet<MapListItem[]>('/maps', {
      tier: tier.value || undefined,
      search: search.value || undefined,
      page: page.value,
      pageSize: PAGE_SIZE,
    })
    if (generation !== loadGeneration) return

    const batch = res.data ?? []
    total.value = res.meta?.total ?? 0
    maps.value = reset ? batch : [...maps.value, ...batch]
    if (batch.length > 0) page.value += 1

    if (hasMore.value) await checkAfterLoad()
  } catch (e) {
    if (generation !== loadGeneration) return
    error.value = e instanceof Error ? e.message : '加载失败'
  } finally {
    if (generation === loadGeneration) {
      loading.value = false
      loadingMore.value = false
    }
  }
}

watch([tier, search], () => load(true))

onMounted(() => load(true))
</script>

<template>
  <section class="space-y-6">
    <header class="flex items-end justify-between gap-2">
      <h1 class="px-page-heading text-2xl">地图</h1>
      <span class="font-pixel text-[9px] text-px-muted">MAPS</span>
    </header>
    <TierFilter v-model="tier" />
    <input
      v-model="search"
      type="search"
      placeholder="搜索地图名…"
      class="px-input w-full max-w-md px-4 py-2 font-medium"
    />
    <SkeletonMapGrid v-if="loading && maps.length === 0" :count="initialSkeletonCount" />
    <p v-else-if="error && maps.length === 0" class="px-panel-sm p-4 text-sm">{{ error }}</p>
    <template v-else>
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <MapCard v-for="m in maps" :key="m.map" :item="m" :image-config="mapImageConfig" />
      </div>
      <div v-if="loadingMore" class="mt-4">
        <SkeletonMapGrid v-if="loadMoreSkeletonCount > 0" :count="loadMoreSkeletonCount" />
      </div>
      <p v-if="error" class="px-panel-sm p-4 text-sm">{{ error }}</p>
      <p v-if="maps.length > 0" class="text-sm text-px-muted">
        共 {{ total }} 张地图
        <span v-if="hasMore"> · 已显示 {{ maps.length }}</span>
        <span v-else> · 已全部加载</span>
      </p>
      <p v-else-if="!loading" class="text-sm text-px-muted">暂无地图</p>
      <!-- 哨兵放在最底部，便于触底检测 -->
      <div ref="loadMoreRef" class="h-px w-full" aria-hidden="true" />
    </template>
  </section>
</template>
