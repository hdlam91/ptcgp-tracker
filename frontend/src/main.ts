import { registerSW } from 'virtual:pwa-register'
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import './style.css'

createApp(App).use(router).mount('#app')

// Installs the service worker in production builds. A new release is picked up automatically and
// the page reloads once; in `vite dev` this does nothing.
registerSW({ immediate: true })
