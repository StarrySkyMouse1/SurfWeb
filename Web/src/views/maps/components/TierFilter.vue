<script setup lang="ts">
import ChipFilter from '../../../components/ChipFilter.vue'

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
  <ChipFilter v-model="model" :options="tiers" />
</template>
