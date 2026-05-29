<script setup lang="ts">
import type { MapDetail } from '../../../api/client'

/** 与详情页最多「主线 + 6 个 Bonus」一致的占位宽度 */
const TAB_PLACEHOLDERS = [
  'w-[3.25rem]',
  'w-[5.5rem]',
  'w-[5.5rem]',
  'w-[5.5rem]',
  'w-[5.5rem]',
  'w-[5.5rem]',
  'w-[5.5rem]',
] as const

defineProps<{
  loading: boolean
  detail?: MapDetail | null
}>()

const track = defineModel<number>('track', { default: 0 })
</script>

<template>
  <div class="flex min-h-[2.375rem] flex-wrap items-center gap-2" :aria-busy="loading">
    <template v-if="loading">
      <span
        v-for="(width, index) in TAB_PLACEHOLDERS"
        :key="index"
        :class="[
          'box-border inline-block h-[2.375rem] shrink-0 animate-pulse border-2 border-px-ink bg-neutral-200 shadow-none',
          width,
        ]"
        aria-hidden="true"
      />
    </template>
    <template v-else-if="detail">
      <button
        type="button"
        class="px-chip px-4 py-1.5"
        :class="track === 0 ? 'px-chip-active' : ''"
        @click="track = 0"
      >
        主线
      </button>
      <button
        v-for="bonusTrack in detail.bonusTracks ?? []"
        :key="bonusTrack"
        type="button"
        class="px-chip px-4 py-1.5"
        :class="track === bonusTrack ? 'px-chip-active' : ''"
        @click="track = bonusTrack"
      >
        Bonus {{ bonusTrack }}
      </button>
    </template>
  </div>
</template>
