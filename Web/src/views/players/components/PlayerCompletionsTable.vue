<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import type { PlayerCompletion } from '../../../api/client'
import MapPreviewImage from '../../../components/MapPreviewImage.vue'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'
import {
  formatRecordDate,
  formatTimeGap,
  shouldShowGapFromWr,
} from '../../../utils/format'
import { tierChipColorClass, type MapImageConfig } from '../../../utils/display'

const props = defineProps<{
  loading: boolean
  rows: (PlayerCompletion | null)[]
  skeletonRows: number
  mapImageConfig?: MapImageConfig | null
}>()

const rowIndices = computed(() => {
  const count = props.loading ? props.skeletonRows : props.rows.length
  return Array.from({ length: count }, (_, i) => i)
})
</script>

<template>
  <div class="overflow-x-auto bg-px-surface">
    <table
      class="w-full min-w-[560px] table-fixed text-left text-sm"
      :aria-busy="loading"
    >
      <colgroup>
        <col class="w-[36%]" />
        <col class="w-20" />
        <col class="w-28" />
        <col class="w-24" />
        <col class="w-36" />
      </colgroup>
      <thead class="px-table-head">
        <tr>
          <th class="px-table-head-cell text-left">地图</th>
          <th class="px-table-head-cell">Tier</th>
          <th class="px-table-head-cell text-right">时间</th>
          <th class="px-table-head-cell text-right">同步</th>
          <th class="px-table-head-cell text-right">日期</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="index in rowIndices"
          :key="loading ? `comp-sk-${index}` : rows[index]?.map ?? `comp-empty-${index}`"
          :class="loading || !rows[index] ? 'px-table-row-empty h-14' : 'px-table-row h-14'"
        >
          <template v-if="loading">
            <td class="px-player-completion-map-cell">
              <div class="flex h-14 w-full items-stretch gap-2 pr-3 pl-0">
                <span
                  class="h-14 w-20 shrink-0 animate-pulse bg-neutral-200"
                  aria-hidden="true"
                />
                <div class="flex min-w-0 flex-1 items-center py-2">
                  <SkeletonBar class="h-4 max-w-[12rem] flex-1 shrink-0" />
                </div>
              </div>
            </td>
            <td class="px-player-completion-tier-cell">
              <span
                class="px-chip inline-block animate-pulse border-px-ink bg-neutral-200 font-pixel text-[10px] leading-none text-transparent shadow-none"
                aria-hidden="true"
              >T0</span>
            </td>
            <td class="px-table-data-cell text-right">
              <div class="px-table-cell-content">
                <SkeletonBar class="ml-auto h-4 w-16 shrink-0" />
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
          <template v-else-if="rows[index]">
            <td class="px-player-completion-map-cell">
              <RouterLink
                :to="`/maps/${encodeURIComponent(rows[index]!.map)}`"
                class="px-player-completion-map-link"
                :title="rows[index]!.map"
              >
                <MapPreviewImage
                  :map="rows[index]!.map"
                  :image-config="mapImageConfig"
                  variant="thumb"
                />
                <span class="px-player-completion-map-name">
                  <span class="truncate font-bold underline-offset-2 hover:underline">
                    {{ rows[index]!.map }}
                  </span>
                </span>
              </RouterLink>
            </td>
            <td class="px-player-completion-tier-cell">
              <span
                v-if="rows[index]!.tier != null"
                :class="[
                  'px-chip bg-px-surface',
                  tierChipColorClass(rows[index]!.tier!),
                ]"
              >
                T{{ rows[index]!.tier }}
              </span>
              <span v-else class="text-neutral-400">—</span>
            </td>
            <td class="px-table-data-cell text-right font-mono font-bold">
              {{ rows[index]!.timeFormatted }}
              <span
                v-if="shouldShowGapFromWr(rows[index]!)"
                class="ml-2 text-xs font-normal opacity-70"
              >
                {{ formatTimeGap(rows[index]!.gapFromWr!, 3) }}
              </span>
            </td>
            <td class="px-table-data-cell text-right font-mono">
              {{ rows[index]!.sync != null ? `${Math.round(rows[index]!.sync!)}%` : '—' }}
            </td>
            <td class="px-table-data-cell text-right font-mono text-xs">
              {{ rows[index]!.date ? formatRecordDate(rows[index]!.date) : '—' }}
            </td>
          </template>
          <td v-else colspan="5" class="px-table-row-empty h-14" aria-hidden="true">&nbsp;</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
