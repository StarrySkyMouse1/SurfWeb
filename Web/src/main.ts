import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import router from './router'
import { documentTitle } from './config/site'

document.title = documentTitle

createApp(App).use(router).mount('#app')
