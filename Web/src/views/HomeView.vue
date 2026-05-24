<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import PaginationBar from '../components/PaginationBar.vue'
import { apiGet, type RankingEntry, type RecentRecord } from '../api/client'
import {
  HOME_LIST_CELL_CLASS,
  HOME_LIST_HEAD_CLASS,
  HOME_LIST_HEAD_PIXEL_CLASS,
  HOME_LIST_PAGE_SIZE,
  HOME_LIST_PANEL_CLASS,
  HOME_LIST_TABLE_WRAP_CLASS,
  HOME_LIST_ROW_CLASS,
  HOME_LIST_ROW_EMPTY_CLASS,
  HOME_RANK_CELL_CLASS,
  HOME_COLUMN_HEADER_CLASS,
  HOME_COLUMN_TITLE_CLASS,
  HOME_TABLES_GRID_CLASS,
} from '../constants/homeTable'
import { PX_EN_SUBTITLE_CLASS, PX_ERROR_BOX_CLASS, PX_TABLE_HEAD_CLASS } from '../constants/pixelTheme'
import { formatRecordDate } from '../utils/formatRecordDate'
import { formatTimeGap } from '../utils/formatTimeGap'
import { isRecordWr, shouldShowGapFromWr } from '../utils/isRecordWr'
import SkeletonTable from '../components/skeleton/SkeletonTable.vue'
import { skeletonRowsForPage } from '../utils/skeletonRowCount'

const pageSize = HOME_LIST_PAGE_SIZE
const maxListTotal = 100

const recent = ref<RecentRecord[]>([])
const recentTotal = ref(0)
const recentPage = ref(1)
const recentError = ref('')
const recentLoading = ref(true)
let recentLoadId = 0

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

function rankingDisplayRank(index: number): number {
  return (rankingsPage.value - 1) * pageSize + index + 1
}

async function loadRankings() {
  const id = ++rankingsLoadId
  rankingsLoading.value = true
  rankingsError.value = ''
  const page = rankingsPage.value
  try {
    const res = await apiGet<RankingEntry[]>('/rankings', {
      type: 'points',
      page,
      pageSize,
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
watch(recentPage, loadRecent)

loadRankings()
loadRecent()
</script>

<template>
  <section>
    <div :class="HOME_TABLES_GRID_CLASS">
    <div class="flex min-h-0 flex-col">
      <div :class="HOME_COLUMN_HEADER_CLASS">
        <h2 :class="HOME_COLUMN_TITLE_CLASS">排行</h2>
        <span :class="PX_EN_SUBTITLE_CLASS">RANKING</span>
      </div>
      <p v-if="rankingsError" :class="PX_ERROR_BOX_CLASS">{{ rankingsError }}</p>
      <div v-else :class="HOME_LIST_PANEL_CLASS" class="min-h-0 flex-1">
        <SkeletonTable
          v-if="rankingsLoading"
          variant="ranking"
          :rows="rankingsSkeletonRows"
          borderless
        />
        <div v-else :class="HOME_LIST_TABLE_WRAP_CLASS">
          <table class="w-full table-fixed text-sm">
            <colgroup>
              <col class="w-12" />
              <col />
              <col class="w-28" />
            </colgroup>
            <thead :class="PX_TABLE_HEAD_CLASS">
              <tr>
                <th :class="[HOME_LIST_HEAD_PIXEL_CLASS, HOME_RANK_CELL_CLASS]">#</th>
                <th :class="[HOME_LIST_HEAD_CLASS, 'text-left']">玩家</th>
                <th :class="[HOME_LIST_HEAD_CLASS, 'text-right']">积分</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="(row, index) in rankingSlots"
                :key="row ? row.auth : `rank-empty-${index}`"
                :class="row ? HOME_LIST_ROW_CLASS : HOME_LIST_ROW_EMPTY_CLASS"
              >
                <template v-if="row">
                  <td
                    :class="[
                      HOME_RANK_CELL_CLASS,
                      HOME_LIST_CELL_CLASS,
                      rankingDisplayRank(index) === 1 && 'text-px-accent',
                    ]"
                  >
                    {{ String(rankingDisplayRank(index)).padStart(2, '0') }}
                  </td>
                  <td :class="HOME_LIST_CELL_CLASS">
                    <RouterLink :to="`/players/${row.auth}`" class="font-bold hover:underline">
                      {{ row.name ?? row.auth }}
                    </RouterLink>
                  </td>
                  <td :class="[HOME_LIST_CELL_CLASS, 'text-right font-mono font-bold']">
                    {{ row.value }}
                  </td>
                </template>
                <template v-else>
                  <td colspan="3" :class="HOME_LIST_ROW_EMPTY_CLASS" aria-hidden="true">&nbsp;</td>
                </template>
              </tr>
            </tbody>
          </table>
        </div>
        <PaginationBar
          v-model:page="rankingsPage"
          attached
          :page-size="pageSize"
          :total="rankingsTotal"
          :loading="rankingsLoading"
        />
      </div>
    </div>

    <div class="flex min-h-0 flex-col">
      <div :class="HOME_COLUMN_HEADER_CLASS">
        <h2 :class="HOME_COLUMN_TITLE_CLASS">最新记录</h2>
        <span :class="PX_EN_SUBTITLE_CLASS">RECENT</span>
      </div>
      <p v-if="recentError" :class="PX_ERROR_BOX_CLASS">{{ recentError }}</p>
      <div v-else :class="HOME_LIST_PANEL_CLASS" class="min-h-0 flex-1">
        <SkeletonTable
          v-if="recentLoading"
          variant="recent"
          :rows="recentSkeletonRows"
          borderless
        />
        <div v-else :class="HOME_LIST_TABLE_WRAP_CLASS">
          <table class="w-full table-fixed text-sm">
            <colgroup>
              <col class="w-[32%]" />
              <col />
              <col class="w-40" />
            </colgroup>
            <thead :class="PX_TABLE_HEAD_CLASS">
              <tr>
                <th :class="[HOME_LIST_HEAD_CLASS, 'text-left']">玩家</th>
                <th :class="[HOME_LIST_HEAD_CLASS, 'text-left']">地图</th>
                <th :class="[HOME_LIST_HEAD_CLASS, 'text-right']">时间</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="(row, index) in recentSlots"
                :key="row ? row.id : `recent-empty-${index}`"
                :class="row ? HOME_LIST_ROW_CLASS : HOME_LIST_ROW_EMPTY_CLASS"
              >
                <template v-if="row">
                  <td :class="HOME_LIST_CELL_CLASS">
                    <RouterLink :to="`/players/${row.auth}`" class="font-bold leading-tight hover:underline">
                      {{ row.playerName ?? row.auth }}
                    </RouterLink>
                  </td>
                  <td :class="HOME_LIST_CELL_CLASS">
                    <RouterLink
                      :to="`/maps/${encodeURIComponent(row.map)}`"
                      class="block truncate font-bold leading-tight hover:underline"
                      :title="row.map"
                    >
                      {{ row.map }}
                    </RouterLink>
                    <span class="block text-xs leading-tight text-px-muted">
                      {{ row.track > 0 ? `B${row.track}` : '主线' }}
                    </span>
                  </td>
                  <td :class="[HOME_LIST_CELL_CLASS, 'text-right']">
                    <div class="font-mono font-bold leading-tight">
                      {{ row.timeFormatted }}
                      <span
                        v-if="isRecordWr(row) || shouldShowGapFromWr(row)"
                        class="ml-1 text-xs font-normal opacity-70"
                      >
                        {{ formatTimeGap(isRecordWr(row) ? 0 : row.gapFromWr!, 3) }}
                      </span>
                    </div>
                    <div v-if="row.date" class="font-mono text-xs leading-tight opacity-60">
                      {{ formatRecordDate(row.date) }}
                    </div>
                  </td>
                </template>
                <template v-else>
                  <td colspan="3" :class="HOME_LIST_ROW_EMPTY_CLASS" aria-hidden="true">&nbsp;</td>
                </template>
              </tr>
            </tbody>
          </table>
        </div>
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
