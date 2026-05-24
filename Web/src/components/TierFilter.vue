<script setup lang="ts">
import { PX_CHIP_ACTIVE_CLASS, PX_CHIP_CLASS } from '../constants/pixelTheme'

const tiers = [
  { label: '全部', value: '' },
  { label: 'T1', value: '1' },
  { label: 'T2', value: '2' },
  { label: 'T3', value: '3' },
  { label: 'T4', value: '4' },
  { label: 'T5', value: '5' },
  { label: 'T6', value: '6' },
  { label: 'T7', value: '7' },
  { label: 'T8', value: '8' },
  { label: 'T0', value: '0' },
]

const model = defineModel<string>({ default: '' })
</script>

<template>
  <div class="flex flex-wrap gap-2">
    <button
      v-for="t in tiers"
      :key="t.value"
      type="button"
      :class="[PX_CHIP_CLASS, 'px-3 py-1.5', model === t.value ? PX_CHIP_ACTIVE_CLASS : '']"
      @click="model = t.value"
    >
      {{ t.label }}
    </button>
  </div>
</template>
