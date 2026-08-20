<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { isDigitalProduct, type ProductListItem } from '~/types/eshop'

const { data: products, refresh, status } = await useEshopData<ProductListItem[]>('products')

const sorted = computed(() =>
  [...(products.value ?? [])].sort((a, b) => (a.ordinal ?? 0) - (b.ordinal ?? 0))
)

const columns: TableColumn<ProductListItem>[] = [
  { accessorKey: 'id', header: 'ID' },
  { id: 'type', header: 'Type' },
  { id: 'sku', accessorFn: row => row.product.sku, header: 'SKU' },
  { id: 'price', header: 'Price' },
  { id: 'tags', accessorFn: row => row.product.tags, header: 'Tags' },
  { id: 'stock', accessorFn: row => row.product.initialStockQuantity, header: 'Stock' },
  { accessorKey: 'ordinal', header: 'Ordinal' },
  { id: 'actions', header: '' }
]

const physicalModalOpen = ref(false)
const digitalModalOpen = ref(false)
const toast = useToast()

async function discontinue(id: number) {
  await $eshop(`products/${id}`, { method: 'DELETE' })
  toast.add({ title: `Product #${id} discontinued`, color: 'success' })
  await refresh()
}

async function move(item: ProductListItem, direction: -1 | 1) {
  const ids = sorted.value.map(p => p.id)
  const index = ids.indexOf(item.id)
  const targetIndex = index + direction
  if (targetIndex < 0 || targetIndex >= ids.length) return

  const reordered = [...ids]
  ;[reordered[index], reordered[targetIndex]] = [reordered[targetIndex]!, reordered[index]!]

  await $eshop('products/reorder', {
    method: 'PATCH',
    body: { productIdsInOrder: reordered }
  })
  await refresh()
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold">
          Products
        </h1>
        <p class="text-muted">
          Active, in-stock-ordered products -- <code>Product</code> is abstract, so this reuses the
          create commands' own generated DTOs.
        </p>
      </div>
      <div class="flex gap-2">
        <UButton
          icon="i-lucide-plus"
          label="Physical product"
          color="neutral"
          variant="subtle"
          @click="physicalModalOpen = true"
        />
        <UButton
          icon="i-lucide-plus"
          label="Digital product"
          @click="digitalModalOpen = true"
        />
      </div>
    </div>

    <UTable
      :data="sorted"
      :columns="columns"
      :loading="status === 'pending'"
    >
      <template #type-cell="{ row }">
        <UBadge :color="isDigitalProduct(row.original.product) ? 'primary' : 'neutral'">
          {{ isDigitalProduct(row.original.product) ? 'Digital' : 'Physical' }}
        </UBadge>
      </template>

      <template #price-cell="{ row }">
        <span v-if="isDigitalProduct(row.original.product)">
          {{ row.original.product.price.amount }} {{ row.original.product.price.currency }}
        </span>
        <span v-else>
          {{ row.original.product.priceAmount }} {{ row.original.product.priceCurrency }}
        </span>
      </template>

      <template #tags-cell="{ row }">
        <div class="flex flex-wrap gap-1">
          <UBadge
            v-for="tag in row.original.product.tags"
            :key="tag"
            variant="subtle"
            color="neutral"
            size="sm"
          >
            {{ tag }}
          </UBadge>
        </div>
      </template>

      <template #actions-cell="{ row }">
        <div class="flex items-center gap-1 justify-end">
          <UButton
            icon="i-lucide-arrow-up"
            color="neutral"
            variant="ghost"
            size="xs"
            @click="move(row.original, -1)"
          />
          <UButton
            icon="i-lucide-arrow-down"
            color="neutral"
            variant="ghost"
            size="xs"
            @click="move(row.original, 1)"
          />
          <UButton
            icon="i-lucide-archive"
            color="error"
            variant="ghost"
            size="xs"
            @click="discontinue(row.original.id)"
          />
        </div>
      </template>
    </UTable>

    <CreatePhysicalProductModal
      v-model:open="physicalModalOpen"
      @created="refresh"
    />
    <CreateDigitalProductModal
      v-model:open="digitalModalOpen"
      @created="refresh"
    />
  </div>
</template>
