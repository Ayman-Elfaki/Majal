<script setup lang="ts">
import type { AdminCategoryItem, CreateDigitalProductRequest } from '~/types/eshop'

const open = defineModel<boolean>('open', { default: false })
const emit = defineEmits<{ created: [] }>()

const { data: categories } = await useEshopData<AdminCategoryItem[]>('categories')
const categoryOptions = computed(() =>
  (categories.value ?? []).map(c => ({ label: `#${c.id} ${c.category.name}`, value: c.id }))
)

const submitting = ref(false)
const toast = useToast()

function emptyForm(): CreateDigitalProductRequest {
  return {
    categoryId: categoryOptions.value[0]?.value ?? 0,
    sku: '',
    price: { amount: 0, currency: 'USD' },
    tags: [],
    translations: [{ name: '', description: '', locale: 'en-US' }],
    downloadUrl: '',
    initialStockQuantity: 0
  }
}

const form = ref<CreateDigitalProductRequest>(emptyForm())

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
    const result = await $eshop<{ id: number }>('products/digital', {
      method: 'POST',
      body: form.value
    })
    toast.add({ title: `Digital product #${result.id} created`, color: 'success' })
    open.value = false
    emit('created')
  } catch (error) {
    toast.add({ title: 'Failed to create digital product', description: String(error), color: 'error' })
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <UModal
    v-model:open="open"
    title="New digital product"
    description="Price is left unflattened here, so it's a nested { amount, currency } object."
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
              placeholder="EBOOK-001"
            />
          </UFormField>
          <UFormField
            label="Download URL"
            required
          >
            <UInput
              v-model="form.downloadUrl"
              class="w-full"
              placeholder="https://example.com/download"
            />
          </UFormField>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <UFormField label="Price amount">
            <UInputNumber
              v-model="form.price.amount"
              class="w-full"
              :step="0.01"
            />
          </UFormField>
          <UFormField label="Price currency">
            <UInput
              v-model="form.price.currency"
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

        <UFormField label="Initial stock quantity">
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
