---
layout: default
title: "Cart API – Simple Cart"
description: "SimpleCart public cart, checkout, shipping, payment and adjustment endpoints — full REST reference with response shapes."
permalink: "/uTPro.Feature.SimpleCart/cart-api/"
feature: true
feature_name: "Simple Cart"
---

# Public API

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

All storefront endpoints are anonymous and JSON. The cart is scoped to the visitor's session. Base path: `/api/simplecart`.

---

## Cart — `/api/simplecart/cart`

| Method | Route | Body | Result |
|--------|-------|------|--------|
| `GET` | `/cart` | — | Current cart |
| `POST` | `/cart/items` | `{ "productKey": "<guid>", "sku": "optional", "quantity": 1 }` | Updated cart (404 if product unavailable) |
| `PUT` | `/cart/items/{productKey}` | `{ "quantity": 2 }` | Updated cart (quantity 0 removes the line) |
| `DELETE` | `/cart/items/{productKey}` | — | Updated cart |
| `DELETE` | `/cart` | — | 204 No Content |

**Cart response shape:**

```json
{
  "items": [
    { "productKey": "…", "sku": "", "name": "Widget", "unitPrice": 9.99, "quantity": 2, "lineTotal": 19.98 }
  ],
  "totalQuantity": 2,
  "subTotal": 19.98
}
```

The request never carries a price or name — both are resolved **server-side** from the product content in the current culture.

---

## Checkout — `/api/simplecart/checkout`

| Method | Route | Body |
|--------|-------|------|
| `POST` | `/checkout` | `{ customerName, customerEmail, customerPhone?, shippingAddress?, notes?, shippingMethod?, paymentProvider?, codes? }` |

Only `customerName` and `customerEmail` are required. Returns the created order or `400` if the cart is empty / validation fails.

**Order response shape:**

```json
{
  "id": 12,
  "orderNumber": "SC-20260819-a1b2c3d4e5f6",
  "status": "Pending",
  "customerName": "Jane Doe",
  "customerEmail": "jane@example.com",
  "currency": "USD",
  "lines": [
    { "productKey": "…", "sku": "", "name": "Widget", "unitPrice": 9.99, "quantity": 2, "lineTotal": 19.98 }
  ],
  "subTotal": 19.98,
  "shippingTotal": 0,
  "taxTotal": 0,
  "grandTotal": 19.98,
  "createdUtc": "2026-08-19T10:00:00Z"
}
```

---

## Shipping — `/api/simplecart/shipping`

| Method | Route | Body |
|--------|-------|------|
| `POST` | `/shipping/quotes` | `{ "countryCode": "VN", "address": "optional" }` |

Returns quotes from all registered `IShippingProvider`s:

```json
[{ "methodAlias": "standard", "methodName": "Standard shipping", "price": 5.00, "providerAlias": "flat-rate" }]
```

Pass the chosen `methodAlias` to checkout as `shippingMethod`.

---

## Payment — `/api/simplecart/payment`

| Method | Route | Body / Purpose |
|--------|-------|----------------|
| `GET` | `/payment/methods` | List enabled payment methods: `[{ alias, name }]` |
| `POST` | `/payment/start` | `{ "orderNumber": "…", "providerAlias": "?", "returnUrl": "?", "cancelUrl": "?" }` |
| `GET\|POST` | `/payment/callback/{alias}` | Gateway callback (provider-specific, signature-verified) |

**Start response:**

```json
{ "success": true, "action": "Redirect", "redirectUrl": "https://checkout.stripe.com/..." }
```

`action` values: `Redirect` (online gateway), `Completed` (settled immediately), `Pending` (offline, e.g. COD).

---

## Adjustments — `/api/simplecart/adjustments`

| Method | Route | Body |
|--------|-------|------|
| `POST` | `/adjustments/preview` | `{ "codes": ["SAVE10", "GC-XXXX-YYYY-ZZZZ"] }` |

Preview discount / gift-card adjustments before checkout:

```json
{
  "currency": "USD",
  "subTotal": 19.98,
  "adjustments": [
    { "type": "discount", "label": "Discount SAVE10 (10%)", "code": "SAVE10", "amount": -2.00 }
  ],
  "adjustmentTotal": -2.00,
  "total": 17.98
}
```

Pass the same `codes` to checkout to apply them.

---

## Catalog — `/api/simplecart/catalog`

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/catalog/products` | List all products |
| `GET` | `/catalog/products?parentKey={guid}` | List products under a category node |
| `GET` | `/catalog/products/{key}` | Get a single product |

Products are returned with key, localized name, SKU, price, description, URL, image URL and availability — resolved in the current request culture.

---

## Backoffice management API

Base: `/umbraco/management/api/v1/utpro/simple-cart`

These endpoints power the **uTPro Cart** backoffice section. They require an authenticated backoffice user with the Cart section granted. All are `POST`.

| Route | Purpose |
|-------|---------|
| `/permissions` | Check current user permissions and schema status |
| `/products` | List products (includes unpublished drafts) |
| `/orders` | Paged orders. Body: `{ skip, take, status?, paymentStatus?, search? }` |
| `/order` | Single order detail. Body: `{ id }` |
| `/update-order-status` | Change status. Body: `{ id, status }` |
| `/order-stats` | Dashboard metrics (revenue, counts by status) |
| `/provider-settings` | List all payment/shipping provider configs (secrets masked) |
| `/save-provider-settings` | Save provider config. Body: `{ alias, enabled, values }` |
| `/settings` | Current bound `uTPro:SimpleCart` options |

Normal integrations should use the public API above. The management API exists for the section UI.

---

## JavaScript client

```html
<script src="/uTPro/simplecart/simplecart.js"></script>
```

```js
// Cart
uTProSimpleCart.get();
uTProSimpleCart.add(productKey, quantity, sku);
uTProSimpleCart.update(productKey, quantity);
uTProSimpleCart.remove(productKey);
uTProSimpleCart.clear();

// Shipping & payment
uTProSimpleCart.shippingQuotes({ countryCode, address });
uTProSimpleCart.paymentMethods();

// Adjustments (discounts / gift cards)
uTProSimpleCart.previewAdjustments(codes);

// Checkout & payment
uTProSimpleCart.checkout({ customerName, customerEmail, customerPhone, shippingAddress, notes, shippingMethod, paymentProvider, codes });
uTProSimpleCart.startPayment({ orderNumber, providerAlias, returnUrl, cancelUrl });

// Events
document.addEventListener("simplecart:changed", e => { /* e.detail = updated cart */ });
document.addEventListener("simplecart:ordered", e => { /* e.detail = created order */ });
```
