<script setup lang="ts">
import type { PlayerSummary } from '../../../api/client'
import { formatPlaytime } from '../../../utils/format'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'

defineProps<{
  loading: boolean
  player?: PlayerSummary | null
}>()
</script>

<template>
  <div class="px-panel p-8" :aria-busy="loading">
    <template v-if="loading">
      <SkeletonBar class="h-9 w-48 max-w-full" />
      <dl class="mt-6 grid gap-4 sm:grid-cols-3">
        <div v-for="n in 3" :key="n" class="px-panel-sm p-4">
          <SkeletonBar class="h-3 w-14" />
          <SkeletonBar class="mt-3 h-7 w-20" />
        </div>
      </dl>
    </template>
    <template v-else-if="player">
      <div class="flex items-end justify-between gap-2">
        <div>
          <h1 class="text-2xl font-bold">{{ player.name ?? player.auth }}</h1>
          <p class="mt-1 text-xs text-px-muted">玩家资料</p>
        </div>
        <span class="font-pixel text-[9px] text-px-muted">PLAYER</span>
      </div>
      <dl class="mt-6 grid gap-4 sm:grid-cols-3">
        <div class="px-panel-sm p-4">
          <dt class="text-xs font-bold text-px-muted">积分</dt>
          <dd class="mt-1 font-mono text-xl font-bold">#{{ player.pointsRank }}</dd>
          <dd class="mt-0.5 text-sm text-px-muted">{{ player.points.toFixed(1) }}</dd>
        </div>
        <div class="px-panel-sm p-4">
          <dt class="text-xs font-bold text-px-muted">游玩时长</dt>
          <dd class="mt-1 font-mono text-xl font-bold">#{{ player.playtimeRank }}</dd>
          <dd class="mt-0.5 text-sm text-px-muted">{{ formatPlaytime(player.playtime) }}</dd>
        </div>
        <div class="px-panel-sm p-4">
          <dt class="text-xs font-bold text-px-muted">完成地图</dt>
          <dd class="mt-1 font-mono text-xl font-bold">#{{ player.completionRank }}</dd>
          <dd class="mt-0.5 text-sm text-px-muted">{{ player.completionCount }} 张</dd>
        </div>
      </dl>
    </template>
  </div>
</template>
