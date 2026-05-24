<script setup lang="ts">
/** 与详情页最多「主线 + 6 个 Bonus」一致的占位宽度 */
const TAB_PLACEHOLDERS = ['w-[3.25rem]', 'w-[5.5rem]', 'w-[5.5rem]', 'w-[5.5rem]', 'w-[5.5rem]', 'w-[5.5rem]', 'w-[5.5rem]'] as const
</script>

<template>
  <span
    v-for="(width, index) in TAB_PLACEHOLDERS"
    :key="index"
    :class="[
      'box-border inline-block h-[2.375rem] shrink-0 animate-pulse border-2 border-px-ink bg-neutral-200 shadow-none',
      width,
    ]"
    aria-hidden="true"
  />
</template>
