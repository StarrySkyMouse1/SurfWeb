import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import MapsView from '../views/MapsView.vue'
import MapDetailView from '../views/MapDetailView.vue'
import PlayerView from '../views/PlayerView.vue'
import ServersView from '../views/ServersView.vue'
const router = createRouter({
  history: createWebHistory(),
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
