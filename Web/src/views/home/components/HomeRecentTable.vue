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
    <table class="px-home-recent-table" :aria-busy="loading">
      <colgroup>
        <col class="px-home-recent-col-map" />
        <col class="px-home-recent-col-player" />
        <col class="px-home-recent-col-time" />
      </colgroup>
      <thead class="px-table-head">
        <tr>
          <th class="px-table-head-cell text-left">地图</th>
          <th class="px-table-head-cell px-home-recent-player-col text-left">玩家</th>
          <th class="px-table-head-cell px-home-recent-time-col text-right">时间</th>
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
              <div class="px-home-recent-map-link">
                <span
                  class="px-home-recent-map-thumb-skeleton h-14 w-20 shrink-0 animate-pulse bg-neutral-200"
                  aria-hidden="true"
                />
                <div class="px-home-recent-map-text">
                  <SkeletonBar class="h-4 min-w-0 w-full max-w-full shrink" />
                  <div class="px-home-recent-map-sub" aria-hidden="true">
                    <div class="px-home-recent-map-track">
                      <span
                        class="box-border inline-block h-[1.125rem] w-7 shrink-0 animate-pulse border-2 border-px-ink bg-neutral-200"
                      />
                      <SkeletonBar class="h-3 w-8 shrink-0" />
                    </div>
                    <SkeletonBar class="px-home-recent-map-player-skeleton h-3 w-16 max-w-full shrink" />
                  </div>
                </div>
              </div>
            </td>
            <td class="px-home-list-cell px-home-recent-player-col">
              <SkeletonBar class="h-4 w-24 max-w-full shrink-0" />
            </td>
            <td class="px-home-list-cell px-home-recent-time-col text-right">
              <SkeletonBar class="ml-auto h-4 w-[5.5rem] shrink-0" />
              <SkeletonBar class="mt-0.5 ml-auto h-3 w-24 shrink-0" />
            </td>
          </template>
          <template v-else-if="rows[index]">
            <td class="px-home-recent-map-cell">
              <div class="px-home-recent-map-link">
                <RouterLink
                  :to="`/maps/${encodeURIComponent(rows[index]!.map)}`"
                  class="px-home-recent-map-main"
                  :title="rows[index]!.map"
                >
                  <MapPreviewImage
                    :map="rows[index]!.map"
                    :image-config="mapImageConfig"
                    variant="thumb"
                  />
                  <span class="px-home-recent-map-name">{{ rows[index]!.map }}</span>
                </RouterLink>
                <div class="px-home-recent-map-sub">
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
                  <RouterLink
                    :to="`/players/${rows[index]!.auth}`"
                    class="px-home-recent-map-player"
                  >
                    {{ rows[index]!.playerName ?? rows[index]!.auth }}
                  </RouterLink>
                </div>
              </div>
            </td>
            <td class="px-home-list-cell px-home-recent-player-col">
              <RouterLink
                :to="`/players/${rows[index]!.auth}`"
                class="block min-w-0 truncate font-bold leading-tight hover:underline"
              >
                {{ rows[index]!.playerName ?? rows[index]!.auth }}
              </RouterLink>
            </td>
            <td class="px-home-list-cell px-home-recent-time-col text-right">
              <div class="px-home-recent-time-main font-mono font-bold leading-tight whitespace-nowrap">
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
                class="px-home-recent-time-date font-mono text-xs leading-tight whitespace-nowrap opacity-60"
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
