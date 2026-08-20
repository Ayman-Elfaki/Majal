# EShop.Web

A [Nuxt UI](https://ui.nuxt.com) front end for the [EShop](../EShop) sample API, calling every endpoint
through [nuxt-api-party](https://nuxt-api-party.byjohann.dev/)'s server-side proxy (so EShop's base URL
never reaches the browser and CORS never applies).

Response types aren't generated from EShop's OpenAPI document -- its Wolverine.Http endpoints return
`Task<IResult>`, so the document only describes request bodies, not responses. Instead, `app/types/eshop.ts`
hand-mirrors EShop's actual JSON shapes.

## Pages

| Page | Demonstrates |
|---|---|
| `/categories` | `[DtoFor<Category>]` on an aggregate root |
| `/products` | Physical (flattened price) vs. digital (nested price) product creation, `[Ordinal]` reordering, soft-delete |
| `/products/archived` | `IgnoreArchivableFilter()`, whole-type `[ExcludeDtoFor<T>]` |
| `/translations` | `IgnoreTranslatableFilter()` |
| `/customers` | `GET /customers` reusing the create command's own `CustomerDto` (no supplied arguments needed) |
| `/orders` | Polymorphic `PaymentMethod` DTO, reversed `[FlattenDtoFor<Money>]`, real domain-event dispatch, `GET /orders` reusing `GetOrderQuery.OrderDto` |

## Setup

Requires the [EShop](../EShop) API running first (`dotnet run --project ../EShop`, default
`http://localhost:9100`). Point this app at a different URL by setting `ESHOP_API_BASE_URL` (see
`.env.example`).

```bash
bun install
bun run dev
```

Opens on `http://localhost:3000`.
