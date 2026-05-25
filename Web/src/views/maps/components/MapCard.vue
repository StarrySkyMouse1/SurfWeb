<script setup lang="ts">
import { RouterLink } from 'vue-router'
import MapPreviewImage from '../../../components/MapPreviewImage.vue'
import type { MapListItem } from '../../../api/client'
import { tierChipColorClass, type MapImageConfig } from '../../../utils/display'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'

defineProps<{
  loading?: boolean
  item?: MapListItem
  imageConfig?: MapImageConfig | null
}>()
</script>

<template>
  <component
    :is="loading ? 'div' : RouterLink"
    :to="loading || !item ? undefined : `/maps/${encodeURIComponent(item.map)}`"
    :class="loading ? 'px-panel-sm block overflow-hidden' : 'px-card-link'"
    :aria-busy="loading"
  >
    <template v-if="loading">
      <div
        class="relative aspect-[16/9] w-full shrink-0 animate-pulse border-b-2 border-px-ink px-map-placeholder"
        aria-hidden="true"
      />
      <div class="p-4">
        <div class="flex items-start justify-between gap-2">
          <span
            class="px-chip animate-pulse border-px-ink bg-neutral-200 font-pixel text-[10px] leading-none text-transparent shadow-none"
            aria-hidden="true"
          >T0</span>
          <SkeletonBar class="h-[11px] w-14" />
        </div>
        <SkeletonBar class="mt-3 h-6 w-3/4 max-w-full" />
        <SkeletonBar class="mt-2 h-4 w-2/3 max-w-full" />
      </div>
    </template>
    <template v-else-if="item">
      <MapPreviewImage :map="item.map" :image-config="imageConfig" variant="card" />
      <div class="p-4">
        <div class="flex items-start justify-between gap-2">
          <span
            :class="[
              'px-chip bg-px-surface',
              tierChipColorClass(item.tier),
            ]"
          >
            T{{ item.tier }}
          </span>
          <span class="font-mono text-[11px] text-px-muted">{{ item.completions }} 完赛</span>
        </div>
        <h3 class="mt-3 truncate font-bold">{{ item.map }}</h3>
        <p class="mt-2 min-h-4 font-mono text-xs leading-4">
          <template v-if="item.worldRecordTimeFormatted">
            WR <span class="font-bold">{{ item.worldRecordTimeFormatted }}</span>
            <span v-if="item.worldRecordPlayer" class="font-sans"> · {{ item.worldRecordPlayer }}</span>
          </template>
        </p>
      </div>
    </template>
  </component>
</template>
