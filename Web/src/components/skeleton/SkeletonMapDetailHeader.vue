<script setup lang="ts">
import SkeletonBar from './SkeletonBar.vue'
</script>

<template>
  <div class="flex flex-col lg:flex-row lg:items-stretch">
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
</template>
