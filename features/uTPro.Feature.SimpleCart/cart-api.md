---
layout: default
title: "Cart API – Simple Cart"
description: "The public JSON cart API for uTPro Simple Cart: get, add, update quantity, remove and clear, with price-safe server-side resolution."
permalink: "/uTPro.Feature.SimpleCart/cart-api/"
feature: true
feature_name: "Simple Cart"
---

# Cart API

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

The public storefront cart API is served at `/api/simplecart/cart`. It is intentionally **anonymous** (a shopper does not need to log in to build a cart) and **session-scoped** (a cart belongs to one visitor). See [Security](security/).

---

## Endpoints

| Method | Route | Body | Result |
|--------|-------|------|--------|
| `GET` | `/api/simplecart/cart` | — | Current cart |
| `POST` | `/api/simplecart/cart/items` | `{ "productKey": "<guid>", "sku": "optional", "quantity": 1 }` | Updated cart |
| `PUT` | `/api/simplecart/cart/items/{productKey}` | `{ "quantity": 2 }` | Updated cart |
| `DELETE` | `/api/simplecart/cart/items/{productKey}` | — | Updated cart |
| `DELETE` | `/api/simplecart/cart` | — | `204 No Content` |

The request carries **only** a product key (plus optional SKU and quantity) — never a price or name.

---

## Add an item

```http
POST /api/simplecart/cart/items
Content-Type: application/json

{ "productKey": "8f3c...e21a", "quantity": 2 }
```

- Adding a product that is already in the cart **increments** that line rather than duplicating it.
- Quantity is clamped to the configured maximum per line (see [Configuration](configuration/)); values below 1 are treated as 1.
- An unknown, unpublished or unavailable product returns **404 Not Found**.
- A missing or empty `productKey` returns **400 Bad Request**.

## Update quantity

```http
PUT /api/simplecart/cart/items/8f3c...e21a
Content-Type: application/json

{ "quantity": 5 }
```

Sets the **absolute** quantity for the line. A quantity of `0` (or less) removes the line.

## Remove an item / clear the cart

```http
DELETE /api/simplecart/cart/items/8f3c...e21a   → updated cart
DELETE /api/simplecart/cart                      → 204 No Content
```

---

## Cart response shape

```json
{
  "items": [
    {
      "productKey": "8f3c...e21a",
      "sku": "TSHIRT-BLK-M",
      "name": "Black T-Shirt",
      "unitPrice": 19.90,
      "quantity": 2,
      "lineTotal": 39.80
    }
  ],
  "totalQuantity": 2,
  "subTotal": 39.80
}
```

`name` and `unitPrice` are resolved live from product content in the current culture on every read, and totals are computed on read, so they can never drift from the underlying lines.
