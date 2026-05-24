<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { buildMapImageUrl, type MapImageConfig } from '../utils/mapImageUrl'

const props = defineProps<{
  map: string
  imageConfig?: MapImageConfig | null
  /** card：地图卡片；detail：详情头图；server：服务器页左侧缩略图 */
  variant?: 'card' | 'detail' | 'server'
}>()

const imageFailed = ref(false)

const previewUrl = computed(() => buildMapImageUrl(props.map, props.imageConfig))

watch(
  () => [props.map, props.imageConfig?.baseUrl, props.imageConfig?.extension],
  () => {
    imageFailed.value = false
  },
)

const frameClass = computed(() => {
  if (props.variant === 'detail') {
    return 'relative aspect-[16/9] w-full shrink-0 px-map-placeholder lg:aspect-auto lg:h-full lg:min-h-[9.5rem] lg:w-72 xl:w-80'
  }
  if (props.variant === 'server') {
    return 'relative aspect-[16/9] w-full px-map-placeholder'
  }
  return 'relative aspect-[16/9] w-full shrink-0 px-map-placeholder'
})

const borderClass = computed(() => {
  if (props.variant === 'detail') return 'border-t-2 border-px-ink lg:border-t-0 lg:border-l-2'
  if (props.variant === 'server') return ''
  return 'border-b-2 border-px-ink'
})
</script>

<template>
  <div :class="[frameClass, borderClass]">
    <img
      v-if="previewUrl && !imageFailed"
      :src="previewUrl"
      :alt="map"
      class="absolute inset-0 h-full w-full object-cover"
      loading="lazy"
      @error="imageFailed = true"
    />
    <div
      v-else
      class="absolute inset-0 flex items-center justify-center text-sm font-bold text-neutral-500"
    >
      无预览
    </div>
  </div>
</template>
