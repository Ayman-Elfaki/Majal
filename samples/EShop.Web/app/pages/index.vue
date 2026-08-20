<script setup lang="ts">
import type { AdminCategoryItem, ProductListItem } from '~/types/eshop'

const { data: categories } = await useEshopData<AdminCategoryItem[]>('categories')
const { data: products } = await useEshopData<ProductListItem[]>('products')

const sections = [
  {
    title: 'Categories',
    description: 'Create categories and list them with a Prefix-overridden nested translation DTO.',
    icon: 'i-lucide-tags',
    to: '/categories'
  },
  {
    title: 'Products',
    description: 'Create physical (flattened price) and digital (nested price) products in an existing category.',
    icon: 'i-lucide-package',
    to: '/products'
  },
  {
    title: 'Archived products',
    description: 'Discontinued products, bypassing the default Archivable query filter.',
    icon: 'i-lucide-archive',
    to: '/products/archived'
  },
  {
    title: 'Translations',
    description: 'Every product translation across every locale, bypassing the Translatable filter.',
    icon: 'i-lucide-languages',
    to: '/translations'
  },
  {
    title: 'Customers',
    description: 'Register customers.',
    icon: 'i-lucide-users',
    to: '/customers'
  },
  {
    title: 'Orders',
    description: 'Place orders with a polymorphic payment method and view them back.',
    icon: 'i-lucide-shopping-cart',
    to: '/orders'
  }
]
</script>

<template>
  <div class="flex flex-col gap-8">
    <UPageHero
      title="EShop"
      description="A Nuxt UI front end for the EShop sample API, calling every endpoint through nuxt-api-party's server-side proxy."
    />

    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <UPageCard
        title="Active products"
        :description="`${products?.length ?? 0} in stock listing`"
        icon="i-lucide-package"
        to="/products"
      />
      <UPageCard
        title="Categories"
        :description="`${categories?.length ?? 0} registered`"
        icon="i-lucide-tags"
        to="/categories"
      />
    </div>

    <UPageGrid>
      <UPageCard
        v-for="section in sections"
        :key="section.to"
        :title="section.title"
        :description="section.description"
        :icon="section.icon"
        :to="section.to"
        variant="subtle"
      />
    </UPageGrid>
  </div>
</template>
