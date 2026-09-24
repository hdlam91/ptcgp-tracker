import { createRouter, createWebHistory } from 'vue-router'
import { useAuth } from '@/composables/useAuth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { requiresAuth: false },
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/views/RegisterView.vue'),
      meta: { requiresAuth: false },
    },
    {
      path: '/',
      name: 'collection',
      component: () => import('@/views/CollectionView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/sets/:setCode',
      name: 'set-detail',
      component: () => import('@/views/SetDetailView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/trade-list',
      name: 'trade-list',
      component: () => import('@/views/TradeListView.vue'),
      meta: { requiresAuth: true },
    },
  ],
})

router.beforeEach(async (to) => {
  const { initialized, isAuthenticated, fetchCurrentUser } = useAuth()

  if (!initialized.value) {
    await fetchCurrentUser()
  }

  const requiresAuth = to.meta.requiresAuth !== false
  if (requiresAuth && !isAuthenticated.value) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (!requiresAuth && isAuthenticated.value) {
    return { name: 'collection' }
  }

  return true
})

export default router
