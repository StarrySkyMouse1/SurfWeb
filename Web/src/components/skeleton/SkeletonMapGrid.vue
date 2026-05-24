<script setup lang="ts">
import SkeletonMapCard from './SkeletonMapCard.vue'

withDefaults(
  defineProps<{
    count?: number
  }>(),
  { count: 12 },
)
</script>

<template>
  <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
    <SkeletonMapCard v-for="n in count" :key="n" />
  </div>
</template>
