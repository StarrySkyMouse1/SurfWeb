<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import type {
  PlayerRecord,
  PlayerRecordCategory,
  PlayerRecordScope,
} from '../../../api/client'
import MapPreviewImage from '../../../components/MapPreviewImage.vue'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'
import {
  formatRecordDate,
  formatTimeGap,
  isRecordWr,
  shouldShowGapFromWr,
} from '../../../utils/format'
import { tierChipColorClass, type MapImageConfig } from '../../../utils/display'

const props = withDefaults(
  defineProps<{
    loading: boolean
    rows: (PlayerRecord | null)[]
    skeletonRows: number
    category: PlayerRecordCategory
    scope: PlayerRecordScope
    mapImageConfig?: MapImageConfig | null
    /** 外层已有 px-panel 时不再套一层面板 */
    bare?: boolean
  }>(),
  { bare: true },
)

type ColKey = 'map' | 'track' | 'stage' | 'tier' | 'time' | 'sync' | 'date'

const COL_WIDTH_CLASS: Record<ColKey, string> = {
  map: 'w-[40%] min-w-[11rem]',
  tier: 'w-[5rem]',
  track: 'w-20',
  stage: 'w-24',
  time: 'w-28',
  sync: 'w-24',
  date: 'w-36',
}

const columns = computed((): { key: ColKey; label: string; align?: 'right' }[] => {
  const { category, scope } = props
  if (category === 'incomplete') {
    if (scope === 'main') {
      return [
        { key: 'map', label: '地图' },
        { key: 'tier', label: 'Tier' },
      ]
    }
    if (scope === 'stage') {
      return [
        { key: 'map', label: '地图' },
        { key: 'tier', label: 'Tier' },
        { key: 'stage', label: '阶段' },
      ]
    }
    return [
      { key: 'map', label: '地图' },
      { key: 'tier', label: 'Tier' },
      { key: 'track', label: '赛道' },
    ]
  }

  if (category === 'wr') {
    if (scope === 'main') {
      return [
        { key: 'map', label: '地图' },
        { key: 'tier', label: 'Tier' },
        { key: 'time', label: 'WR 时间', align: 'right' },
        { key: 'date', label: '达成日期', align: 'right' },
      ]
    }
    if (scope === 'stage') {
      return [
        { key: 'map', label: '地图' },
        { key: 'tier', label: 'Tier' },
        { key: 'stage', label: '阶段' },
        { key: 'time', label: 'WR 时间', align: 'right' },
        { key: 'date', label: '达成日期', align: 'right' },
      ]
    }
    return [
      { key: 'map', label: '地图' },
      { key: 'tier', label: 'Tier' },
      { key: 'track', label: '赛道' },
      { key: 'time', label: 'WR 时间', align: 'right' },
      { key: 'date', label: '达成日期', align: 'right' },
    ]
  }

  if (scope === 'main') {
    return [
      { key: 'map', label: '地图' },
      { key: 'tier', label: 'Tier' },
      { key: 'time', label: '时间', align: 'right' },
      { key: 'sync', label: '同步', align: 'right' },
      { key: 'date', label: '日期', align: 'right' },
    ]
  }
  if (scope === 'stage') {
    return [
      { key: 'map', label: '地图' },
      { key: 'tier', label: 'Tier' },
      { key: 'stage', label: '阶段' },
      { key: 'time', label: '时间', align: 'right' },
      { key: 'date', label: '日期', align: 'right' },
    ]
  }
  return [
    { key: 'map', label: '地图' },
    { key: 'tier', label: 'Tier' },
    { key: 'track', label: '赛道' },
    { key: 'time', label: '时间', align: 'right' },
    { key: 'sync', label: '同步', align: 'right' },
    { key: 'date', label: '日期', align: 'right' },
  ]
})

const colCount = computed(() => columns.value.length)

const rowIndices = computed(() => {
  const count = props.loading ? props.skeletonRows : props.rows.length
  return Array.from({ length: count }, (_, i) => i)
})

function trackLabel(track?: number): string {
  if (track == null || track === 0) return '主线'
  return `B${track}`
}

function stageLabel(stage?: number): string {
  return stage != null ? `阶段 ${stage}` : '—'
}

const isRecent = computed(() => props.category === 'recent')

const showTime = computed(() => columns.value.some((c) => c.key === 'time'))
const showSync = computed(() => columns.value.some((c) => c.key === 'sync'))
const showDate = computed(() => columns.value.some((c) => c.key === 'date'))
const showTrack = computed(() => columns.value.some((c) => c.key === 'track'))
const showStage = computed(() => columns.value.some((c) => c.key === 'stage'))

function mobileMetaLine(row: PlayerRecord): string {
  const parts: string[] = []
  if (
    isRecent.value &&
    (isRecordWr(row) || shouldShowGapFromWr(row))
  ) {
    const gap = isRecordWr(row) ? 0 : row.gapFromWr!
    parts.push(gap >= 10 ? `+${Math.round(gap)}` : formatTimeGap(gap, 3))
  }
  if (showSync.value) {
    parts.push(row.sync != null ? `${Math.round(row.sync)}%` : '—')
  }
  if (showDate.value && row.date) {
    const d = new Date(row.date)
    if (!Number.isNaN(d.getTime())) {
      const mm = String(d.getMonth() + 1).padStart(2, '0')
      const dd = String(d.getDate()).padStart(2, '0')
      parts.push(`${mm}/${dd}`)
    }
  } else if (showDate.value) {
    parts.push('—')
  }
  return parts.join(' · ')
}
</script>

<template>
  <div :class="props.bare ? '' : 'px-panel'" class="px-player-records">
    <!-- 桌面：表格 -->
    <div class="px-player-records-desktop px-paged-table-wrap px-paged-table-wrap--scroll-x">
      <table
        class="px-player-records-table w-full min-w-[560px] table-fixed text-left text-sm"
        :aria-busy="loading"
      >
        <colgroup>
          <col
            v-for="col in columns"
            :key="col.key"
            :class="COL_WIDTH_CLASS[col.key]"
          />
        </colgroup>
        <thead class="px-table-head">
          <tr>
            <th
              v-for="col in columns"
              :key="col.key"
              class="px-table-head-cell"
              :class="col.align === 'right' ? 'text-right' : 'text-left'"
            >
              {{ col.label }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="index in rowIndices"
            :key="loading ? `rec-sk-${index}` : rows[index]?.map ?? `rec-empty-${index}`"
            :class="loading || !rows[index] ? 'px-table-row-empty' : 'px-table-row'"
          >
            <template v-if="loading">
              <td v-if="columns[0]?.key === 'map'" class="p-0 align-middle">
                <div class="flex h-14 max-h-14 w-full items-stretch overflow-hidden pr-3 pl-0">
                  <span
                    class="h-14 w-20 shrink-0 animate-pulse bg-neutral-200"
                    aria-hidden="true"
                  />
                  <div class="flex min-w-0 flex-1 items-center py-2">
                    <SkeletonBar class="h-4 w-full max-w-[12rem] shrink-0" />
                  </div>
                </div>
              </td>
              <td
                v-for="col in columns.filter((c) => c.key !== 'map')"
                :key="col.key"
                class="px-table-data-cell"
                :class="[
                  col.align === 'right' ? 'text-right' : '',
                  col.key === 'tier' ? 'px-table-tier-cell' : '',
                ]"
              >
                <div class="px-table-cell-content">
                  <SkeletonBar
                    :class="
                      col.align === 'right'
                        ? 'ml-auto h-4 w-[5.5rem] shrink-0'
                        : col.key === 'tier'
                          ? 'h-4 w-8 shrink-0'
                          : 'h-4 w-16 max-w-full shrink-0'
                    "
                  />
                </div>
              </td>
            </template>
            <template v-else-if="rows[index]">
              <template v-for="col in columns" :key="col.key">
                <td v-if="col.key === 'map'" class="p-0 align-middle">
                  <RouterLink
                    :to="`/maps/${encodeURIComponent(rows[index]!.map)}`"
                    class="px-table-map-link"
                    :title="rows[index]!.map"
                    @click.stop
                  >
                    <MapPreviewImage
                      :map="rows[index]!.map"
                      :image-config="mapImageConfig"
                      variant="thumb"
                    />
                    <span class="px-table-map-link-text">
                      <span class="break-words whitespace-normal font-semibold leading-snug underline-offset-2 hover:underline">{{
                        rows[index]!.map
                      }}</span>
                    </span>
                  </RouterLink>
                </td>
                <td
                  v-else-if="col.key === 'track' || col.key === 'stage'"
                  class="px-table-data-cell font-pixel text-[9px]"
                >
                  <div class="px-table-cell-content">
                    {{
                      col.key === 'track'
                        ? trackLabel(rows[index]!.track)
                        : stageLabel(rows[index]!.stage)
                    }}
                  </div>
                </td>
                <td v-else-if="col.key === 'tier'" class="px-table-data-cell px-table-tier-cell">
                  <div class="px-table-cell-content">
                    <span
                      v-if="rows[index]!.tier != null"
                      :class="['px-chip', tierChipColorClass(rows[index]!.tier!)]"
                    >T{{ rows[index]!.tier }}</span>
                    <span v-else class="text-px-muted">—</span>
                  </div>
                </td>
                <td
                  v-else-if="col.key === 'time'"
                  class="px-table-data-cell text-right font-mono"
                >
                  <div class="px-table-cell-content font-bold leading-tight">
                    {{ rows[index]!.timeFormatted ?? '—' }}
                    <span
                      v-if="
                        isRecent &&
                        (isRecordWr(rows[index]!) || shouldShowGapFromWr(rows[index]!))
                      "
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
                </td>
                <td
                  v-else-if="col.key === 'sync'"
                  class="px-table-data-cell text-right font-mono"
                >
                  <div class="px-table-cell-content">
                    {{
                      rows[index]!.sync != null ? `${Math.round(rows[index]!.sync!)}%` : '—'
                    }}
                  </div>
                </td>
                <td
                  v-else-if="col.key === 'date'"
                  class="px-table-data-cell text-right font-mono text-xs"
                >
                  <div class="px-table-cell-content">
                    {{ rows[index]!.date ? formatRecordDate(rows[index]!.date) : '—' }}
                  </div>
                </td>
              </template>
            </template>
            <td v-else :colspan="colCount" class="px-table-row-empty" aria-hidden="true">
              &nbsp;
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- 手机：卡片列表，无横向滚动 -->
    <div class="px-player-records-mobile" :aria-busy="loading">
      <template v-for="index in rowIndices" :key="loading ? `rec-m-sk-${index}` : rows[index]?.map ?? `rec-m-empty-${index}`">
        <div v-if="loading" class="px-player-record-card px-player-record-card--skeleton" aria-hidden="true">
          <span class="px-player-record-card-thumb animate-pulse bg-neutral-200" />
          <div class="px-player-record-card-body">
            <SkeletonBar class="h-4 w-3/4 max-w-full" />
            <SkeletonBar class="mt-2 h-3 w-16" />
          </div>
          <div class="px-player-record-card-meta">
            <SkeletonBar class="ml-auto h-4 w-14" />
            <SkeletonBar class="mt-1 ml-auto h-3 w-20" />
          </div>
        </div>
        <RouterLink
          v-else-if="rows[index]"
          :to="`/maps/${encodeURIComponent(rows[index]!.map)}`"
          class="px-player-record-card"
        >
          <MapPreviewImage
            :map="rows[index]!.map"
            :image-config="mapImageConfig"
            variant="thumb"
          />
          <div class="px-player-record-card-body">
            <span class="px-player-record-card-name">{{ rows[index]!.map }}</span>
            <div class="px-player-record-card-tags">
              <span
                v-if="rows[index]!.tier != null"
                :class="['px-chip shrink-0', tierChipColorClass(rows[index]!.tier!)]"
              >T{{ rows[index]!.tier }}</span>
              <span v-if="showStage" class="px-player-record-card-kind">{{
                stageLabel(rows[index]!.stage)
              }}</span>
              <span v-else-if="showTrack" class="px-player-record-card-kind">{{
                trackLabel(rows[index]!.track)
              }}</span>
            </div>
          </div>
          <div v-if="showTime || showSync || showDate" class="px-player-record-card-meta">
            <div v-if="showTime" class="px-player-record-card-time">
              {{ rows[index]!.timeFormatted ?? '—' }}
            </div>
            <div v-if="mobileMetaLine(rows[index]!)" class="px-player-record-card-sub">
              {{ mobileMetaLine(rows[index]!) }}
            </div>
          </div>
        </RouterLink>
        <div v-else class="px-player-record-card px-player-record-card--empty" aria-hidden="true" />
      </template>
    </div>
  </div>
</template>
