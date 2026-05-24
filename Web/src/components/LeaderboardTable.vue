<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import type { LeaderboardEntry } from '../api/client'
import {
  LEADERBOARD_PAGE_SIZE,
  LEADERBOARD_ROW_CLASS,
  LEADERBOARD_ROW_EMPTY_CLASS,
  TABLE_CELL_CONTENT_CLASS,
  TABLE_DATA_CELL_CLASS,
  TABLE_HEAD_CELL_CLASS,
} from '../constants/tableCell'
import { PX_PANEL_CLASS, PX_RANK_CELL_CLASS, PX_TABLE_HEAD_CLASS } from '../constants/pixelTheme'
import { formatRecordDate } from '../utils/formatRecordDate'
import { formatTimeGap } from '../utils/formatTimeGap'
import SkeletonBar from './skeleton/SkeletonBar.vue'

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
  { bare: false, pageSize: LEADERBOARD_PAGE_SIZE, loading: false },
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
  <div :class="[props.bare ? '' : PX_PANEL_CLASS, 'overflow-x-auto']">
    <table class="w-full min-w-[560px] table-fixed text-left text-sm" :aria-busy="loading">
      <colgroup>
        <col class="w-12" />
        <col />
        <col class="w-36" />
        <col class="w-24" />
        <col class="w-40" />
      </colgroup>
      <thead :class="PX_TABLE_HEAD_CLASS">
        <tr>
          <th :class="[PX_RANK_CELL_CLASS, 'py-2']">#</th>
          <th :class="[TABLE_HEAD_CELL_CLASS, 'text-left']">玩家</th>
          <th :class="[TABLE_HEAD_CELL_CLASS, 'text-right']">时间</th>
          <th :class="[TABLE_HEAD_CELL_CLASS, 'text-right']">同步</th>
          <th :class="[TABLE_HEAD_CELL_CLASS, 'text-right']">日期</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="(row, index) in rowSlots"
          :key="`lb-row-${index}`"
          :class="
            loading || !row
              ? LEADERBOARD_ROW_EMPTY_CLASS
              : [LEADERBOARD_ROW_CLASS, row.rank === 1 && 'font-semibold']
          "
        >
          <template v-if="loading">
            <td :class="[PX_RANK_CELL_CLASS, TABLE_DATA_CELL_CLASS]">
              <div :class="TABLE_CELL_CONTENT_CLASS">
                <SkeletonBar class="mx-auto h-4 w-6 shrink-0" />
              </div>
            </td>
            <td :class="TABLE_DATA_CELL_CLASS">
              <div :class="TABLE_CELL_CONTENT_CLASS">
                <SkeletonBar class="h-4 w-28 max-w-full shrink-0" />
              </div>
            </td>
            <td :class="[TABLE_DATA_CELL_CLASS, 'text-right']">
              <div :class="TABLE_CELL_CONTENT_CLASS">
                <SkeletonBar class="ml-auto h-4 w-[5.5rem] shrink-0" />
              </div>
            </td>
            <td :class="[TABLE_DATA_CELL_CLASS, 'text-right']">
              <div :class="TABLE_CELL_CONTENT_CLASS">
                <SkeletonBar class="ml-auto h-4 w-10 shrink-0" />
              </div>
            </td>
            <td :class="[TABLE_DATA_CELL_CLASS, 'text-right']">
              <div :class="TABLE_CELL_CONTENT_CLASS">
                <SkeletonBar class="ml-auto h-4 w-24 shrink-0" />
              </div>
            </td>
          </template>
          <template v-else-if="row">
            <td :class="[PX_RANK_CELL_CLASS, TABLE_DATA_CELL_CLASS, row.rank === 1 && 'text-px-accent']">
              {{ String(row.rank).padStart(2, '0') }}
            </td>
            <td :class="TABLE_DATA_CELL_CLASS">
              <RouterLink
                :to="`/players/${row.auth}`"
                class="font-semibold underline-offset-2 hover:underline"
                @click.stop
              >
                {{ row.playerName ?? row.auth }}
              </RouterLink>
            </td>
            <td :class="[TABLE_DATA_CELL_CLASS, 'text-right font-mono']">
              <span>{{ row.timeFormatted }}</span>
              <span v-if="gapSeconds(row) != null" class="ml-2 text-xs opacity-70">
                {{ formatTimeGap(gapSeconds(row)!) }}
              </span>
            </td>
            <td :class="[TABLE_DATA_CELL_CLASS, 'text-right font-mono']">
              {{ row.sync != null ? `${Math.round(row.sync)}%` : '—' }}
            </td>
            <td :class="[TABLE_DATA_CELL_CLASS, 'text-right font-mono text-xs']">
              {{ row.date ? formatRecordDate(row.date) : '—' }}
            </td>
          </template>
          <td v-else colspan="5" :class="LEADERBOARD_ROW_EMPTY_CLASS" aria-hidden="true">&nbsp;</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
