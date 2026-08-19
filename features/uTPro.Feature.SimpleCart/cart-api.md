---
layout: default
title: "Cart API"
description: "SimpleCart public cart, checkout, shipping, payment and adjustment endpoints."
permalink: "/uTPro.Feature.SimpleCart/cart-api/"
---

# Public API

All storefront endpoints are anonymous and JSON. The cart is scoped to the visitor's session.

## Cart — `/api/simplecart/cart`

| Method | Route | Body | Result |
|--------|-------|------|--------|
| `GET` | `/cart` | — | Current cart |
| `POST` | `/cart/items` | `{ productKey, sku?, quantity }` | Updated cart (404 if unavailable) |
| `PUT` | `/cart/items/{productKey}` | `{ quantity }` | Updated cart (0 removes) |
| `DELETE` | `/cart/items/{productKey}` | — | Updated cart |
| `DELETE` | `/cart` | — | 204 No Content |

## Checkout — `/api/simplecart/checkout`

| Method | Route | Body |
|--------|-------|------|
| `POST` | `/checkout` | `{ customerName, customerEmail, customerPhone?, shippingAddress?, notes?, shippingMethod?, paymentProvider?, codes? }` |

Returns the created order or 400.

## Shipping — `/api/simplecart/shipping`

| Method | Route | Body |
|--------|-------|------|
| `POST` | `/shipping/quotes` | `{ countryCode?, address? }` |

Returns `[{ methodAlias, methodName, price, providerAlias }]`.

## Payment — `/api/simplecart/payment`

| Method | Route | Body / Purpose |
|--------|-------|----------------|
| `GET` | `/payment/methods` | List enabled methods |
| `POST` | `/payment/start` | `{ orderNumber, providerAlias?, returnUrl?, cancelUrl? }` |
| `GET\|POST` | `/payment/callback/{alias}` | Gateway callback (provider-specific) |

## Adjustments — `/api/simplecart/adjustments`

| Method | Route | Body |
|--------|-------|------|
| `POST` | `/adjustments/preview` | `{ codes: ["SAVE10", "GC-..."] }` |

Returns `{ currency, subTotal, adjustments, adjustmentTotal, total }`.

## Catalog — `/api/simplecart/catalog`

| Method | Route |
|--------|-------|
| `GET` | `/catalog/products?parentKey={guid?}` |
| `GET` | `/catalog/products/{key}` |

## JavaScript client

```html
<script src="/uTPro/simplecart/simplecart.js"></script>
```

```js
uTProSimpleCart.get();
uTProSimpleCart.add(productKey, quantity, sku);
uTProSimpleCart.update(productKey, quantity);
uTProSimpleCart.remove(productKey);
uTProSimpleCart.clear();
uTProSimpleCart.shippingQuotes({ countryCode, address });
uTProSimpleCart.paymentMethods();
uTProSimpleCart.previewAdjustments(codes);
uTProSimpleCart.checkout({ customerName, customerEmail, ..., codes });
uTProSimpleCart.startPayment({ orderNumber, providerAlias, returnUrl, cancelUrl });

// Events
document.addEventListener("simplecart:changed", e => { /* e.detail = cart */ });
document.addEventListener("simplecart:ordered", e => { /* e.detail = order */ });
```
