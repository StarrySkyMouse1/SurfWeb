<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import * as echarts from 'echarts/core'
import { LineChart } from 'echarts/charts'
import {
  GridComponent,
  TooltipComponent,
} from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'
import type { ECharts } from 'echarts/core'
import type { LeaderboardEntry, MapCheckpointChart } from '../../../api/client'
import { formatTimeGap } from '../../../utils/format'
import {
  MAP_CP_RANK_COLORS,
  computeCpYMax,
  formatCpAxisTick,
  cpChartTooltipPosition,
  cpSeriesDisplayName,
  cpTooltipGapSeconds,
  hasCheckpointSeriesData,
  leaderboardTimeByAuth,
} from '../../../utils/mapCheckpointChart'
import SkeletonBar from '../../../components/skeleton/SkeletonBar.vue'

echarts.use([LineChart, GridComponent, TooltipComponent, CanvasRenderer])

const props = withDefaults(
  defineProps<{
    chart: MapCheckpointChart | null
    /** 主线榜第一名用时，终点 tooltip 与右侧榜 +X.XX 对齐 */
    leaderTime?: number | null
    /** 当前页主线榜行，用于终点差距与玩家名 */
    leaderboardRows?: LeaderboardEntry[]
    loading?: boolean
  }>(),
  { loading: false, leaderTime: null, leaderboardRows: () => [] },
)

const finishTimeByAuth = computed(() => leaderboardTimeByAuth(props.leaderboardRows))

const nameByAuth = computed(() => {
  const map = new Map<number, string>()
  for (const row of props.leaderboardRows) {
    if (row.playerName) map.set(row.auth, row.playerName)
  }
  return map
})

const rootRef = ref<HTMLElement | null>(null)

const hasChartPlot = computed(
  () => (props.chart?.checkpointLabels.length ?? 0) > 0
    && props.chart?.series.some((s) => hasCheckpointSeriesData(s)),
)
let instance: ECharts | null = null

function dispose() {
  instance?.dispose()
  instance = null
}

function render() {
  const el = rootRef.value
  if (!el || props.loading) return
  const data = props.chart
  if (!data?.series.length || !hasChartPlot.value) {
    dispose()
    return
  }

  if (!instance) instance = echarts.init(el, undefined, { renderer: 'canvas' })

  const yMax = computeCpYMax(data)
  const leaderTime = props.leaderTime
  const finishTimes = finishTimeByAuth.value
  const names = nameByAuth.value
  const series = data.series.map((p, i) => {
    const color = MAP_CP_RANK_COLORS[i] ?? MAP_CP_RANK_COLORS[9]
    const top = i === 0
    return {
      name: cpSeriesDisplayName(p, names),
      type: 'line' as const,
      data: p.cumulativeSeconds,
      showSymbol: true,
      symbol: 'circle',
      symbolSize: top ? 7 : 5,
      z: 10 - i,
      lineStyle: { width: top ? 2.5 : 2, color },
      itemStyle: {
        color: '#ffffff',
        borderColor: color,
        borderWidth: 2,
      },
      emphasis: {
        focus: 'series' as const,
        lineStyle: { width: top ? 3 : 2.5, color },
      },
    }
  })

  instance.setOption(
    {
      animation: false,
      grid: { left: 48, right: 36, top: 16, bottom: 28 },
      xAxis: {
        type: 'category',
        data: data.checkpointLabels,
        boundaryGap: ['4%', '12%'],
        axisLine: { lineStyle: { color: '#121212', width: 2 } },
        axisTick: { show: false },
        axisLabel: {
          color: '#121212',
          fontFamily: 'Silkscreen, monospace',
          fontSize: 10,
          margin: 10,
        },
      },
      yAxis: {
        type: 'value',
        min: 0,
        max: yMax,
        splitNumber: 5,
        axisLine: { show: false },
        axisTick: { show: false },
        axisLabel: {
          color: '#121212',
          fontFamily: '"JetBrains Mono", monospace',
          fontSize: 12,
          fontWeight: 700,
          formatter: (v: number) => formatCpAxisTick(v),
        },
        splitLine: { lineStyle: { color: 'rgba(18, 18, 18, 0.1)' } },
      },
      tooltip: {
        trigger: 'axis',
        appendToBody: false,
        confine: true,
        className: 'px-map-cp-echarts-tooltip',
        position: cpChartTooltipPosition,
        backgroundColor: '#121212',
        borderColor: '#121212',
        borderWidth: 2,
        padding: [8, 10],
        extraCssText:
          'box-shadow:4px 4px 0 #121212;border-radius:0;z-index:1;overflow:visible;',
        textStyle: { color: '#f3f1eb', fontSize: 12 },
        axisPointer: { type: 'line', lineStyle: { color: '#3d5afe', width: 2 } },
        formatter(params: unknown) {
          const list = params as { axisValue: string; dataIndex: number; value: number; seriesName: string; seriesIndex: number; color: string }[]
          if (!list?.length) return ''
          const cpIndex = list[0].dataIndex
          const title = list[0].axisValue
          const rows = [...list]
            .filter((item) => item.value != null && !Number.isNaN(item.value))
            .sort((a, b) => {
              const gapA = cpTooltipGapSeconds(
                title, a.seriesIndex, cpIndex, a.value, data.series, leaderTime, finishTimes,
              )
              const gapB = cpTooltipGapSeconds(
                title, b.seriesIndex, cpIndex, b.value, data.series, leaderTime, finishTimes,
              )
              return gapA - gapB
            })
            .map((item) => {
              const lineColor = MAP_CP_RANK_COLORS[item.seriesIndex] ?? item.color
              const gapSec = cpTooltipGapSeconds(
                title, item.seriesIndex, cpIndex, item.value, data.series, leaderTime, finishTimes,
              )
              const gap = formatTimeGap(gapSec)
              return (
                `<div style="margin-top:4px;display:flex;align-items:center">` +
                `<span style="display:inline-block;width:10px;height:${item.seriesIndex === 0 ? 3 : 2}px;background:${lineColor};margin-right:6px"></span>` +
                `<span style="font-family:JetBrains Mono,monospace;font-size:11px;margin-right:8px;min-width:4.25rem">${gap}</span>` +
                `<span style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${item.seriesName}</span></div>`
              )
            })
          return (
            `<div style="font-family:Silkscreen,monospace;font-size:11px;letter-spacing:.04em;padding-bottom:6px;margin-bottom:6px;border-bottom:1px solid rgba(255,255,255,.22)">` +
            `${title}</div>${rows.join('')}`
          )
        },
      },
      series,
    },
    true,
  )
  instance.resize()
}

function onResize() {
  instance?.resize()
}

onMounted(() => {
  render()
  window.addEventListener('resize', onResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize)
  dispose()
})

watch(
  () => [props.chart, props.loading, hasChartPlot.value, props.leaderTime, props.leaderboardRows] as const,
  () => {
    requestAnimationFrame(render)
  },
  { deep: true },
)

defineExpose({ resize: onResize })
</script>

<template>
  <section class="px-panel overflow-hidden">
    <div
      class="flex flex-wrap items-end justify-between gap-3 border-b-2 border-px-ink bg-px-ink px-4 py-2.5 text-px-surface"
    >
      <div>
        <p class="font-pixel text-[10px] uppercase tracking-wide opacity-70">MAIN · CP</p>
        <p class="text-sm font-bold">检查点差异</p>
      </div>
      <p class="font-mono text-[10px] opacity-75">TOP10</p>
    </div>
    <div class="px-map-cp-chart-body p-3">
      <div v-if="loading" class="px-map-cp-chart-area" aria-busy="true">
        <SkeletonBar class="h-full w-full" />
      </div>
      <div
        v-else-if="!chart?.series.length"
        class="px-map-cp-chart-area flex items-center justify-center text-sm text-px-muted"
      >
        暂无检查点数据
      </div>
      <template v-else>
        <div
          v-if="hasChartPlot"
          ref="rootRef"
          class="px-map-cp-chart-area"
          role="img"
          aria-label="主线 TOP10 检查点差异"
        />
        <div
          v-else
          class="px-map-cp-chart-area flex items-center justify-center text-sm text-px-muted"
        >
          暂无检查点分段数据
        </div>
        <div class="px-map-cp-legend" aria-label="TOP10 图例">
          <div
            v-for="(p, i) in chart.series"
            :key="p.auth"
            class="px-map-cp-legend-item"
            :class="{ 'px-map-cp-legend-item--no-cp': !hasCheckpointSeriesData(p) }"
            :style="{ '--legend-color': MAP_CP_RANK_COLORS[i] ?? MAP_CP_RANK_COLORS[9] }"
            :title="hasCheckpointSeriesData(p) ? undefined : '无检查点分段数据'"
          >
            <span class="px-map-cp-legend-swatch" aria-hidden="true" />
            <span class="font-pixel text-[10px]" :style="{ color: 'var(--legend-color)' }">
              {{ String(p.rank).padStart(2, '0') }}
            </span>
            <span class="text-xs font-medium">{{
              cpSeriesDisplayName(p, nameByAuth)
            }}</span>
          </div>
        </div>
      </template>
    </div>
  </section>
</template>
