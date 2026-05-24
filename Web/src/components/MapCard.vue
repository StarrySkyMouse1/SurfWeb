<script setup lang="ts">
import { RouterLink } from 'vue-router'
import MapPreviewImage from './MapPreviewImage.vue'
import type { MapListItem } from '../api/client'
import type { MapImageConfig } from '../utils/mapImageUrl'
import { PX_CARD_INTERACTIVE_CLASS, tierChipColorClass } from '../constants/pixelTheme'

defineProps<{
  item: MapListItem
  imageConfig?: MapImageConfig | null
}>()
</script>

<template>
  <RouterLink
    :to="`/maps/${encodeURIComponent(item.map)}`"
    :class="PX_CARD_INTERACTIVE_CLASS"
  >
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
  </RouterLink>
</template>
