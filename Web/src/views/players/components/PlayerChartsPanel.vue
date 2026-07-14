<script setup lang="ts">
import { computed } from 'vue'
import type { PlayerCharts } from '../../../api/client'
import { paintChartBars } from '../../../utils/playerCharts'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'

const props = defineProps<{
  loading: boolean
  charts?: PlayerCharts | null
  panelTitle: string
}>()

const primaryPainted = computed(() =>
  props.charts ? paintChartBars(props.charts.primaryBars) : [],
)
const tierPainted = computed(() => (props.charts ? paintChartBars(props.charts.tierBars) : []))

const isMonthly = computed(() =>
  (props.charts?.primaryBars.length ?? 0) >= 12,
)

const isTierDense = computed(() => (props.charts?.tierBars.length ?? 0) >= 9)

/** 窄屏月份轴：去掉「月」字，避免 10/11/12 互相挤压 */
function axisLabel(label: string, compact: boolean): string {
  if (!compact) return label
  return label.replace(/月$/u, '')
}
</script>

<template>
  <div class="px-chart-panel" aria-label="图表概览">
    <div class="px-chart-panel-head">
      <span class="text-xs font-semibold">{{ panelTitle }}</span>
      <span class="hidden text-[10px] font-semibold opacity-70 sm:inline">图表</span>
    </div>
    <div class="px-chart-panel-body">
      <template v-if="loading">
        <div v-for="n in 2" :key="n" class="px-chart-block">
          <SkeletonBar class="mb-3 h-3 w-32" />
          <SkeletonBar class="h-[var(--px-chart-plot-h)] w-full" />
        </div>
      </template>
      <template v-else-if="charts">
        <div class="px-chart-block">
          <p class="mb-3 text-[11px] font-bold text-px-muted">{{ charts.primaryTitle }}</p>
          <div
            class="px-chart-bars"
            :class="{ 'px-chart-bars--months': isMonthly }"
            role="img"
          >
            <div
              v-for="(bar, i) in primaryPainted"
              :key="`p-${i}`"
              class="px-chart-bar-col"
            >
              <div class="px-chart-bar-cell">
                <span
                  class="px-chart-bar-value mb-0.5 font-mono text-[9px] font-bold"
                  :class="bar.valueClass"
                >{{ bar.value }}</span>
                <div
                  class="px-chart-bar"
                  :class="{ accent: bar.tone === 'accent', muted: bar.tone === 'muted' }"
                  :style="{ height: `${bar.heightPct}%` }"
                />
              </div>
              <span class="px-chart-axis-label font-pixel text-px-muted">
                <span class="px-chart-axis-label-full">{{ bar.label }}</span>
                <span class="px-chart-axis-label-short">{{ axisLabel(bar.label, true) }}</span>
              </span>
            </div>
          </div>
          <div
            v-if="charts.primaryFooterLeft || charts.primaryFooterRight"
            class="mt-2 flex flex-wrap justify-between gap-2 text-[10px] text-px-muted"
          >
            <span v-if="charts.primaryFooterLeft">{{ charts.primaryFooterLeft }}</span>
            <span v-if="charts.primaryFooterRight">{{ charts.primaryFooterRight }}</span>
          </div>
        </div>

        <div class="px-chart-block">
          <p class="mb-3 text-[11px] font-bold text-px-muted">{{ charts.tierTitle }}</p>
          <div
            class="px-chart-bars"
            :class="{ 'px-chart-bars--months': isTierDense }"
            role="img"
          >
            <div
              v-for="(bar, i) in tierPainted"
              :key="`t-${i}`"
              class="px-chart-bar-col"
            >
              <div class="px-chart-bar-cell">
                <span
                  class="px-chart-bar-value mb-0.5 font-mono text-[9px] font-bold"
                  :class="bar.valueClass"
                >{{ bar.value }}</span>
                <div
                  class="px-chart-bar"
                  :class="{ accent: bar.tone === 'accent', muted: bar.tone === 'muted' }"
                  :style="{ height: `${bar.heightPct}%` }"
                />
              </div>
              <span class="px-chart-axis-label font-pixel text-px-muted">
                <span class="px-chart-axis-label-full">{{ bar.label }}</span>
                <span class="px-chart-axis-label-short">{{ axisLabel(bar.label, true) }}</span>
              </span>
            </div>
          </div>
          <p v-if="charts.topTierLabel" class="mt-2 text-[10px] text-px-muted">
            最多 Tier {{ charts.topTierLabel }}
          </p>
        </div>
      </template>
    </div>
  </div>
</template>
