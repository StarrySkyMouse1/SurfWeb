<script setup lang="ts">
import ChipFilter from '../../../components/ChipFilter.vue'

/** 与 GET /records/recent?filter= 一致 */
export type RecentRecordFilterValue = '' | 'main' | 'bonus' | 'wr'

const options: { label: string; value: RecentRecordFilterValue }[] = [
  { label: '全部', value: '' },
  { label: '主线', value: 'main' },
  { label: '奖励', value: 'bonus' },
  { label: 'WR', value: 'wr' },
]

const model = defineModel<RecentRecordFilterValue>({ default: '' })
</script>

<template>
  <ChipFilter v-model="model" :options="options" size="sm" />
</template>
