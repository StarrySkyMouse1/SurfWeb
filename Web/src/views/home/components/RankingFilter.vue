<script setup lang="ts">
import { ref, watch } from 'vue'
import FilterSplitChip from './FilterSplitChip.vue'

/** 与 GET /rankings?type= 一致 */
export type RankingFilterValue = 'points' | 'completions' | 'playtime' | 'wr'

/** 与 GET /rankings?type=wr&wrScope= 一致 */
export type WrRankingScope = 'main' | 'bonus' | 'stage'

/** 与 GET /rankings?type=completions&completionScope= 一致 */
export type CompletionRankingScope = 'main' | 'bonus'

const primaryOptions: { label: string; value: Exclude<RankingFilterValue, 'wr' | 'completions'> }[] = [
  { label: '积分', value: 'points' },
  { label: '时长', value: 'playtime' },
]

const completionScopeOptions = [
  { label: '主线', value: 'main' as const },
  { label: '奖励', value: 'bonus' as const },
]

const wrScopeOptions = [
  { label: '主线', value: 'main' as const },
  { label: '阶段', value: 'stage' as const },
  { label: '奖励', value: 'bonus' as const },
]

const model = defineModel<RankingFilterValue>({ default: 'points' })
const wrScope = defineModel<WrRankingScope>('wrScope', { default: 'main' })
const completionScope = defineModel<CompletionRankingScope>('completionScope', {
  default: 'main',
})

const completionChipRef = ref<InstanceType<typeof FilterSplitChip> | null>(null)
const wrChipRef = ref<InstanceType<typeof FilterSplitChip> | null>(null)

const paddingClass = 'px-2 py-1'

function selectPrimary(value: Exclude<RankingFilterValue, 'wr' | 'completions'>) {
  model.value = value
  completionChipRef.value?.closePopover()
  wrChipRef.value?.closePopover()
}

watch(model, (type) => {
  if (type === 'points' || type === 'playtime') {
    completionChipRef.value?.closePopover()
    wrChipRef.value?.closePopover()
  } else if (type === 'completions') {
    wrChipRef.value?.closePopover()
  } else if (type === 'wr') {
    completionChipRef.value?.closePopover()
  }
})
</script>

<template>
  <div class="px-filter-chip-row">
    <button
      v-for="opt in primaryOptions"
      :key="opt.value"
      type="button"
      :class="['px-chip', paddingClass, model === opt.value ? 'px-chip-active' : '']"
      @click="selectPrimary(opt.value)"
    >
      {{ opt.label }}
    </button>

    <FilterSplitChip
      ref="completionChipRef"
      v-model="model"
      v-model:scope="completionScope"
      prefix="完成"
      filter-value="completions"
      :scope-options="completionScopeOptions"
      popover-label="选择完成范围"
    />

    <FilterSplitChip
      ref="wrChipRef"
      v-model="model"
      v-model:scope="wrScope"
      prefix="WR"
      filter-value="wr"
      :scope-options="wrScopeOptions"
      popover-label="选择 WR 范围"
    />
  </div>
</template>
