<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import type { LeaderboardEntry } from '../../../api/client'
import { formatRecordDate, formatTimeGap } from '../../../utils/format'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'

const props = withDefaults(
  defineProps<{
    rows: LeaderboardEntry[]
    leaderTime?: number | null
    pageSize?: number
    /** 外层已有 px-panel 时去掉重复阴影框 */
    bare?: boolean
    /** 与真实表同 DOM，仅单元格内容为骨架条 */
    loading?: boolean
  }>(),
  { bare: false, pageSize: 10, loading: false },
)

const rowSlots = computed(() =>
  Array.from({ length: props.pageSize }, (_, i) => props.rows[i] ?? null),
)

const baselineTime = computed(() => {
  if (props.leaderTime != null) return props.leaderTime
  return props.rows.find((r) => r.rank === 1)?.time ?? null
})

function gapSeconds(row: LeaderboardEntry): number | null {
  if (row.rank === 1 || baselineTime.value == null) return null
  return row.time - baselineTime.value
}
</script>

<template>
  <div :class="[props.bare ? '' : 'px-panel', 'overflow-x-auto']">
    <table class="w-full min-w-[560px] table-fixed text-left text-sm" :aria-busy="loading">
      <colgroup>
        <col class="w-12" />
        <col />
        <col class="w-36" />
        <col class="w-24" />
        <col class="w-40" />
      </colgroup>
      <thead class="px-table-head">
        <tr>
          <th class="px-rank-cell py-2">#</th>
          <th class="px-table-head-cell text-left">玩家</th>
          <th class="px-table-head-cell text-right">时间</th>
          <th class="px-table-head-cell text-right">同步</th>
          <th class="px-table-head-cell text-right">日期</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="(row, index) in rowSlots"
          :key="`lb-row-${index}`"
          :class="
            loading || !row
              ? 'px-table-row-empty'
              : ['px-table-row', row.rank === 1 && 'font-semibold']
          "
        >
          <template v-if="loading">
            <td class="px-rank-cell px-table-data-cell">
              <div class="px-table-cell-content">
                <SkeletonBar class="mx-auto h-4 w-6 shrink-0" />
              </div>
            </td>
            <td class="px-table-data-cell">
              <div class="px-table-cell-content">
                <SkeletonBar class="h-4 w-28 max-w-full shrink-0" />
              </div>
            </td>
            <td class="px-table-data-cell text-right">
              <div class="px-table-cell-content">
                <SkeletonBar class="ml-auto h-4 w-[5.5rem] shrink-0" />
              </div>
            </td>
            <td class="px-table-data-cell text-right">
              <div class="px-table-cell-content">
                <SkeletonBar class="ml-auto h-4 w-10 shrink-0" />
              </div>
            </td>
            <td class="px-table-data-cell text-right">
              <div class="px-table-cell-content">
                <SkeletonBar class="ml-auto h-4 w-24 shrink-0" />
              </div>
            </td>
          </template>
          <template v-else-if="row">
            <td
              :class="[
                'px-rank-cell px-table-data-cell',
                row.rank === 1 && 'text-px-accent',
              ]"
            >
              {{ String(row.rank).padStart(2, '0') }}
            </td>
            <td class="px-table-data-cell">
              <RouterLink
                :to="`/players/${row.auth}`"
                class="font-semibold underline-offset-2 hover:underline"
                @click.stop
              >
                {{ row.playerName ?? row.auth }}
              </RouterLink>
            </td>
            <td class="px-table-data-cell text-right font-mono">
              <span>{{ row.timeFormatted }}</span>
              <span v-if="gapSeconds(row) != null" class="ml-2 text-xs opacity-70">
                {{ formatTimeGap(gapSeconds(row)!) }}
              </span>
            </td>
            <td class="px-table-data-cell text-right font-mono">
              {{ row.sync != null ? `${Math.round(row.sync)}%` : '—' }}
            </td>
            <td class="px-table-data-cell text-right font-mono text-xs">
              {{ row.date ? formatRecordDate(row.date) : '—' }}
            </td>
          </template>
          <td v-else colspan="5" class="px-table-row-empty" aria-hidden="true">&nbsp;</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
