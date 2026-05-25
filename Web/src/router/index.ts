import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import MapsView from '../views/maps/MapsView.vue'
import MapDetailView from '../views/map-detail/MapDetailView.vue'
import PlayerView from '../views/players/PlayerView.vue'
import ServersView from '../views/servers/ServersView.vue'
const router = createRouter({
  history: createWebHistory(),
  scrollBehavior(to, _from, savedPosition) {
    if (savedPosition) return savedPosition
    if (to.hash) return { el: to.hash, behavior: 'smooth' }
    return { top: 0, behavior: 'smooth' }
  },
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/maps', name: 'maps', component: MapsView },
    { path: '/servers', name: 'servers', component: ServersView },
    { path: '/maps/:name', name: 'map-detail', component: MapDetailView, props: true },
    { path: '/players/:auth', name: 'player', component: PlayerView, props: true },
    { path: '/rankings', redirect: '/' },
    { path: '/records', redirect: '/' },
  ],
})

export default router
