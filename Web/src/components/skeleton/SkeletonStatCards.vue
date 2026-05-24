<script setup lang="ts">
import SkeletonBar from './SkeletonBar.vue'
</script>

<template>
  <div class="px-panel p-8">
    <SkeletonBar class="h-9 w-48 max-w-full" />
    <dl class="mt-6 grid gap-4 sm:grid-cols-3">
      <div
        v-for="n in 3"
        :key="n"
        class="px-panel-sm p-4"
      >
        <SkeletonBar class="h-3 w-14" />
        <SkeletonBar class="mt-3 h-7 w-20" />
      </div>
    </dl>
  </div>
</template>
