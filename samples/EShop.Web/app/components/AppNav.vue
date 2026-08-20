<script setup lang="ts">
const items = [
  { label: 'Dashboard', to: '/', icon: 'i-lucide-layout-dashboard' },
  { label: 'Categories', to: '/categories', icon: 'i-lucide-tags' },
  { label: 'Products', to: '/products', icon: 'i-lucide-package' },
  { label: 'Archived', to: '/products/archived', icon: 'i-lucide-archive' },
  { label: 'Translations', to: '/translations', icon: 'i-lucide-languages' },
  { label: 'Customers', to: '/customers', icon: 'i-lucide-users' },
  { label: 'Orders', to: '/orders', icon: 'i-lucide-shopping-cart' }
]
</script>

<template>
  <UNavigationMenu :items="items" />
</template>
