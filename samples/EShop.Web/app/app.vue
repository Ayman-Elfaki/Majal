<script setup lang="ts">
useHead({
  meta: [
    { name: 'viewport', content: 'width=device-width, initial-scale=1' }
  ],
  htmlAttrs: {
    lang: 'en'
  }
})

const title = 'EShop'
const description = 'A Nuxt UI sample app driving the EShop API through nuxt-api-party.'

useSeoMeta({
  title,
  description,
  ogTitle: title,
  ogDescription: description
})
</script>

<template>
  <UApp>
    <UHeader>
      <template #left>
        <NuxtLink to="/">
          <AppLogo />
        </NuxtLink>
      </template>

      <AppNav />

      <template #right>
        <UColorModeButton />

        <UButton
          to="https://github.com/Ayman-Elfaki/Majal"
          target="_blank"
          icon="i-simple-icons-github"
          aria-label="Majal on GitHub"
          color="neutral"
          variant="ghost"
        />
      </template>
    </UHeader>

    <UMain>
      <UContainer class="py-8">
        <NuxtPage />
      </UContainer>
    </UMain>

    <UFooter>
      <template #left>
        <p class="text-sm text-muted">
          EShop.Web -- a Nuxt UI + nuxt-api-party sample for the
          <a
            href="https://github.com/Ayman-Elfaki/Majal"
            class="underline"
          >Majal</a> source generators.
        </p>
      </template>
    </UFooter>
  </UApp>
</template>
