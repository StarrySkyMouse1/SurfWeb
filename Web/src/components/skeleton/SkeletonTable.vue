<script setup lang="ts">
import { computed } from 'vue'
import {
  TABLE_CELL_CONTENT_CLASS,
  TABLE_CELL_SECONDARY_LINE_CLASS,
  TABLE_DATA_CELL_CLASS,
  TABLE_HEAD_CELL_CLASS,
  TABLE_SKELETON_BODY_ROW_CLASS,
} from '../../constants/tableCell'
import {
  HOME_LIST_HEAD_CLASS,
  HOME_LIST_HEAD_PIXEL_CLASS,
  HOME_LIST_ROW_EMPTY_CLASS,
  HOME_RANK_CELL_CLASS,
} from '../../constants/homeTable'
import { LEADERBOARD_ROW_EMPTY_CLASS } from '../../constants/tableCell'
import { PX_PANEL_CLASS, PX_RANK_CELL_CLASS } from '../../constants/pixelTheme'
import SkeletonBar from './SkeletonBar.vue'

type Variant = 'ranking' | 'leaderboard' | 'completions' | 'recent'

type Line = { bar: string } | { spacer: true }

type Column = {
  label: string
  align?: string
  headClass?: string
  cellClass?: string
  colClass?: string
  lines: Line[]
}

const props = withDefaults(
  defineProps<{
    variant: Variant
    rows?: number
    /** 由外层容器提供边框时设为 true */
    borderless?: boolean
  }>(),
  {
    rows: 10,
    borderless: false,
  },
)

const columns: Record<Variant, Column[]> = {
  ranking: [
    {
      label: '#',
      headClass: `${HOME_LIST_HEAD_PIXEL_CLASS} ${HOME_RANK_CELL_CLASS}`,
      cellClass: HOME_RANK_CELL_CLASS,
      colClass: 'w-12',
      lines: [{ bar: 'mx-auto h-4 w-6 shrink-0' }],
    },
    {
      label: '玩家',
      headClass: `${HOME_LIST_HEAD_CLASS} text-left`,
      lines: [{ bar: 'h-4 w-28 max-w-full shrink-0' }],
    },
    {
      label: '积分',
      align: 'text-right',
      headClass: `${HOME_LIST_HEAD_CLASS} text-right`,
      colClass: 'w-28',
      lines: [{ bar: 'h-4 w-14 ml-auto shrink-0' }],
    },
  ],
  recent: [
    {
      label: '玩家',
      headClass: `${HOME_LIST_HEAD_CLASS} text-left`,
      colClass: 'w-[32%]',
      lines: [{ bar: 'h-4 w-24 max-w-full shrink-0' }],
    },
    {
      label: '地图',
      headClass: `${HOME_LIST_HEAD_CLASS} text-left`,
      lines: [
        { bar: 'h-4 w-32 max-w-full shrink-0' },
        { bar: 'mt-0.5 h-3 w-12 shrink-0' },
      ],
    },
    {
      label: '时间',
      align: 'text-right',
      headClass: `${HOME_LIST_HEAD_CLASS} text-right`,
      colClass: 'w-40',
      lines: [
        { bar: 'h-4 w-[5.5rem] ml-auto shrink-0' },
        { bar: 'mt-0.5 h-3 w-24 ml-auto shrink-0' },
      ],
    },
  ],
  leaderboard: [
    {
      label: '#',
      headClass: `${PX_RANK_CELL_CLASS} py-2`,
      cellClass: PX_RANK_CELL_CLASS,
      colClass: 'w-12',
      lines: [{ bar: 'mx-auto h-4 w-6 shrink-0' }],
    },
    {
      label: '玩家',
      headClass: `${TABLE_HEAD_CELL_CLASS} text-left`,
      lines: [{ bar: 'h-4 w-28 max-w-full shrink-0' }],
    },
    {
      label: '时间',
      align: 'text-right',
      headClass: `${TABLE_HEAD_CELL_CLASS} text-right`,
      colClass: 'w-36',
      lines: [{ bar: 'h-4 w-[5.5rem] ml-auto shrink-0' }],
    },
    {
      label: '同步',
      align: 'text-right',
      headClass: `${TABLE_HEAD_CELL_CLASS} text-right`,
      colClass: 'w-24',
      lines: [{ bar: 'h-4 w-10 ml-auto shrink-0' }],
    },
    {
      label: '日期',
      align: 'text-right',
      headClass: `${TABLE_HEAD_CELL_CLASS} text-right`,
      colClass: 'w-40',
      lines: [{ bar: 'h-4 w-24 ml-auto shrink-0' }],
    },
  ],
  completions: [
    { label: '地图', lines: [{ bar: 'h-4 w-36 max-w-full shrink-0' }] },
    { label: 'Tier', colClass: 'w-16', lines: [{ bar: 'h-4 w-8 shrink-0' }] },
    {
      label: '时间',
      align: 'text-right',
      colClass: 'w-28',
      lines: [{ bar: 'h-4 w-16 ml-auto shrink-0' }],
    },
    {
      label: '同步',
      align: 'text-right',
      colClass: 'w-24',
      lines: [{ bar: 'h-4 w-10 ml-auto shrink-0' }],
    },
    {
      label: '日期',
      align: 'text-right',
      colClass: 'w-36',
      lines: [{ bar: 'h-4 w-24 ml-auto shrink-0' }],
    },
  ],
}

const minWidth: Record<Variant, string> = {
  ranking: '',
  leaderboard: 'min-w-[560px]',
  completions: 'min-w-[520px]',
  recent: '',
}

const tableClass = computed(() =>
  `w-full ${minWidth[props.variant]} table-fixed text-left text-sm`.trim(),
)

const tableWrapClass = computed(() => {
  if (!props.borderless) return [PX_PANEL_CLASS, 'overflow-x-auto'] as const
  if (props.variant === 'ranking' || props.variant === 'recent') {
    return 'overflow-x-hidden bg-px-surface' as const
  }
  return 'overflow-x-auto bg-px-surface' as const
})

function isBar(line: Line): line is { bar: string } {
  return 'bar' in line
}

function skeletonRowClass(variant: Variant): string {
  if (variant === 'ranking' || variant === 'recent') return HOME_LIST_ROW_EMPTY_CLASS
  if (variant === 'leaderboard' || variant === 'completions') return LEADERBOARD_ROW_EMPTY_CLASS
  return TABLE_SKELETON_BODY_ROW_CLASS
}
</script>

<template>
  <div :class="tableWrapClass">
    <table :class="tableClass" aria-busy="true">
      <colgroup>
        <col
          v-for="col in columns[variant]"
          :key="col.label"
          :class="col.colClass"
        />
      </colgroup>
      <thead class="px-table-head">
        <tr>
          <th
            v-for="col in columns[variant]"
            :key="col.label"
            :class="col.headClass ?? [TABLE_HEAD_CELL_CLASS, col.align]"
          >
            {{ col.label }}
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="n in props.rows"
          :key="n"
          :class="skeletonRowClass(variant)"
        >
          <td
            v-for="col in columns[variant]"
            :key="col.label"
            :class="[TABLE_DATA_CELL_CLASS, col.cellClass, col.align]"
          >
            <div :class="TABLE_CELL_CONTENT_CLASS">
              <template v-for="(line, idx) in col.lines" :key="idx">
                <SkeletonBar v-if="isBar(line)" :class="line.bar" />
                <div v-else :class="TABLE_CELL_SECONDARY_LINE_CLASS" aria-hidden="true" />
              </template>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
