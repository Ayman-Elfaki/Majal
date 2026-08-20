<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { AdminProductTranslation } from '~/types/eshop'

const { data: translations, status } = await useEshopData<AdminProductTranslation[]>('admin/products/translations')

const columns: TableColumn<AdminProductTranslation>[] = [
  { accessorKey: 'locale', header: 'Locale' },
  { accessorKey: 'name', header: 'Name' },
  { accessorKey: 'description', header: 'Description' }
]
</script>

<template>
  <div class="flex flex-col gap-6">
    <div>
      <h1 class="text-2xl font-bold">
        Product translations
      </h1>
      <p class="text-muted">
        Every product translation across every locale, via <code>IgnoreTranslatableFilter()</code>.
      </p>
    </div>

    <UTable
      :data="translations ?? []"
      :columns="columns"
      :loading="status === 'pending'"
    />
  </div>
</template>
