<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import {
  isDigitalProduct,
  type CustomerListItem,
  type OrderLineRequest,
  type OrderListItem,
  type PlaceOrderResponse,
  type ProductListItem
} from '~/types/eshop'

const { data: customers } = await useEshopData<CustomerListItem[]>('customers')
const { data: products } = await useEshopData<ProductListItem[]>('products')
const { data: orders, refresh: refreshOrders, status } = await useEshopData<OrderListItem[]>('orders')
const toast = useToast()
const submitting = ref(false)

const customerOptions = computed(() =>
  (customers.value ?? []).map(c => ({ label: `${c.customer.name} (${c.id})`, value: c.id }))
)
const productOptions = computed(() =>
  (products.value ?? []).map(p => ({
    label: `#${p.id} ${p.product.sku}`,
    value: p.id,
    amount: isDigitalProduct(p.product) ? p.product.price.amount : p.product.priceAmount,
    currency: isDigitalProduct(p.product) ? p.product.price.currency : p.product.priceCurrency
  }))
)

function customerName(customerId: string) {
  return customers.value?.find(c => c.id === customerId)?.customer.name ?? customerId
}

function orderTotal(order: OrderListItem) {
  const amount = order.order.lines.reduce((sum, l) => sum + l.unitPrice.amount * l.quantity, 0)
  const currency = order.order.lines[0]?.unitPrice.currency ?? ''
  return `${amount.toFixed(2)} ${currency}`.trim()
}

const customerId = ref('')
const paymentType = ref<'creditCardPayment' | 'payPalPayment'>('creditCardPayment')
const cardholderName = ref('')
const last4Digits = ref('')
const payerEmail = ref('')

const lines = ref<OrderLineRequest[]>([])

function addLine() {
  const first = productOptions.value[0]
  lines.value.push({
    productId: first?.value ?? 0,
    quantity: 1,
    amountUnitPrice: first?.amount ?? 0,
    currencyUnitPrice: first?.currency ?? 'USD'
  })
}

function removeLine(index: number) {
  lines.value.splice(index, 1)
}

function onProductChange(line: OrderLineRequest) {
  const product = productOptions.value.find(p => p.value === line.productId)
  if (product) {
    line.amountUnitPrice = product.amount
    line.currencyUnitPrice = product.currency
  }
}

const total = computed(() =>
  lines.value.reduce((sum, l) => sum + l.amountUnitPrice * l.quantity, 0)
)

async function placeOrder() {
  if (!customerId.value || lines.value.length === 0) {
    toast.add({ title: 'Pick a customer and at least one product line', color: 'warning' })
    return
  }

  submitting.value = true
  try {
    const paymentMethod = paymentType.value === 'creditCardPayment'
      ? { $type: 'creditCardPayment' as const, cardholderName: cardholderName.value, last4Digits: last4Digits.value }
      : { $type: 'payPalPayment' as const, payerEmail: payerEmail.value }

    const result = await $eshop<PlaceOrderResponse>('orders', {
      method: 'POST',
      body: { customerId: customerId.value, lines: lines.value, paymentMethod }
    })

    toast.add({ title: `Order ${result.id} placed`, color: 'success' })
    lines.value = []
    await refreshOrders()
    await navigateTo(`/orders/${result.id}`)
  } catch (error) {
    toast.add({ title: 'Failed to place order', description: String(error), color: 'error' })
  } finally {
    submitting.value = false
  }
}

const orderColumns: TableColumn<OrderListItem>[] = [
  { accessorKey: 'id', header: 'Order ID' },
  { id: 'customer', header: 'Customer' },
  { id: 'total', header: 'Total' },
  { accessorKey: 'createdOn', header: 'Placed at' }
]
</script>

<template>
  <div class="flex flex-col gap-6">
    <div>
      <h1 class="text-2xl font-bold">
        Place an order
      </h1>
      <p class="text-muted">
        <code>PaymentMethod</code> is polymorphic (credit card or PayPal), and the unit price uses a
        reversed flatten into each order line.
      </p>
    </div>

    <UCard>
      <div class="flex flex-col gap-4">
        <UFormField
          label="Customer"
          description="Register one on the Customers page first."
        >
          <USelect
            v-model="customerId"
            :items="customerOptions"
            placeholder="Select a customer"
            class="w-full max-w-md"
          />
        </UFormField>

        <UFormField label="Order lines">
          <div class="flex flex-col gap-2">
            <div
              v-for="(line, i) in lines"
              :key="i"
              class="flex flex-wrap items-end gap-2"
            >
              <USelect
                v-model="line.productId"
                :items="productOptions"
                class="w-56"
                @update:model-value="onProductChange(line)"
              />
              <UInputNumber
                v-model="line.quantity"
                :min="1"
                class="w-24"
              />
              <UInputNumber
                v-model="line.amountUnitPrice"
                :step="0.01"
                class="w-28"
              />
              <UInput
                v-model="line.currencyUnitPrice"
                class="w-20"
              />
              <UButton
                icon="i-lucide-x"
                color="neutral"
                variant="ghost"
                @click="removeLine(i)"
              />
            </div>
            <UButton
              icon="i-lucide-plus"
              label="Add line"
              color="neutral"
              variant="subtle"
              size="xs"
              class="self-start"
              :disabled="!productOptions.length"
              @click="addLine"
            />
          </div>
        </UFormField>

        <UFormField label="Payment method">
          <URadioGroup
            v-model="paymentType"
            orientation="horizontal"
            :items="[
              { label: 'Credit card', value: 'creditCardPayment' },
              { label: 'PayPal', value: 'payPalPayment' }
            ]"
          />
        </UFormField>

        <div
          v-if="paymentType === 'creditCardPayment'"
          class="grid grid-cols-2 gap-4"
        >
          <UFormField label="Cardholder name">
            <UInput
              v-model="cardholderName"
              class="w-full"
            />
          </UFormField>
          <UFormField label="Last 4 digits">
            <UInput
              v-model="last4Digits"
              class="w-full"
              maxlength="4"
            />
          </UFormField>
        </div>
        <UFormField
          v-else
          label="PayPal email"
        >
          <UInput
            v-model="payerEmail"
            type="email"
            class="w-full max-w-sm"
          />
        </UFormField>

        <p class="text-sm text-muted">
          Total: <strong>{{ total.toFixed(2) }}</strong>
        </p>

        <UButton
          label="Place order"
          :loading="submitting"
          :disabled="!lines.length"
          class="self-start"
          @click="placeOrder"
        />
      </div>
    </UCard>

    <div>
      <h2 class="text-lg font-semibold mb-2">
        Orders
      </h2>
      <UAlert
        v-if="!orders?.length"
        icon="i-lucide-info"
        color="neutral"
        variant="subtle"
        title="No orders placed yet"
      />
      <UTable
        v-else
        :data="orders"
        :columns="orderColumns"
        :loading="status === 'pending'"
      >
        <template #id-cell="{ row }">
          <NuxtLink
            :to="`/orders/${row.original.id}`"
            class="text-primary underline"
          >
            {{ row.original.id }}
          </NuxtLink>
        </template>
        <template #customer-cell="{ row }">
          {{ customerName(row.original.order.customerId) }}
        </template>
        <template #total-cell="{ row }">
          {{ orderTotal(row.original) }}
        </template>
        <template #createdOn-cell="{ row }">
          {{ new Date(row.original.createdOn).toLocaleString() }}
        </template>
      </UTable>
    </div>
  </div>
</template>
