<script setup lang="ts">
import ChipFilter from '../../../components/ChipFilter.vue'

/** 与 GET /rankings?type= 一致 */
export type RankingFilterValue = 'points' | 'completions' | 'playtime' | 'wr'

const options: { label: string; value: RankingFilterValue }[] = [
  { label: '积分', value: 'points' },
  { label: '完赛', value: 'completions' },
  { label: '时长', value: 'playtime' },
  { label: 'WR', value: 'wr' },
]

const model = defineModel<RankingFilterValue>({ default: 'points' })
</script>

<template>
  <ChipFilter v-model="model" :options="options" size="sm" />
</template>
