<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import type { RecentRecord } from '../../../api/client'
import MapPreviewImage from '../../../components/MapPreviewImage.vue'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'
import {
  formatRecordDate,
  formatTimeGap,
  isRecordWr,
  shouldShowGapFromWr,
} from '../../../utils/format'
import { tierChipColorClass, type MapImageConfig } from '../../../utils/display'

const props = defineProps<{
  loading: boolean
  rows: (RecentRecord | null)[]
  skeletonRows: number
  mapImageConfig?: MapImageConfig | null
}>()

const rowIndices = computed(() => {
  const count = props.loading ? props.skeletonRows : props.rows.length
  return Array.from({ length: count }, (_, i) => i)
})

function trackLabel(row: RecentRecord): string {
  if (row.stage != null) return `阶段${row.stage}`
  if (row.track > 0) return `B${row.track}`
  return '主线'
}
</script>

<template>
  <div class="px-home-list-table-wrap">
    <table :aria-busy="loading">
      <colgroup>
        <col class="w-[42%]" />
        <col />
        <col class="w-40" />
      </colgroup>
      <thead class="px-table-head">
        <tr>
          <th class="px-table-head-cell text-left">地图</th>
          <th class="px-table-head-cell text-left">玩家</th>
          <th class="px-table-head-cell text-right">时间</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="index in rowIndices"
          :key="loading ? `recent-sk-${index}` : rows[index]?.id ?? `recent-empty-${index}`"
          :class="loading || !rows[index] ? 'px-home-table-row-empty' : 'px-home-table-row'"
        >
          <template v-if="loading">
            <td class="px-home-recent-map-cell">
              <div class="flex min-h-14 w-full items-stretch gap-2 pr-3 pl-0">
                <span
                  class="px-home-recent-map-thumb-skeleton h-14 w-20 shrink-0 animate-pulse bg-neutral-200"
                  aria-hidden="true"
                />
                <div class="flex min-w-0 flex-1 flex-col justify-center gap-0.5 py-2 pr-3">
                  <SkeletonBar class="h-4 min-w-0 w-full max-w-full shrink" />
                  <div class="px-home-recent-map-track" aria-hidden="true">
                    <span
                      class="box-border inline-block h-[1.125rem] w-7 shrink-0 animate-pulse border-2 border-px-ink bg-neutral-200"
                    />
                    <SkeletonBar class="h-3 w-8 shrink-0" />
                  </div>
                </div>
              </div>
            </td>
            <td class="px-home-list-cell">
              <div class="px-table-cell-content">
                <SkeletonBar class="h-4 w-24 max-w-full shrink-0" />
              </div>
            </td>
            <td class="px-home-list-cell text-right">
              <div class="px-table-cell-content">
                <SkeletonBar class="ml-auto h-4 w-[5.5rem] shrink-0" />
                <SkeletonBar class="mt-0.5 ml-auto h-3 w-24 shrink-0" />
              </div>
            </td>
          </template>
          <template v-else-if="rows[index]">
            <td class="px-home-recent-map-cell">
              <RouterLink
                :to="`/maps/${encodeURIComponent(rows[index]!.map)}`"
                class="px-home-recent-map-link"
                :title="rows[index]!.map"
              >
                <MapPreviewImage
                  :map="rows[index]!.map"
                  :image-config="mapImageConfig"
                  variant="thumb"
                />
                <span class="px-home-recent-map-text">
                  <span class="px-home-recent-map-name">{{ rows[index]!.map }}</span>
                  <span class="px-home-recent-map-track">
                    <span class="px-home-recent-map-track-tier">
                      <span
                        v-if="rows[index]!.tier != null"
                        :class="[
                          'px-chip shrink-0 bg-px-surface text-[10px] leading-none',
                          tierChipColorClass(rows[index]!.tier!),
                        ]"
                      >
                        T{{ rows[index]!.tier }}
                      </span>
                    </span>
                    <span class="px-home-recent-map-track-kind">{{
                      trackLabel(rows[index]!)
                    }}</span>
                  </span>
                </span>
              </RouterLink>
            </td>
            <td class="px-home-list-cell">
              <RouterLink
                :to="`/players/${rows[index]!.auth}`"
                class="font-bold leading-tight hover:underline"
              >
                {{ rows[index]!.playerName ?? rows[index]!.auth }}
              </RouterLink>
            </td>
            <td class="px-home-list-cell text-right">
              <div class="font-mono font-bold leading-tight">
                {{ rows[index]!.timeFormatted }}
                <span
                  v-if="isRecordWr(rows[index]!) || shouldShowGapFromWr(rows[index]!)"
                  class="ml-1 text-xs font-normal opacity-70"
                >
                  {{
                    formatTimeGap(
                      isRecordWr(rows[index]!) ? 0 : rows[index]!.gapFromWr!,
                      3,
                    )
                  }}
                </span>
              </div>
              <div
                v-if="rows[index]!.date"
                class="font-mono text-xs leading-tight opacity-60"
              >
                {{ formatRecordDate(rows[index]!.date) }}
              </div>
            </td>
          </template>
          <td v-else colspan="3" class="px-home-table-row-empty" aria-hidden="true">&nbsp;</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
