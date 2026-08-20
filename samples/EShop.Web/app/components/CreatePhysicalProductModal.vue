<script setup lang="ts">
import type { AdminCategoryItem, CreatePhysicalProductRequest } from '~/types/eshop'

const open = defineModel<boolean>('open', { default: false })
const emit = defineEmits<{ created: [] }>()

const { data: categories } = await useEshopData<AdminCategoryItem[]>('categories')
const categoryOptions = computed(() =>
  (categories.value ?? []).map(c => ({ label: `#${c.id} ${c.category.name}`, value: c.id }))
)

const submitting = ref(false)
const toast = useToast()

function emptyForm(): CreatePhysicalProductRequest {
  return {
    categoryId: categoryOptions.value[0]?.value ?? 0,
    sku: '',
    priceAmount: 0,
    priceCurrency: 'USD',
    tags: [],
    translations: [{ name: '', description: '', locale: 'en-US' }],
    weightKg: 0,
    initialStockQuantity: undefined
  }
}

const form = ref<CreatePhysicalProductRequest>(emptyForm())

watch(open, (isOpen) => {
  if (isOpen) form.value = emptyForm()
})

function addTranslation() {
  form.value.translations.push({ name: '', description: '', locale: '' })
}

function removeTranslation(index: number) {
  form.value.translations.splice(index, 1)
}

async function onSubmit() {
  submitting.value = true
  try {
    const result = await $eshop<{ id: number }>('products/physical', {
      method: 'POST',
      body: form.value
    })
    toast.add({ title: `Physical product #${result.id} created`, color: 'success' })
    open.value = false
    emit('created')
  } catch (error) {
    toast.add({ title: 'Failed to create physical product', description: String(error), color: 'error' })
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <UModal
    v-model:open="open"
    title="New physical product"
    description="[FlattenDtoFor<Money>] flattens the price into priceAmount/priceCurrency."
  >
    <template #body>
      <UForm
        :state="form"
        class="flex flex-col gap-4"
        @submit="onSubmit"
      >
        <UFormField
          label="Category"
          required
        >
          <USelect
            v-model="form.categoryId"
            :items="categoryOptions"
            class="w-full"
          />
        </UFormField>

        <div class="grid grid-cols-2 gap-4">
          <UFormField
            label="SKU"
            required
          >
            <UInput
              v-model="form.sku"
              class="w-full"
              placeholder="PHONE-001"
            />
          </UFormField>
          <UFormField label="Weight (kg)">
            <UInputNumber
              v-model="form.weightKg"
              class="w-full"
              :step="0.01"
            />
          </UFormField>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <UFormField label="Price amount">
            <UInputNumber
              v-model="form.priceAmount"
              class="w-full"
              :step="0.01"
            />
          </UFormField>
          <UFormField label="Price currency">
            <UInput
              v-model="form.priceCurrency"
              class="w-full"
              placeholder="USD"
            />
          </UFormField>
        </div>

        <UFormField label="Tags">
          <UInputTags
            v-model="form.tags"
            class="w-full"
          />
        </UFormField>

        <UFormField label="Initial stock quantity (optional -- omit to demonstrate the nullable supplied argument)">
          <UInputNumber
            v-model="form.initialStockQuantity"
            class="w-full"
          />
        </UFormField>

        <UFormField label="Translations">
          <div class="flex flex-col gap-2">
            <div
              v-for="(t, i) in form.translations"
              :key="i"
              class="flex gap-2"
            >
              <UInput
                v-model="t.locale"
                placeholder="en-US"
                class="w-24"
              />
              <UInput
                v-model="t.name"
                placeholder="Name"
                class="flex-1"
              />
              <UInput
                v-model="t.description"
                placeholder="Description"
                class="flex-1"
              />
              <UButton
                icon="i-lucide-x"
                color="neutral"
                variant="ghost"
                :disabled="form.translations.length <= 1"
                @click="removeTranslation(i)"
              />
            </div>
            <UButton
              icon="i-lucide-plus"
              label="Add translation"
              color="neutral"
              variant="subtle"
              size="xs"
              class="self-start"
              @click="addTranslation"
            />
          </div>
        </UFormField>

        <UButton
          type="submit"
          label="Create"
          :loading="submitting"
          block
        />
      </UForm>
    </template>
  </UModal>
</template>
