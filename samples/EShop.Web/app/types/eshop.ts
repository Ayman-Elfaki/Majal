/**
 * Hand-written types mirroring EShop's actual JSON shapes (see samples/EShop). EShop's Wolverine.Http
 * endpoints return `Task<IResult>`, so their OpenAPI document only describes response bodies as the opaque
 * `IResult` schema -- there's nothing useful for nuxt-api-party's OpenAPI type generation to read from the
 * response side, only requests. These types fill that gap instead of leaning on codegen.
 */

export interface Money {
  amount: number
  currency: string
}

export interface CategoryTranslation {
  description: string
  locale: string
}

/** Body for `POST /categories`. */
export interface CreateCategoryRequest {
  name: string
  translations: CategoryTranslation[]
}

export interface CategoryView {
  name: string
  translations: CategoryTranslation[]
}

export interface CreateCategoryResponse {
  id: number
  category: CategoryView
}

/** Item shape for `GET /categories`. */
export interface AdminCategoryItem {
  id: number
  category: CategoryView
}

export interface ProductTranslation {
  name: string
  description: string
  locale: string
}

/** Body for `POST /products/physical`. Price is flattened via `[FlattenDtoFor<Money>]`. */
export interface CreatePhysicalProductRequest {
  categoryId: number
  sku: string
  priceAmount: number
  priceCurrency: string
  tags: string[]
  translations: ProductTranslation[]
  weightKg: number
  initialStockQuantity?: number
}

/** Body for `POST /products/digital`. Price is left as a nested object (unflattened). */
export interface CreateDigitalProductRequest {
  categoryId: number
  sku: string
  price: Money
  tags: string[]
  translations: ProductTranslation[]
  downloadUrl: string
  initialStockQuantity: number
}

export interface PhysicalProductView {
  sku: string
  priceAmount: number
  priceCurrency: string
  categoryId: number
  tags: string[]
  translations?: ProductTranslation[]
  weightKg: number
  initialStockQuantity: number
}

export interface DigitalProductView {
  sku: string
  price: Money
  categoryId: number
  tags: string[]
  translations?: ProductTranslation[]
  downloadUrl: string
  initialStockQuantity: number
}

export type ProductView = PhysicalProductView | DigitalProductView

export function isDigitalProduct(product: ProductView): product is DigitalProductView {
  return 'downloadUrl' in product
}

/** Item shape for `GET /products` and `GET /admin/products/archived`. */
export interface ProductListItem {
  id: number
  product: ProductView
  createdOn?: string
  updatedOn?: string | null
  ordinal?: number
}

/** Item shape for `GET /admin/products/translations`. */
export interface AdminProductTranslation {
  name: string
  description: string
  locale: string
}

export interface CreateCustomerRequest {
  name: string
  email: string
}

export interface CustomerView {
  name: string
  email: string
}

export interface RegisterCustomerResponse {
  id: string
  customer: CustomerView
}

/** Item shape for `GET /customers` -- the same shape as the `POST /customers` response. */
export type CustomerListItem = RegisterCustomerResponse

export interface OrderLineRequest {
  productId: number
  quantity: number
  amountUnitPrice: number
  currencyUnitPrice: string
}

export type PaymentMethodRequest
  = | { $type: 'creditCardPayment', cardholderName: string, last4Digits: string }
    | { $type: 'payPalPayment', payerEmail: string }

/** Body for `POST /orders`. */
export interface PlaceOrderRequest {
  customerId: string
  lines: OrderLineRequest[]
  paymentMethod: PaymentMethodRequest
}

export interface OrderLineView {
  productId: number
  quantity: number
  unitPrice: Money
}

export type PaymentMethodView
  = | { $type: 'creditCardPayment', cardholderName: string, last4Digits: string }
    | { $type: 'payPalPayment', payerEmail: string }

export interface OrderView {
  customerId: string
  lines: OrderLineView[]
  paymentMethod: PaymentMethodView
}

export interface PlaceOrderResponse {
  id: string
  order: OrderView
}

/** Response shape for `GET /orders/{id}`. */
export interface GetOrderResponse {
  id: string
  lineIds: number[]
  order: OrderView
}

/** Item shape for `GET /orders`, newest first. */
export interface OrderListItem {
  id: string
  createdOn: string
  lineIds: number[]
  order: OrderView
}
