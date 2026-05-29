<script setup lang="ts">
export interface ChipFilterOption {
  label: string
  value: string
}

const {
  options,
  size = 'md',
} = defineProps<{
  options: ChipFilterOption[]
  size?: 'sm' | 'md'
}>()

const model = defineModel<string>({ default: '' })

const paddingClass = size === 'sm' ? 'px-2 py-1' : 'px-3 py-1.5'
</script>

<template>
  <div class="px-filter-chip-row">
    <button
      v-for="opt in options"
      :key="opt.value"
      type="button"
      :class="['px-chip', paddingClass, model === opt.value ? 'px-chip-active' : '']"
      @click="model = opt.value"
    >
      {{ opt.label }}
    </button>
  </div>
</template>
