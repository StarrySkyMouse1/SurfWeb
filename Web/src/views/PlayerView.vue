<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import PaginationBar from '../components/PaginationBar.vue'
import { apiGet, type PlayerCompletion, type PlayerSummary } from '../api/client'
import { formatRecordDate } from '../utils/formatRecordDate'
import { formatTimeGap } from '../utils/formatTimeGap'
import { shouldShowGapFromWr } from '../utils/isRecordWr'
import { formatPlaytime } from '../utils/formatPlaytime'
import SkeletonStatCards from '../components/skeleton/SkeletonStatCards.vue'
import SkeletonTable from '../components/skeleton/SkeletonTable.vue'
import {
  COMPLETIONS_PAGE_SIZE,
  COMPLETIONS_ROW_CLASS,
  COMPLETIONS_ROW_EMPTY_CLASS,
  TABLE_DATA_CELL_CLASS,
  TABLE_HEAD_CELL_CLASS,
} from '../constants/tableCell'
import { PX_TABLE_HEAD_CLASS } from '../constants/pixelTheme'

const props = defineProps<{ auth: string }>()

const pageSize = COMPLETIONS_PAGE_SIZE

const player = ref<PlayerSummary | null>(null)
const completions = ref<PlayerCompletion[]>([])
const total = ref(0)
const page = ref(1)
const error = ref('')
const completionsError = ref('')
const profileLoading = ref(true)
const completionsLoading = ref(false)
const completionSlots = computed(() =>
  Array.from({ length: pageSize }, (_, i) => completions.value[i] ?? null),
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
    <template v-if="profileLoading">
      <SkeletonStatCards />
      <div>
        <h2 class="mb-4 text-lg font-bold">完成地图</h2>
        <div class="px-panel overflow-hidden">
          <SkeletonTable variant="completions" :rows="pageSize" borderless />
          <PaginationBar
            v-model:page="page"
            attached
            :page-size="pageSize"
            :total="total"
            loading
          />
        </div>
      </div>
    </template>
    <p v-else-if="error" class="px-panel-sm p-4">{{ error }}</p>
    <template v-else-if="player">
      <div class="px-panel p-8">
        <div class="flex items-end justify-between gap-2">
          <div>
            <h1 class="text-2xl font-bold">{{ player.name ?? player.auth }}</h1>
            <p class="mt-1 text-xs text-px-muted">玩家资料</p>
          </div>
          <span class="font-pixel text-[9px] text-px-muted">PLAYER</span>
        </div>
        <dl class="mt-6 grid gap-4 sm:grid-cols-3">
          <div class="px-panel-sm p-4">
            <dt class="text-xs font-bold text-px-muted">积分</dt>
            <dd class="mt-1 font-mono text-xl font-bold">#{{ player.pointsRank }}</dd>
            <dd class="mt-0.5 text-sm text-px-muted">{{ player.points.toFixed(1) }}</dd>
          </div>
          <div class="px-panel-sm p-4">
            <dt class="text-xs font-bold text-px-muted">游玩时长</dt>
            <dd class="mt-1 font-mono text-xl font-bold">#{{ player.playtimeRank }}</dd>
            <dd class="mt-0.5 text-sm text-px-muted">{{ formatPlaytime(player.playtime) }}</dd>
          </div>
          <div class="px-panel-sm p-4">
            <dt class="text-xs font-bold text-px-muted">完成地图</dt>
            <dd class="mt-1 font-mono text-xl font-bold">#{{ player.completionRank }}</dd>
            <dd class="mt-0.5 text-sm text-px-muted">{{ player.completionCount }} 张</dd>
          </div>
        </dl>
      </div>

      <div>
        <h2 class="mb-4 text-lg font-bold">完成地图</h2>
        <p v-if="completionsError" class="px-panel-sm p-4 text-sm">{{ completionsError }}</p>
        <div v-else class="px-panel overflow-hidden">
          <SkeletonTable
            v-if="completionsLoading"
            variant="completions"
            :rows="pageSize"
            borderless
          />
          <div v-else class="overflow-x-auto">
            <table class="w-full min-w-[520px] table-fixed text-left text-sm">
              <colgroup>
                <col />
                <col class="w-16" />
                <col class="w-28" />
                <col class="w-24" />
                <col class="w-36" />
              </colgroup>
              <thead :class="PX_TABLE_HEAD_CLASS">
                <tr>
                  <th :class="[TABLE_HEAD_CELL_CLASS, 'text-left']">地图</th>
                  <th :class="TABLE_HEAD_CELL_CLASS">Tier</th>
                  <th :class="[TABLE_HEAD_CELL_CLASS, 'text-right']">时间</th>
                  <th :class="[TABLE_HEAD_CELL_CLASS, 'text-right']">同步</th>
                  <th :class="[TABLE_HEAD_CELL_CLASS, 'text-right']">日期</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="(c, index) in completionSlots"
                  :key="c ? c.map : `completion-empty-${index}`"
                  :class="c ? COMPLETIONS_ROW_CLASS : COMPLETIONS_ROW_EMPTY_CLASS"
                >
                  <template v-if="c">
                    <td :class="TABLE_DATA_CELL_CLASS">
                      <RouterLink
                        :to="`/maps/${encodeURIComponent(c.map)}`"
                        class="block truncate font-bold underline-offset-2 hover:underline"
                        :title="c.map"
                      >
                        {{ c.map }}
                      </RouterLink>
                    </td>
                    <td :class="TABLE_DATA_CELL_CLASS">
                      <span
                        v-if="c.tier != null"
                        class="border border-current px-1.5 py-0.5 text-xs font-bold"
                      >
                        T{{ c.tier }}
                      </span>
                      <span v-else class="text-neutral-400">—</span>
                    </td>
                    <td :class="[TABLE_DATA_CELL_CLASS, 'text-right font-mono font-bold']">
                      {{ c.timeFormatted }}
                      <span
                        v-if="shouldShowGapFromWr(c)"
                        class="ml-2 text-xs font-normal opacity-70"
                      >
                        {{ formatTimeGap(c.gapFromWr!, 3) }}
                      </span>
                    </td>
                    <td :class="[TABLE_DATA_CELL_CLASS, 'text-right font-mono']">
                      {{ c.sync != null ? `${Math.round(c.sync)}%` : '—' }}
                    </td>
                    <td :class="[TABLE_DATA_CELL_CLASS, 'text-right font-mono text-xs']">
                      {{ c.date ? formatRecordDate(c.date) : '—' }}
                    </td>
                  </template>
                  <td v-else colspan="5" :class="COMPLETIONS_ROW_EMPTY_CLASS" aria-hidden="true">
                    &nbsp;
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <PaginationBar
            v-model:page="page"
            attached
            :page-size="pageSize"
            :total="total"
            :loading="completionsLoading"
          />
        </div>
      </div>
    </template>
  </section>
</template>
