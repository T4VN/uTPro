---
layout: default
title: "Reference – Simple Cart"
description: "Product schema aliases, endpoints, static assets and the FoxCart-style module roadmap for uTPro Simple Cart."
permalink: "/uTPro.Feature.SimpleCart/reference/"
feature: true
feature_name: "Simple Cart"
---

# Reference

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

---

## Product schema

Auto-provisioned on first boot (unless disabled via `AutoProvisionSchema`).

| Item | Alias | Notes |
|---|---|---|
| Document type | `uTProProduct` | The product content type |
| Product name | `productName` | Make culture-variant to translate |
| SKU | `sku` | Variant / stock code |
| Price | `price` | Numeric unit price |
| Description | `description` | Optional, culture-variant |
| Image | `image` | Optional media |
| Is available | `isAvailable` | Purchasable toggle |

---

## Endpoints

| API | Base route | Access |
|---|---|---|
| Cart | `/api/simplecart/cart` | Anonymous, session-scoped |
| Catalog | `/api/simplecart/catalog` | Anonymous, read-only |

See [Cart API](cart-api/) and [Product Catalog](catalog/) for full method/route tables.

---

## Static assets

| Asset | Path |
|---|---|
| Storefront script | `/uTPro/simplecart/simplecart.js` |
| Storefront stylesheet | `/uTPro/simplecart/simplecart.css` |
| Cart component view | `Views/Shared/Components/Cart/Default.cshtml` (overridable) |

---

## Configuration

| Section | Key | Default |
|---|---|---|
| `uTPro:SimpleCart` | `MaxQuantityPerLine` | `999` |
| `uTPro:SimpleCart` | `AutoProvisionSchema` | `true` |
| `uTPro:SimpleCart` | `SessionCookieName` | `uTPro.SimpleCart.Session` |

See [Configuration](configuration/) for details.

---

## Roadmap (FoxCart-style module split)

Simple Cart is the **cart** slice of a larger, modular commerce story. Planned modules:

- **Catalog** — richer product/category/variant modelling and a formal product service.
- **Checkout** — a pluggable checkout pipeline (address → shipping → payment → confirmation).
- **Orders** — order persistence, status workflow and a backoffice order dashboard.
- **Payments** — pluggable payment providers, one package per gateway.
- **Shipping** — pluggable shipping providers for rate calculation.

Each module plugs into the existing seams (see [Extensibility](extensibility/)), so adopting them is additive rather than a migration.
