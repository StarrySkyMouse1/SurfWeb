<script setup lang="ts">
import { ref, watch } from 'vue'
import FilterSplitChip from './FilterSplitChip.vue'

/** 浏览类筛选，与 GET /records/recent?filter= 一致（all 不传参） */
export type RecentBrowseFilter = '' | 'main' | 'stage' | 'bonus'

export type RecentFilterMode = 'browse' | 'wr'

/** WR 子范围，与 GET /records/recent?filter=wr&wrScope= 一致 */
export type RecentWrScope = 'main' | 'stage' | 'bonus'

const browseScopeOptions = [
  { label: '全部', value: '' as const },
  { label: '主线', value: 'main' as const },
  { label: '阶段', value: 'stage' as const },
  { label: '奖励', value: 'bonus' as const },
]

const wrScopeOptions = [
  { label: '主线', value: 'main' as const },
  { label: '阶段', value: 'stage' as const },
  { label: '奖励', value: 'bonus' as const },
]

const mode = defineModel<RecentFilterMode>('mode', { default: 'browse' })
const browseScope = defineModel<RecentBrowseFilter>('browseScope', { default: '' })
const wrScope = defineModel<RecentWrScope>('wrScope', { default: 'main' })

const browseChipRef = ref<InstanceType<typeof FilterSplitChip> | null>(null)
const wrChipRef = ref<InstanceType<typeof FilterSplitChip> | null>(null)

watch(mode, (m) => {
  if (m === 'browse') wrChipRef.value?.closePopover()
  else browseChipRef.value?.closePopover()
})
</script>

<template>
  <div class="px-filter-chip-row">
    <FilterSplitChip
      ref="browseChipRef"
      v-model="mode"
      v-model:scope="browseScope"
      prefix="完成"
      filter-value="browse"
      :scope-options="browseScopeOptions"
      popover-label="选择完成范围"
    />
    <FilterSplitChip
      ref="wrChipRef"
      v-model="mode"
      v-model:scope="wrScope"
      prefix="WR"
      filter-value="wr"
      :scope-options="wrScopeOptions"
      popover-label="选择 WR 范围"
    />
  </div>
</template>
