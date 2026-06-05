<script setup lang="ts">
import { RouterLink } from 'vue-router'
import { siteTitle } from '../config/site'

const links = [
  { to: '/', label: '首页', code: 'HOME' },
  { to: '/maps', label: '地图', code: 'MAPS' },
  { to: '/servers', label: '服务器', code: 'SRV' },
]
</script>

<template>
  <header class="sticky top-0 z-50 bg-px-paper/95 pt-4 backdrop-blur-sm">
    <div class="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-4">
      <div class="px-panel flex w-full flex-wrap items-center justify-between gap-4 px-4 py-4">
        <RouterLink to="/" class="flex items-center gap-3 hover:opacity-90">
          <div
            class="h-10 w-10 shrink-0 overflow-hidden border-2 border-px-ink bg-px-surface"
            style="box-shadow: 2px 2px 0 var(--color-px-ink)"
          >
            <img
              src="/brand-icon.png"
              :alt="siteTitle"
              width="40"
              height="40"
              class="h-full w-full object-cover"
            />
          </div>
          <div>
            <p class="font-bold leading-none">{{ siteTitle }}</p>
            <p class="px-en-subtitle mt-1">SURF RECORD</p>
          </div>
        </RouterLink>
        <nav class="flex flex-wrap gap-2">
          <RouterLink
            v-for="link in links"
            :key="link.to"
            :to="link.to"
            class="px-btn flex min-w-[4.5rem] flex-col items-center px-3 py-1.5 leading-tight"
            active-class="px-btn-active"
          >
            <span class="font-pixel text-[10px]">{{ link.code }}</span>
            <span class="text-xs">{{ link.label }}</span>
          </RouterLink>
        </nav>
      </div>
    </div>
  </header>
</template>
