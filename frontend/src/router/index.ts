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
      path: '/cards',
      name: 'all-cards',
      component: () => import('@/views/AllCardsView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/trade-list',
      name: 'trade-list',
      component: () => import('@/views/TradeListView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/share/:handle',
      name: 'shared-trade-list',
      component: () => import('@/views/SharedTradeListView.vue'),
      // Public: works whether or not the visitor is logged in, and never redirects.
      meta: { public: true },
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/views/SettingsView.vue'),
      meta: { requiresAuth: true, requiresAdmin: true },
    },
    {
      path: '/legal',
      name: 'legal',
      component: () => import('@/views/LegalView.vue'),
      meta: { public: true },
    },
  ],
})

router.beforeEach(async (to) => {
  if (to.meta.public) {
    return true
  }

  const { initialized, isAuthenticated, isAdmin, fetchCurrentUser } = useAuth()

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

  // The backend enforces this too; this just keeps non-admins off a page that would only show errors.
  if (to.meta.requiresAdmin && !isAdmin.value) {
    return { name: 'collection' }
  }

  return true
})

export default router
