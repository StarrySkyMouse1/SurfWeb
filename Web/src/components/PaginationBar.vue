<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    page: number
    pageSize: number
    total: number
    attached?: boolean
    /** 表格加载中时仍显示分页栏，按钮禁用 */
    loading?: boolean
  }>(),
  { attached: false, loading: false },
)

const emit = defineEmits<{
  'update:page': [page: number]
}>()

const showNav = computed(() => props.loading || props.total > 0)

const totalPages = computed(() => Math.max(1, Math.ceil(props.total / props.pageSize)))
const hasKnownTotal = computed(() => props.total > 0)
const canPrev = computed(() => !props.loading && props.page > 1)
const canNext = computed(() => !props.loading && props.page < totalPages.value)

function goTo(next: number) {
  const clamped = Math.min(Math.max(1, next), totalPages.value)
  if (clamped !== props.page) emit('update:page', clamped)
}
</script>

<template>
  <nav v-if="showNav" class="px-pagination-nav" aria-label="分页">
    <p class="min-w-[13rem] tabular-nums text-px-muted">
      <template v-if="loading && !hasKnownTotal">
        共 — 条 · 第 {{ page }} / — 页
      </template>
      <template v-else-if="total > 0">
        共 {{ total }} 条 · 第 {{ page }} / {{ totalPages }} 页
      </template>
      <template v-else>第 {{ page }} 页</template>
    </p>
    <div class="flex gap-2">
      <button
        type="button"
        :class="[
          'px-pagination-btn',
          'disabled:cursor-not-allowed disabled:opacity-40',
          !canPrev && 'bg-px-paper',
        ]"
        :disabled="!canPrev"
        @click="goTo(page - 1)"
      >
        上一页
      </button>
      <button
        type="button"
        :class="[
          'px-pagination-btn',
          'disabled:cursor-not-allowed disabled:opacity-40',
          !canNext && 'bg-px-paper',
        ]"
        :disabled="!canNext"
        @click="goTo(page + 1)"
      >
        下一页
      </button>
    </div>
  </nav>
</template>
