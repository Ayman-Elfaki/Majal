<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { CreateCustomerRequest, CustomerListItem, RegisterCustomerResponse } from '~/types/eshop'

const { data: customers, refresh, status } = await useEshopData<CustomerListItem[]>('customers')
const toast = useToast()
const submitting = ref(false)

const form = ref<CreateCustomerRequest>({ name: '', email: '' })

const columns: TableColumn<CustomerListItem>[] = [
  { accessorKey: 'id', header: 'ID' },
  { id: 'name', accessorFn: row => row.customer.name, header: 'Name' },
  { id: 'email', accessorFn: row => row.customer.email, header: 'Email' },
  { id: 'copy', header: '' }
]

async function onSubmit() {
  submitting.value = true
  try {
    const result = await $eshop<RegisterCustomerResponse>('customers', {
      method: 'POST',
      body: form.value
    })
    toast.add({ title: `Customer ${result.id} registered`, color: 'success' })
    form.value = { name: '', email: '' }
    await refresh()
  } catch (error) {
    toast.add({ title: 'Failed to register customer', description: String(error), color: 'error' })
  } finally {
    submitting.value = false
  }
}

async function copyId(id: string) {
  await navigator.clipboard.writeText(id)
  toast.add({ title: 'Customer ID copied', color: 'success' })
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <div>
      <h1 class="text-2xl font-bold">
        Customers
      </h1>
      <p class="text-muted">
        <code>[DtoFor&lt;Customer&gt;]</code> -- <code>Name</code> and <code>Email</code> are plain
        readable value objects, so <code>GET /customers</code> reuses the same DTO the create command
        generates.
      </p>
    </div>

    <UCard class="max-w-lg">
      <UForm
        :state="form"
        class="flex flex-col gap-4"
        @submit="onSubmit"
      >
        <UFormField
          label="Name"
          required
        >
          <UInput
            v-model="form.name"
            class="w-full"
            placeholder="Ada Lovelace"
          />
        </UFormField>
        <UFormField
          label="Email"
          required
        >
          <UInput
            v-model="form.email"
            type="email"
            class="w-full"
            placeholder="ada@example.com"
          />
        </UFormField>
        <UButton
          type="submit"
          label="Register"
          :loading="submitting"
          block
        />
      </UForm>
    </UCard>

    <UTable
      :data="customers ?? []"
      :columns="columns"
      :loading="status === 'pending'"
    >
      <template #copy-cell="{ row }">
        <UButton
          icon="i-lucide-copy"
          color="neutral"
          variant="ghost"
          size="xs"
          @click="copyId(row.original.id)"
        />
      </template>
    </UTable>
  </div>
</template>
