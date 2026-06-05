<script setup lang="ts">
import type { LeaderboardEntry, MapCheckpointChart as MapCheckpointChartData } from '../../../api/client'
import MapCheckpointChartPanel from './MapCheckpointChart.vue'
import MapLeaderboardCard from './MapLeaderboardCard.vue'

defineProps<{
  checkpointChart: MapCheckpointChartData | null
  checkpointLoading: boolean
  rows: LeaderboardEntry[]
  total: number
  leaderTime: number | null
  mainLoading: boolean
  mainError: string
}>()

const page = defineModel<number>('page', { default: 1 })
</script>

<template>
  <div class="px-map-main-row">
    <div class="px-map-main-chart-col min-w-0">
      <MapCheckpointChartPanel
        :chart="checkpointChart"
        :loading="checkpointLoading"
        :leader-time="leaderTime"
        :leaderboard-rows="rows"
      />
    </div>
    <div class="px-map-main-table-col min-w-0">
      <MapLeaderboardCard
        v-model:page="page"
        section-code="MAIN"
        title="主线记录"
        hint="排行"
        :rows="rows"
        :total="total"
        :leader-time="leaderTime"
        :loading="mainLoading"
        :error="mainError"
      />
    </div>
  </div>
</template>
