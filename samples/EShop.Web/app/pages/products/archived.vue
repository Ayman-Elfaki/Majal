<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { isDigitalProduct, type ArchivedProductItem } from '~/types/eshop'

const { data: products, status } = await useEshopData<ArchivedProductItem[]>('admin/products/archived')

const columns: TableColumn<ArchivedProductItem>[] = [
  { accessorKey: 'id', header: 'ID' },
  { id: 'type', header: 'Type' },
  { accessorKey: 'sku', header: 'SKU' },
  { id: 'price', header: 'Price' },
  { id: 'tags', header: 'Tags' }
]
</script>

<template>
  <div class="flex flex-col gap-6">
    <div>
      <h1 class="text-2xl font-bold">
        Archived products
      </h1>
      <p class="text-muted">
        Uses <code>IgnoreArchivableFilter()</code> to bypass the default query filter, and a whole-type
        <code>[DtoIgnoreType&lt;ProductTranslation&gt;]</code> so this listing has no translations.
      </p>
    </div>

    <UAlert
      v-if="!products?.length"
      icon="i-lucide-info"
      color="neutral"
      variant="subtle"
      title="Nothing archived yet"
      description="Discontinue a product from the Products page to see it here."
    />

    <UTable
      v-else
      :data="products"
      :columns="columns"
      :loading="status === 'pending'"
    >
      <template #type-cell="{ row }">
        <UBadge :color="isDigitalProduct(row.original) ? 'primary' : 'neutral'">
          {{ isDigitalProduct(row.original) ? 'Digital' : 'Physical' }}
        </UBadge>
      </template>

      <template #price-cell="{ row }">
        <span v-if="isDigitalProduct(row.original)">
          {{ row.original.price.amount }} {{ row.original.price.currency }}
        </span>
        <span v-else>
          {{ row.original.priceAmount }} {{ row.original.priceCurrency }}
        </span>
      </template>

      <template #tags-cell="{ row }">
        <div class="flex flex-wrap gap-1">
          <UBadge
            v-for="tag in row.original.tags"
            :key="tag"
            variant="subtle"
            color="neutral"
            size="sm"
          >
            {{ tag }}
          </UBadge>
        </div>
      </template>
    </UTable>
  </div>
</template>
