<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { siteTitle } from '../config/site'

const router = useRouter()
const route = useRoute()

const links = [
  { to: '/', label: '首页', code: 'HOME' },
  { to: '/maps', label: '地图', code: 'MAPS' },
  { to: '/servers', label: '服务器', code: 'SRV' },
]

/** 非首页、且手机宽度下显示返回 */
const showBack = computed(() => route.path !== '/')

function goBack() {
  if (typeof window !== 'undefined' && window.history.length > 1) {
    router.back()
    return
  }
  router.push('/')
}
</script>

<template>
  <header class="sticky top-0 z-50 bg-px-paper/95 pt-4 backdrop-blur-sm">
    <div class="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-4">
      <div class="px-panel flex w-full flex-wrap items-center gap-3 px-4 py-4 sm:gap-4">
        <RouterLink to="/" class="flex min-w-0 flex-1 items-center gap-3 hover:opacity-90 sm:flex-none">
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
          <div class="min-w-0">
            <p class="truncate font-bold leading-none">{{ siteTitle }}</p>
            <p class="px-en-subtitle mt-1">SURF RECORD</p>
          </div>
        </RouterLink>

        <button
          v-if="showBack"
          type="button"
          class="px-header-back px-btn flex h-8 w-8 shrink-0 items-center justify-center sm:hidden"
          aria-label="返回上一页"
          title="返回"
          @click="goBack"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            stroke-linecap="square"
            stroke-linejoin="miter"
            class="h-4 w-4"
            aria-hidden="true"
          >
            <path d="M15 6 9 12l6 6" />
          </svg>
        </button>

        <nav class="flex w-full flex-wrap gap-2 sm:ml-auto sm:w-auto">
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
