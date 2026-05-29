<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

export interface FilterScopeOption {
  label: string
  value: string
}

const props = withDefaults(
  defineProps<{
    /** 左侧主标签；为空时仅显示当前范围文字（如「全部」） */
    prefix?: string
    filterValue: string
    scopeOptions: FilterScopeOption[]
    popoverLabel: string
    /** 左侧 min-width，无 prefix 时略窄 */
    leftMinWidth?: string
  }>(),
  {
    prefix: '',
    leftMinWidth: undefined,
  },
)

const model = defineModel<string>({ required: true })
const scope = defineModel<string>('scope', { required: true })

const popoverOpen = ref(false)
const rootRef = ref<HTMLElement | null>(null)

const isActive = computed(() => model.value === props.filterValue)

const scopeLabel = computed(
  () => props.scopeOptions.find((o) => o.value === scope.value)?.label ?? props.scopeOptions[0]?.label,
)

const leftLabel = computed(() =>
  props.prefix ? `${props.prefix}·${scopeLabel.value}` : scopeLabel.value,
)

const leftMinWidthClass = computed(() => {
  if (props.leftMinWidth) return props.leftMinWidth
  return props.prefix ? 'min-w-[4.25rem]' : 'min-w-[2.5rem]'
})

function selectType() {
  model.value = props.filterValue
  popoverOpen.value = false
}

function toggleScopePopover() {
  model.value = props.filterValue
  popoverOpen.value = !popoverOpen.value
}

function selectScope(value: string) {
  model.value = props.filterValue
  scope.value = value
  popoverOpen.value = false
}

function closePopover() {
  popoverOpen.value = false
}

function onDocumentPointerDown(event: PointerEvent) {
  if (!popoverOpen.value) return
  const root = rootRef.value
  if (root && !root.contains(event.target as Node)) {
    popoverOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('pointerdown', onDocumentPointerDown)
})

onUnmounted(() => {
  document.removeEventListener('pointerdown', onDocumentPointerDown)
})

defineExpose({ closePopover })
</script>

<template>
  <div ref="rootRef" class="relative inline-flex shrink-0 self-center">
    <div class="px-wr-split-chip" :class="isActive ? 'px-wr-split-chip-active' : ''">
      <button
        type="button"
        class="px-wr-split-chip-part px-wr-split-chip-left"
        :class="leftMinWidthClass"
        @click="selectType"
      >
        {{ leftLabel }}
      </button>
      <button
        type="button"
        class="px-wr-split-chip-part px-wr-split-chip-right"
        :aria-expanded="popoverOpen"
        aria-haspopup="listbox"
        :aria-label="popoverLabel"
        @click="toggleScopePopover"
      >
        <span class="px-wr-split-chip-caret" aria-hidden="true">▼</span>
      </button>
    </div>

    <div
      v-if="popoverOpen"
      class="px-filter-popover"
      role="listbox"
      :aria-label="popoverLabel"
    >
      <button
        v-for="opt in scopeOptions"
        :key="opt.value"
        type="button"
        role="option"
        :aria-selected="scope === opt.value"
        :class="[
          'px-filter-popover-item',
          scope === opt.value ? 'px-filter-popover-item-active' : '',
        ]"
        @click="selectScope(opt.value)"
      >
        {{ opt.label }}
      </button>
    </div>
  </div>
</template>
