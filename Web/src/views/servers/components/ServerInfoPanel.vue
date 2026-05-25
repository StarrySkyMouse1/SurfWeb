<script setup lang="ts">
import { RouterLink } from 'vue-router'
import MapPreviewImage from '../../../components/MapPreviewImage.vue'
import type { ServerInfo } from '../../../composables/useServerInfo'
import {
  joinServer,
  parseServerEndpoint,
  tierChipColorClass,
  type MapImageConfig,
} from '../../../utils/display'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'

withDefaults(
  defineProps<{
    loading?: boolean
    servers: ServerInfo[]
    error?: string
    fixedSidebar?: boolean
    showPanelTitle?: boolean
    mapImageConfig?: MapImageConfig | null
  }>(),
  { loading: false, fixedSidebar: false, showPanelTitle: true },
)

function canJoin(address: string): boolean {
  return parseServerEndpoint(address) !== null
}
</script>

<template>
  <div
    :class="[
      'px-home-list-panel',
      fixedSidebar ? 'px-server-panel-sidebar' : '',
      'flex flex-col',
    ]"
  >
    <div v-if="showPanelTitle" class="px-server-panel-head">
      <h2 class="px-server-panel-title">服务器</h2>
    </div>

    <div
      :class="fixedSidebar ? 'px-server-panel-body' : 'min-h-0 flex-1 overflow-y-auto'"
      :aria-busy="loading"
    >
      <article
        v-if="loading"
        class="flex flex-col gap-4 p-6"
        aria-hidden="true"
      >
        <SkeletonBar class="h-6 w-40" />
        <div class="px-server-map-players-grid">
          <div class="px-server-map-card">
            <SkeletonBar class="px-server-map-frame" />
          </div>
          <div class="px-server-players-card">
            <div class="px-server-players-head">
              <SkeletonBar class="h-3 w-20 bg-px-surface/30" />
            </div>
            <ul class="space-y-2 p-3 px-server-players-list">
              <SkeletonBar v-for="n in 5" :key="n" class="h-3 w-full" />
            </ul>
          </div>
        </div>
        <div class="px-panel-sm">
          <SkeletonBar class="h-11 w-full" />
        </div>
      </article>
      <div v-else-if="error" class="p-4 text-sm">
        {{ error }}
      </div>
      <div
        v-else-if="servers.length === 0"
        class="flex h-full min-h-[240px] items-center justify-center p-6 text-sm text-px-muted lg:min-h-0"
      >
        暂无服务器信息
      </div>
      <div v-else class="divide-y-2 divide-px-ink">
        <article
          v-for="(server, index) in servers"
          :key="`${server.name}-${server.address}-${index}`"
          class="flex flex-col gap-4 p-6"
        >
          <div class="flex flex-wrap items-start justify-between gap-3">
            <div class="flex min-w-0 flex-1 flex-wrap items-center gap-2">
              <RouterLink
                v-if="server.map"
                :to="`/maps/${encodeURIComponent(server.map)}`"
                class="text-xl font-bold leading-tight break-all hover:underline"
                :title="server.map"
              >
                {{ server.map }}
              </RouterLink>
              <h3 v-else class="text-xl font-bold leading-tight text-px-muted">暂无地图</h3>
              <span
                v-if="server.map && server.mapTier != null"
                :class="['px-chip shrink-0 bg-px-surface', tierChipColorClass(server.mapTier)]"
              >
                T{{ server.mapTier }}
              </span>
            </div>
            <span
              class="px-chip shrink-0"
              :class="
                server.online
                  ? 'border-px-accent bg-px-accent-soft text-px-accent'
                  : 'border-px-ink bg-px-paper text-px-muted'
              "
            >
              {{ server.online ? '在线' : '离线' }}
            </span>
          </div>

          <div class="px-server-map-players-grid">
            <div class="px-server-map-card">
              <RouterLink
                v-if="server.map"
                :to="`/maps/${encodeURIComponent(server.map)}`"
                class="block overflow-hidden"
              >
                <MapPreviewImage
                  :map="server.map"
                  :image-config="mapImageConfig"
                  variant="server"
                />
              </RouterLink>
              <div
                v-else
                :class="[
                  'px-server-map-frame',
                  'flex items-center justify-center bg-px-paper text-sm text-px-muted',
                ]"
              >
                暂无地图
              </div>
            </div>

            <div class="px-server-players-card">
              <div class="px-server-players-head">
                在线玩家
                <span v-if="server.online" class="font-mono font-normal opacity-80">
                  · {{ server.onlinePlayers.length }}
                </span>
              </div>
              <ul class="px-server-players-list">
                <template v-if="server.online">
                  <li
                    v-for="(player, playerIndex) in server.onlinePlayers"
                    :key="`${player.name}-${playerIndex}`"
                    class="px-server-player-row"
                  >
                    <RouterLink
                      v-if="player.auth != null"
                      :to="`/players/${player.auth}`"
                      class="min-w-0 truncate font-bold hover:underline"
                      :title="player.name"
                    >
                      {{ player.name }}
                    </RouterLink>
                    <span v-else class="min-w-0 truncate font-bold" :title="player.name">
                      {{ player.name }}
                    </span>
                    <span class="shrink-0 font-mono text-xs text-px-muted">
                      {{ player.durationDisplay }}
                    </span>
                  </li>
                  <li
                    v-if="server.onlinePlayers.length === 0"
                    class="flex min-h-[8rem] items-center justify-center px-3 py-6 text-sm text-px-muted"
                  >
                    暂无玩家
                  </li>
                </template>
                <li
                  v-else
                  class="flex min-h-[8rem] items-center justify-center px-3 py-6 text-sm text-px-muted"
                >
                  服务器离线
                </li>
              </ul>
            </div>
          </div>

          <button
            type="button"
            :class="[
              'px-btn',
              'w-full bg-px-accent py-3 text-sm font-bold text-white',
              !canJoin(server.address) ? 'cursor-not-allowed opacity-50' : '',
            ]"
            :disabled="!canJoin(server.address)"
            @click="joinServer(server.address)"
          >
            加入
          </button>
        </article>
      </div>
    </div>
  </div>
</template>
