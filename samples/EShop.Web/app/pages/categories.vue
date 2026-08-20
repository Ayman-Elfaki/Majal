<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { AdminCategoryItem, CreateCategoryRequest } from '~/types/eshop'

const { data: categories, refresh, status } = await useEshopData<AdminCategoryItem[]>('categories')

const columns: TableColumn<AdminCategoryItem>[] = [
  { accessorKey: 'id', header: 'ID' },
  { id: 'name', accessorFn: row => row.category.name, header: 'Name' },
  { id: 'translations', accessorFn: row => row.category.translations, header: 'Translations' }
]

const modalOpen = ref(false)
const submitting = ref(false)
const toast = useToast()

function emptyForm(): CreateCategoryRequest {
  return {
    name: '',
    translations: [{ description: '', locale: 'en-US' }]
  }
}

const form = ref<CreateCategoryRequest>(emptyForm())

function addTranslation() {
  form.value.translations.push({ description: '', locale: '' })
}

function removeTranslation(index: number) {
  form.value.translations.splice(index, 1)
}

async function onSubmit() {
  submitting.value = true
  try {
    const result = await $eshop<{ id: number }>('categories', {
      method: 'POST',
      body: form.value
    })
    toast.add({ title: `Category #${result.id} created`, color: 'success' })
    modalOpen.value = false
    form.value = emptyForm()
    await refresh()
  } catch (error) {
    toast.add({ title: 'Failed to create category', description: String(error), color: 'error' })
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold">
          Categories
        </h1>
        <p class="text-muted">
          <code>[DtoFor&lt;Category&gt;]</code> -- an aggregate root, referenced by ID from products.
        </p>
      </div>
      <UButton
        icon="i-lucide-plus"
        label="New category"
        @click="modalOpen = true"
      />
    </div>

    <UTable
      :data="categories ?? []"
      :columns="columns"
      :loading="status === 'pending'"
    >
      <template #translations-cell="{ row }">
        <div class="flex flex-wrap gap-1">
          <UBadge
            v-for="t in row.original.category.translations"
            :key="t.locale"
            variant="subtle"
            color="neutral"
          >
            {{ t.locale }}: {{ t.description }}
          </UBadge>
        </div>
      </template>
    </UTable>

    <UModal
      v-model:open="modalOpen"
      title="New category"
    >
      <template #body>
        <UForm
          :state="form"
          class="flex flex-col gap-4"
          @submit="onSubmit"
        >
          <UFormField
            label="Name"
            name="name"
            required
          >
            <UInput
              v-model="form.name"
              class="w-full"
              placeholder="Electronics"
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
                  class="w-28"
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
  </div>
</template>
