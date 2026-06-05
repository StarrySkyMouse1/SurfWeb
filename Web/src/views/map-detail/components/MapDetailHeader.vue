<script setup lang="ts">
import MapPreviewImage from '../../../components/MapPreviewImage.vue'
import type { MapDetail } from '../../../api/client'
import { tierChipColorClass, type MapImageConfig } from '../../../utils/display'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'

defineProps<{
  loading: boolean
  detail?: MapDetail | null
  mapImageConfig?: MapImageConfig | null
}>()
</script>

<template>
  <div class="px-panel overflow-hidden">
    <div
      v-if="loading"
      class="flex flex-col lg:flex-row lg:items-stretch"
      aria-busy="true"
    >
      <div class="min-w-0 flex-1 p-6">
        <div class="flex flex-wrap items-center gap-3">
          <span
            class="px-chip box-border inline-block h-[1.625rem] w-10 animate-pulse border-px-ink bg-neutral-200 shadow-none"
            aria-hidden="true"
          />
          <SkeletonBar class="h-8 w-64 max-w-full" />
        </div>
        <SkeletonBar class="mt-4 h-4 w-56 max-w-full" />
        <SkeletonBar class="mt-1 h-4 w-24 max-w-full" />
      </div>
      <div
        class="relative aspect-[16/9] w-full shrink-0 animate-pulse border-t-2 border-px-ink px-map-placeholder lg:aspect-auto lg:h-full lg:min-h-[9.5rem] lg:w-72 lg:border-t-0 lg:border-l-2 xl:w-80"
        aria-hidden="true"
      />
    </div>
    <div v-else-if="detail" class="flex flex-col lg:flex-row lg:items-stretch">
      <div class="min-w-0 flex-1 p-6">
        <div class="flex flex-wrap items-center gap-3">
          <span :class="['px-chip bg-px-surface', tierChipColorClass(detail.tier)]">
            T{{ detail.tier }}
          </span>
          <h1 class="text-2xl font-black break-all">{{ detail.map }}</h1>
        </div>
        <p class="mt-4 font-mono text-sm">
          WR {{ detail.worldRecordTimeFormatted ?? '—' }}
          <span v-if="detail.worldRecordPlayer"> · {{ detail.worldRecordPlayer }}</span>
        </p>
        <p class="mt-1 text-sm text-px-muted">{{ detail.completions }} 人完成</p>
      </div>
      <MapPreviewImage
        :map="String(detail.map)"
        :image-config="mapImageConfig"
        variant="detail"
      />
    </div>
  </div>
</template>
