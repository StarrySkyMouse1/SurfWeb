<script setup lang="ts">
import { computed } from 'vue'
import type { LeaderboardEntry } from '../../../api/client'
import { bonusTrackLabel, stageLabel } from '../../../utils/mapCheckpointChart'
import MapLeaderboardCard from './MapLeaderboardCard.vue'

const props = defineProps<{
  stages: number[]
  bonusTracks: number[]
  stageRows: LeaderboardEntry[]
  stageTotal: number
  stageLeaderTime: number | null
  stageLoading: boolean
  stageError: string
  bonusRows: LeaderboardEntry[]
  bonusTotal: number
  bonusLeaderTime: number | null
  bonusLoading: boolean
  bonusError: string
}>()

const stageScope = defineModel<number | null>('stageScope', { default: null })
const bonusScope = defineModel<number | null>('bonusScope', { default: null })
const stagePage = defineModel<number>('stagePage', { default: 1 })
const bonusPage = defineModel<number>('bonusPage', { default: 1 })

const hasStageTabs = computed(() => props.stages.length > 0)
const hasBonusTabs = computed(() => props.bonusTracks.length > 0)
</script>

<template>
  <div class="px-map-record-grid">
    <div class="px-map-record-col">
      <div
        class="px-map-record-toolbar"
        role="tablist"
        aria-label="阶段范围"
      >
        <button
          v-for="s in stages"
          v-show="hasStageTabs"
          :key="s"
          type="button"
          role="tab"
          class="px-filter-scope-btn"
          :aria-selected="stageScope === s"
          @click="stageScope = s"
        >
          {{ stageLabel(s) }}
        </button>
      </div>
      <MapLeaderboardCard
        v-model:page="stagePage"
        section-code="STAGE"
        title="阶段记录"
        :rows="stageRows"
        :total="stageTotal"
        :leader-time="stageLeaderTime"
        :loading="stageLoading"
        :error="hasStageTabs ? stageError : ''"
      />
    </div>

    <div class="px-map-record-col">
      <div
        class="px-map-record-toolbar"
        role="tablist"
        aria-label="奖励赛道"
      >
        <button
          v-for="t in bonusTracks"
          v-show="hasBonusTabs"
          :key="t"
          type="button"
          role="tab"
          class="px-filter-scope-btn"
          :aria-selected="bonusScope === t"
          @click="bonusScope = t"
        >
          {{ bonusTrackLabel(t) }}
        </button>
      </div>
      <MapLeaderboardCard
        v-model:page="bonusPage"
        section-code="BONUS"
        title="奖励记录"
        :rows="bonusRows"
        :total="bonusTotal"
        :leader-time="bonusLeaderTime"
        :loading="bonusLoading"
        :error="hasBonusTabs ? bonusError : ''"
      />
    </div>
  </div>
</template>
