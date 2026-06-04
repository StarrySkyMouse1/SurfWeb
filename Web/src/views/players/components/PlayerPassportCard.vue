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
  <article class="px-panel px-player-passport" :aria-busy="loading">
    <p
      v-if="!loading && player"
      class="px-player-passport-watermark"
      aria-hidden="true"
    >
      {{ player.auth }}
    </p>

    <template v-if="loading">
      <header class="px-player-passport-header">
        <div class="px-player-passport-header-main w-full">
          <SkeletonBar class="h-8 w-40 max-w-full" />
          <SkeletonBar class="mt-2 h-3 w-16" />
        </div>
      </header>
      <div class="px-player-passport-bento">
        <div v-for="n in 3" :key="n" class="px-player-passport-tile-stack">
          <SkeletonBar class="h-[3.375rem] w-full" />
          <SkeletonBar class="h-[2.625rem] w-full" />
        </div>
      </div>
    </template>

    <template v-else-if="player">
      <header class="px-player-passport-header">
        <div class="px-player-passport-header-main">
          <h2 class="px-player-passport-header-id">{{ player.name ?? player.auth }}</h2>
          <p class="px-player-passport-header-sub">玩家资料</p>
        </div>
      </header>

      <div class="px-player-passport-bento">
        <div class="px-player-passport-tile-stack">
          <article class="px-player-passport-tile">
            <p class="px-player-passport-tile-label">积分</p>
            <p class="px-player-passport-tile-rank">#{{ player.pointsRank }}</p>
            <p class="px-player-passport-tile-val">{{ player.points.toFixed(1) }}</p>
          </article>
          <div class="px-player-passport-medal-row px-player-passport-medal-row--single" aria-label="游戏时长">
            <div class="px-player-passport-medal">
              <span class="px-player-passport-medal-rank">#{{ player.playtimeRank }}</span>
              <span class="px-player-passport-medal-body">
                <span class="px-player-passport-medal-label">游戏时长</span>
                <span class="px-player-passport-medal-val">{{ formatPlaytime(player.playtime) }}</span>
              </span>
            </div>
          </div>
        </div>

        <div class="px-player-passport-tile-stack">
          <article class="px-player-passport-tile">
            <p class="px-player-passport-tile-label">完成主线</p>
            <p class="px-player-passport-tile-rank">#{{ player.mainCompletionRank }}</p>
            <p class="px-player-passport-tile-val">{{ player.mainCompletionCount }}</p>
          </article>
          <div class="px-player-passport-medal-row px-player-passport-medal-row--single" aria-label="完成奖励">
            <div class="px-player-passport-medal">
              <span class="px-player-passport-medal-rank">#{{ player.bonusCompletionRank }}</span>
              <span class="px-player-passport-medal-body">
                <span class="px-player-passport-medal-label">完成奖励</span>
                <span class="px-player-passport-medal-val">{{ player.bonusCompletionCount }}</span>
              </span>
            </div>
          </div>
        </div>

        <div class="px-player-passport-tile-stack px-player-passport-tile-stack--wr">
          <article class="px-player-passport-tile">
            <p class="px-player-passport-tile-label">WR 数量</p>
            <p class="px-player-passport-tile-rank">#{{ player.wrRank }}</p>
            <p class="px-player-passport-tile-val">{{ player.wrCount }}</p>
          </article>
          <div
            class="px-player-passport-medal-row px-player-passport-medal-row--triple"
            role="list"
            aria-label="WR 分类"
          >
            <div class="px-player-passport-medal" role="listitem">
              <span class="px-player-passport-medal-rank">#{{ player.mainWrRank }}</span>
              <span class="px-player-passport-medal-body">
                <span class="px-player-passport-medal-label">主线 WR</span>
                <span class="px-player-passport-medal-val">{{ player.mainWrCount }}</span>
              </span>
            </div>
            <div class="px-player-passport-medal" role="listitem">
              <span class="px-player-passport-medal-rank">#{{ player.stageWrRank }}</span>
              <span class="px-player-passport-medal-body">
                <span class="px-player-passport-medal-label">阶段 WR</span>
                <span class="px-player-passport-medal-val">{{ player.stageWrCount }}</span>
              </span>
            </div>
            <div class="px-player-passport-medal" role="listitem">
              <span class="px-player-passport-medal-rank">#{{ player.bonusWrRank }}</span>
              <span class="px-player-passport-medal-body">
                <span class="px-player-passport-medal-label">奖励 WR</span>
                <span class="px-player-passport-medal-val">{{ player.bonusWrCount }}</span>
              </span>
            </div>
          </div>
        </div>
      </div>
    </template>
  </article>
</template>
