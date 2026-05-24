<script setup lang="ts">
import SkeletonBar from './SkeletonBar.vue'

withDefaults(
  defineProps<{
    rows?: number
  }>(),
  { rows: 12 },
)
</script>

<template>
  <ul class="divide-y-2 divide-px-ink px-panel overflow-hidden">
    <li
      v-for="n in rows"
      :key="n"
      class="flex flex-wrap items-center justify-between gap-2 p-3 sm:p-4"
    >
      <div class="min-w-0 space-y-2">
        <SkeletonBar class="h-5 w-24" />
        <SkeletonBar class="h-4 w-40 max-w-full" />
      </div>
      <div class="shrink-0 space-y-2 text-right">
        <SkeletonBar class="ml-auto h-5 w-16" />
        <SkeletonBar class="ml-auto h-3 w-28" />
      </div>
    </li>
  </ul>
</template>
