<script setup lang="ts">
import { ref, watch } from 'vue'
import type { PlayerRecordCategory, PlayerRecordScope } from '../../../api/client'
import FilterSplitChip from '../../home/components/FilterSplitChip.vue'

const category = defineModel<PlayerRecordCategory>('category', { required: true })
const scope = defineModel<PlayerRecordScope>('scope', { required: true })
const incompleteTier = defineModel<string>('incompleteTier', { default: '' })

const incompleteTierOptions = [
  { label: '全部', value: '' },
  ...Array.from({ length: 9 }, (_, i) => ({
    label: `T${i}`,
    value: String(i),
  })),
]

const scopes: { value: PlayerRecordScope; label: string }[] = [
  { value: 'main', label: '主线' },
  { value: 'stage', label: '阶段' },
  { value: 'bonus', label: '奖励' },
]

const incompleteChipRef = ref<InstanceType<typeof FilterSplitChip> | null>(null)

function selectCategory(value: PlayerRecordCategory) {
  category.value = value
  if (value !== 'incomplete') incompleteChipRef.value?.closePopover()
}

function selectScope(value: PlayerRecordScope) {
  scope.value = value
}

watch(category, (c) => {
  if (c !== 'incomplete') incompleteChipRef.value?.closePopover()
})
</script>

<template>
  <div class="px-filter-main" role="tablist" aria-label="记录大类">
    <button
      type="button"
      class="px-filter-main-btn"
      role="tab"
      :aria-selected="category === 'recent'"
      @click="selectCategory('recent')"
    >
      近期记录
    </button>
    <button
      type="button"
      class="px-filter-main-btn"
      role="tab"
      :aria-selected="category === 'wr'"
      @click="selectCategory('wr')"
    >
      WR
    </button>
    <FilterSplitChip
      ref="incompleteChipRef"
      v-model="category"
      v-model:scope="incompleteTier"
      class="px-filter-main-split"
      prefix="未完成"
      filter-value="incomplete"
      :scope-options="incompleteTierOptions"
      popover-label="选择 Tier"
    />
  </div>
  <div class="px-filter-scope" role="tablist" aria-label="主线 / 阶段 / 奖励">
    <button
      v-for="opt in scopes"
      :key="opt.value"
      type="button"
      class="px-filter-scope-btn"
      role="tab"
      :aria-selected="scope === opt.value"
      @click="selectScope(opt.value)"
    >
      {{ opt.label }}
    </button>
  </div>
</template>
