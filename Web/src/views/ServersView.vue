<script setup lang="ts">
import ServerInfoPanel from '../components/ServerInfoPanel.vue'
import SkeletonServerInfo from '../components/skeleton/SkeletonServerInfo.vue'
import { useMapImageConfig } from '../composables/useMapImageConfig'
import { useServerInfo } from '../composables/useServerInfo'
import { PX_PAGE_HEADING_CLASS } from '../constants/pixelTheme'

const { servers, loading, error } = useServerInfo()
const { config: mapImageConfig } = useMapImageConfig()
</script>

<template>
  <section>
    <header class="mb-6 flex items-end justify-between gap-2">
      <h1 :class="[PX_PAGE_HEADING_CLASS, 'text-2xl']">服务器</h1>
      <span class="font-pixel text-[9px] text-px-muted">SERVER</span>
    </header>
    <SkeletonServerInfo v-if="loading" />
    <ServerInfoPanel
      v-else
      :servers="servers"
      :error="error || undefined"
      :show-panel-title="false"
      :map-image-config="mapImageConfig"
    />
  </section>
</template>
