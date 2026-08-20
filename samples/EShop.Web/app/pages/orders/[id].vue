<script setup lang="ts">
import type { GetOrderResponse } from '~/types/eshop'

const route = useRoute()
const orderId = route.params.id as string

const { data, refresh, status } = await useEshopData<GetOrderResponse>(`orders/${orderId}`)
const toast = useToast()

// `lineIds` and `order.lines` come from the same underlying collection in the same iteration order (see
// GetOrderQuery.cs), so zipping them by index recovers each line's ID for the reorder control below.
const lineRows = computed(() => {
  if (!data.value) return []
  return data.value.order.lines.map((line, i) => ({
    id: data.value!.lineIds[i]!,
    ...line
  }))
})

const total = computed(() =>
  lineRows.value.reduce((sum, l) => sum + l.unitPrice.amount * l.quantity, 0)
)

async function move(index: number, direction: -1 | 1) {
  const targetIndex = index + direction
  if (targetIndex < 0 || targetIndex >= lineRows.value.length) return

  const ids = lineRows.value.map(l => l.id)
  ;[ids[index], ids[targetIndex]] = [ids[targetIndex]!, ids[index]!]

  await $eshop(`orders/${orderId}/lines/reorder`, {
    method: 'PATCH',
    body: { lineIdsInOrder: ids }
  })
  toast.add({ title: 'Order lines reordered', color: 'success' })
  await refresh()
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex items-center gap-3">
      <UButton
        to="/orders"
        icon="i-lucide-arrow-left"
        color="neutral"
        variant="ghost"
        square
      />
      <h1 class="text-2xl font-bold">
        Order {{ orderId }}
      </h1>
    </div>

    <USkeleton
      v-if="status === 'pending'"
      class="h-48 w-full"
    />

    <template v-else-if="data">
      <UCard>
        <template #header>
          <span class="font-semibold">Customer</span>
        </template>
        {{ data.order.customerId }}
      </UCard>

      <UCard>
        <template #header>
          <span class="font-semibold">Payment method</span>
        </template>
        <template v-if="data.order.paymentMethod.$type === 'creditCardPayment'">
          Credit card ending in {{ data.order.paymentMethod.last4Digits }} ({{ data.order.paymentMethod.cardholderName }})
        </template>
        <template v-else>
          PayPal ({{ data.order.paymentMethod.payerEmail }})
        </template>
      </UCard>

      <UCard>
        <template #header>
          <span class="font-semibold">Lines</span>
        </template>
        <div class="flex flex-col divide-y divide-default">
          <div
            v-for="(line, i) in lineRows"
            :key="line.id"
            class="flex items-center justify-between py-2"
          >
            <div>
              <span class="font-medium">Product #{{ line.productId }}</span>
              <span class="text-muted"> x{{ line.quantity }} @ {{ line.unitPrice.amount }} {{ line.unitPrice.currency }}</span>
            </div>
            <div class="flex items-center gap-1">
              <UButton
                icon="i-lucide-arrow-up"
                color="neutral"
                variant="ghost"
                size="xs"
                @click="move(i, -1)"
              />
              <UButton
                icon="i-lucide-arrow-down"
                color="neutral"
                variant="ghost"
                size="xs"
                @click="move(i, 1)"
              />
            </div>
          </div>
        </div>
        <template #footer>
          <p class="text-right font-semibold">
            Total: {{ total.toFixed(2) }}
          </p>
        </template>
      </UCard>
    </template>

    <UAlert
      v-else
      icon="i-lucide-triangle-alert"
      color="error"
      title="Order not found"
    />
  </div>
</template>
