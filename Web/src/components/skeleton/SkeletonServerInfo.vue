<script setup lang="ts">
import {
  HOME_LIST_PANEL_CLASS,
  SERVER_MAP_CARD_CLASS,
  SERVER_MAP_FRAME_CLASS,
  SERVER_MAP_PLAYERS_GRID_CLASS,
  SERVER_PANEL_BODY_HEIGHT_CLASS,
  SERVER_PANEL_FIXED_CLASS,
  SERVER_PANEL_HEAD_CLASS,
  SERVER_PANEL_TITLE_CLASS,
  SERVER_PLAYERS_CARD_CLASS,
  SERVER_PLAYERS_HEAD_CLASS,
  SERVER_PLAYERS_LIST_CLASS,
} from '../../constants/homeTable'
import { PX_PANEL_SM_CLASS } from '../../constants/pixelTheme'
import SkeletonBar from './SkeletonBar.vue'

withDefaults(
  defineProps<{
    fixedSidebar?: boolean
  }>(),
  { fixedSidebar: false },
)
</script>

<template>
  <div
    :class="[
      HOME_LIST_PANEL_CLASS,
      fixedSidebar ? SERVER_PANEL_FIXED_CLASS : '',
      'flex flex-col',
    ]"
    aria-hidden="true"
  >
    <div :class="SERVER_PANEL_HEAD_CLASS">
      <h2 :class="SERVER_PANEL_TITLE_CLASS">服务器</h2>
    </div>
    <div
      :class="
        fixedSidebar
          ? [SERVER_PANEL_BODY_HEIGHT_CLASS, 'p-6']
          : 'p-6'
      "
    >
      <SkeletonBar class="mb-4 h-6 w-40" />
      <div :class="SERVER_MAP_PLAYERS_GRID_CLASS">
        <div :class="SERVER_MAP_CARD_CLASS">
          <SkeletonBar :class="SERVER_MAP_FRAME_CLASS" />
        </div>
        <div :class="SERVER_PLAYERS_CARD_CLASS">
          <div :class="SERVER_PLAYERS_HEAD_CLASS">
            <SkeletonBar class="h-3 w-20 bg-px-surface/30" />
          </div>
          <ul :class="[SERVER_PLAYERS_LIST_CLASS, 'space-y-2 p-3']">
            <SkeletonBar v-for="n in 5" :key="n" class="h-3 w-full" />
          </ul>
        </div>
      </div>
      <div :class="[PX_PANEL_SM_CLASS, 'mt-4']">
        <SkeletonBar class="h-11 w-full" />
      </div>
    </div>
  </div>
</template>
