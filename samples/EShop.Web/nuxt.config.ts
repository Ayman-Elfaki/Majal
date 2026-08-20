// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  modules: [
    '@nuxt/eslint',
    '@nuxt/ui',
    'nuxt-api-party'
  ],

  devtools: {
    enabled: true
  },

  css: ['~/assets/css/main.css'],

  compatibilityDate: '2026-06-30',

  // nuxt-api-party proxies every request through a Nuxt server route, so the EShop API's base
  // URL never reaches the browser and CORS never applies. See samples/EShop for the API itself.
  apiParty: {
    endpoints: {
      eshop: {
        url: process.env.ESHOP_API_BASE_URL || 'http://localhost:9100'
      }
    }
  },

  eslint: {
    config: {
      stylistic: {
        commaDangle: 'never',
        braceStyle: '1tbs'
      }
    }
  }
})
