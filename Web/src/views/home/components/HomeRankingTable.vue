<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import type { RankingEntry } from '../../../api/client'
import type { RankingFilterValue } from './RankingFilter.vue'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'
import { formatPlaytime } from '../../../utils/format'

const props = defineProps<{
  loading: boolean
  rows: (RankingEntry | null)[]
  rankingType: RankingFilterValue
  /** 加载态行数（与当前页实际条数一致，由父组件 skeletonRowsForPage 计算） */
  skeletonRows: number
  page: number
  pageSize: number
}>()

const valueColumnLabel = computed(() => {
  switch (props.rankingType) {
    case 'completions':
      return '完赛'
    case 'playtime':
      return '时长'
    case 'wr':
      return 'WR'
    default:
      return '积分'
  }
})

function formatValue(value: number): string {
  if (props.rankingType === 'points') return value.toFixed(1)
  if (props.rankingType === 'playtime') return formatPlaytime(value)
  return String(Math.round(value))
}

const rowIndices = computed(() => {
  const count = props.loading ? props.skeletonRows : props.rows.length
  return Array.from({ length: count }, (_, i) => i)
})

function displayRank(index: number): number {
  return (props.page - 1) * props.pageSize + index + 1
}
</script>

<template>
  <div class="px-home-list-table-wrap">
    <table :aria-busy="loading">
      <colgroup>
        <col class="w-12" />
        <col />
        <col class="w-28" />
      </colgroup>
      <thead class="px-table-head">
        <tr>
          <th class="px-table-head-pixel px-home-rank-cell">#</th>
          <th class="px-table-head-cell text-left">玩家</th>
          <th class="px-table-head-cell text-right">{{ valueColumnLabel }}</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="index in rowIndices"
          :key="loading ? `rank-sk-${index}` : rows[index]?.auth ?? `rank-empty-${index}`"
          :class="loading || !rows[index] ? 'px-home-table-row-empty' : 'px-home-table-row'"
        >
          <template v-if="loading">
            <td class="px-home-rank-cell px-home-list-cell">
              <div class="px-table-cell-content">
                <SkeletonBar class="mx-auto h-4 w-6 shrink-0" />
              </div>
            </td>
            <td class="px-home-list-cell">
              <div class="px-table-cell-content">
                <SkeletonBar class="h-4 w-28 max-w-full shrink-0" />
              </div>
            </td>
            <td class="px-home-list-cell text-right">
              <div class="px-table-cell-content">
                <SkeletonBar class="ml-auto h-4 w-14 shrink-0" />
              </div>
            </td>
          </template>
          <template v-else-if="rows[index]">
            <td
              :class="[
                'px-home-rank-cell px-home-list-cell',
                displayRank(index) === 1 && 'text-px-accent',
              ]"
            >
              {{ String(displayRank(index)).padStart(2, '0') }}
            </td>
            <td class="px-home-list-cell">
              <RouterLink
                :to="`/players/${rows[index]!.auth}`"
                class="font-bold hover:underline"
              >
                {{ rows[index]!.name ?? rows[index]!.auth }}
              </RouterLink>
            </td>
            <td
              class="px-home-list-cell text-right font-mono font-bold"
              :class="rankingType === 'playtime' && 'px-home-list-cell-truncate'"
              :title="rankingType === 'playtime' ? formatValue(rows[index]!.value) : undefined"
            >
              {{ formatValue(rows[index]!.value) }}
            </td>
          </template>
          <td v-else colspan="3" class="px-home-table-row-empty" aria-hidden="true">&nbsp;</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
