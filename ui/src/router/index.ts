import { createRouter, createWebHistory } from 'vue-router'
import type { RoleCode } from '@/types/ems'
import LoginView from '@/views/LoginView.vue'
import EmployeeListView from '@/views/EmployeeListView.vue'
import EmployeeDetailView from '@/views/EmployeeDetailView.vue'
import EmployeeFormView from '@/views/EmployeeFormView.vue'
import EmployeeAddressesView from '@/views/EmployeeAddressesView.vue'
import MyAddressesView from '@/views/MyAddressesView.vue'
import RolesView from '@/views/RolesView.vue'
import AuditLogsView from '@/views/AuditLogsView.vue'
import ForbiddenView from '@/views/ForbiddenView.vue'
import NotFoundView from '@/views/NotFoundView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/employees',
    },
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { publicOnly: true },
    },
    {
      path: '/employees',
      name: 'employees',
      component: EmployeeListView,
      meta: { requiresAuth: true, roles: ['Admin', 'Basic'] },
    },
    {
      path: '/employees/new',
      name: 'employee-new',
      component: EmployeeFormView,
      meta: { requiresAuth: true, roles: ['Admin'] },
    },
    {
      path: '/employees/:id',
      name: 'employee-detail',
      component: EmployeeDetailView,
      meta: { requiresAuth: true, roles: ['Admin', 'Basic'] },
    },
    {
      path: '/employees/:id/edit',
      name: 'employee-edit',
      component: EmployeeFormView,
      meta: { requiresAuth: true, roles: ['Admin'] },
    },
    {
      path: '/employees/:id/addresses',
      name: 'employee-addresses',
      component: EmployeeAddressesView,
      meta: { requiresAuth: true, roles: ['Admin'] },
    },
    {
      path: '/me/addresses',
      name: 'my-addresses',
      component: MyAddressesView,
      meta: { requiresAuth: true, roles: ['Basic'] },
    },
    {
      path: '/roles',
      name: 'roles',
      component: RolesView,
      meta: { requiresAuth: true, roles: ['Admin'] },
    },
    {
      path: '/audit-logs',
      name: 'audit-logs',
      component: AuditLogsView,
      meta: { requiresAuth: true, roles: ['Admin'] },
    },
    {
      path: '/403',
      name: 'forbidden',
      component: ForbiddenView,
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: NotFoundView,
    },
  ],
})

export default router

declare module 'vue-router' {
  interface RouteMeta {
    publicOnly?: boolean
    requiresAuth?: boolean
    roles?: RoleCode[]
  }
}
