<script setup lang="ts">
import ServerInfoPanel from './components/ServerInfoPanel.vue'
import { useMapImageConfig } from '../../composables/useMapImageConfig'
import { useServerInfo } from '../../composables/useServerInfo'

const { servers, loading, error } = useServerInfo()
const { config: mapImageConfig } = useMapImageConfig()
</script>

<template>
  <section>
    <header class="mb-6 flex items-end justify-between gap-2">
      <h1 class="px-page-heading text-2xl">服务器</h1>
      <span class="px-en-subtitle">SERVER</span>
    </header>
    <ServerInfoPanel
      :loading="loading"
      :servers="servers"
      :error="error || undefined"
      :show-panel-title="false"
      :map-image-config="mapImageConfig"
    />
  </section>
</template>
