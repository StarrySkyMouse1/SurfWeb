<script setup lang="ts">
defineProps<{
  class?: string
}>()
</script>

<template>
  <span
    class="inline-block animate-pulse rounded-sm bg-neutral-200"
    :class="$props.class"
    aria-hidden="true"
  />
</template>
