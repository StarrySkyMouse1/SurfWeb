<script setup lang="ts">
import SkeletonBar from './SkeletonBar.vue'
import { PX_PANEL_SM_CLASS } from '../../constants/pixelTheme'
</script>

<template>
  <div :class="[PX_PANEL_SM_CLASS, 'block overflow-hidden']">
    <!-- 与 MapPreviewImage variant=card 一致 -->
    <div
      class="relative aspect-[16/9] w-full shrink-0 animate-pulse border-b-2 border-px-ink px-map-placeholder"
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
  </div>
</template>
