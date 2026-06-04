<script setup lang="ts">
export type MapDetailCategory = 'main' | 'stage-bonus'

const model = defineModel<MapDetailCategory>({ default: 'main' })

defineProps<{
  loading?: boolean
}>()
</script>

<template>
  <div
    class="flex min-h-[2.375rem] flex-wrap items-center gap-2"
    role="tablist"
    aria-label="地图记录分类"
    :aria-busy="loading"
  >
    <template v-if="loading">
      <span
        v-for="i in 2"
        :key="i"
        class="box-border inline-block h-[2.375rem] w-24 shrink-0 animate-pulse border-2 border-px-ink bg-neutral-200"
        aria-hidden="true"
      />
    </template>
    <template v-else>
      <button
        type="button"
        role="tab"
        class="px-chip px-4 py-1.5"
        :class="model === 'main' ? 'px-chip-active' : ''"
        :aria-selected="model === 'main'"
        @click="model = 'main'"
      >
        主线
      </button>
      <button
        type="button"
        role="tab"
        class="px-chip px-4 py-1.5"
        :class="model === 'stage-bonus' ? 'px-chip-active' : ''"
        :aria-selected="model === 'stage-bonus'"
        @click="model = 'stage-bonus'"
      >
        阶段·奖励
      </button>
    </template>
  </div>
</template>
