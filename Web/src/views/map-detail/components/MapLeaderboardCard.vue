<script setup lang="ts">
import type { LeaderboardEntry } from '../../../api/client'
import LeaderboardTable from './LeaderboardTable.vue'
import PaginationBar from '../../../components/PaginationBar.vue'

withDefaults(
  defineProps<{
    sectionCode: string
    title: string
    hint?: string
    rows: LeaderboardEntry[]
    total: number
    pageSize?: number
    leaderTime?: number | null
    loading?: boolean
    error?: string
  }>(),
  { pageSize: 10, loading: false, hint: '排行' },
)

const page = defineModel<number>('page', { default: 1 })
</script>

<template>
  <section class="px-panel overflow-hidden">
    <div
      class="flex flex-wrap items-end justify-between gap-3 border-b-2 border-px-ink bg-px-ink px-4 py-2.5 text-px-surface"
    >
      <div>
        <p class="font-pixel text-[10px] uppercase tracking-wide opacity-70">{{ sectionCode }}</p>
        <p class="text-sm font-bold">{{ title }}</p>
      </div>
      <p class="font-mono text-[10px] opacity-75">{{ hint }}</p>
    </div>

    <p v-if="error && !loading" class="px-4 py-3 text-sm">{{ error }}</p>
    <template v-else>
      <div class="px-paged-table-wrap">
        <LeaderboardTable
          bare
          :rows="rows"
          :page-size="pageSize"
          :leader-time="leaderTime"
          :loading="loading"
        />
      </div>
      <PaginationBar
        v-model:page="page"
        attached
        :page-size="pageSize"
        :total="total"
        :loading="loading"
      />
    </template>
  </section>
</template>
