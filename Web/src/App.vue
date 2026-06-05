<script setup lang="ts">
import AppHeader from './components/AppHeader.vue'
import { siteTitle } from './config/site'
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <AppHeader />
    <main class="mx-auto w-full max-w-7xl flex-1 px-4 py-8">
      <RouterView v-slot="{ Component, route }">
        <transition name="page-fade" mode="out-in">
          <component :is="Component" :key="route.path" />
        </transition>
      </RouterView>
    </main>
    <footer
      class="mt-auto border-t-2 border-px-ink py-6 text-center text-xs text-px-muted"
    >
      <p class="font-bold">{{ siteTitle }}</p>
      <p class="mt-0.5 font-pixel text-[9px] text-px-muted">SURF RECORD</p>
      <p class="mt-1 normal-case">网站作者 星空鼠鼠</p>
    </footer>
  </div>
</template>
