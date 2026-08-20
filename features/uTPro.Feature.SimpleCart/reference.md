---
layout: default
title: "Reference – Simple Cart"
description: "Product schema aliases, endpoints, static assets, configuration keys and architecture overview for uTPro Simple Cart."
permalink: "/uTPro.Feature.SimpleCart/reference/"
feature: true
feature_name: "Simple Cart"
---

# Reference

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

---

## Product schema

Auto-provisioned on first boot (unless disabled via `AutoProvisionSchema`). The `uTProProduct` type is created with these properties and is allowed at the content root.

| Item | Alias | Notes |
|---|---|---|
| Document type | `uTProProduct` | The product content type |
| Product Name | `productName` | Mandatory. Make culture-variant to translate |
| SKU | `sku` | Variant / stock code |
| Price | `price` | Mandatory. Numeric unit price |
| Description | `description` | Optional, culture-variant |
| Available | `isAvailable` | Purchasable toggle (defaults to available when absent) |

> The catalog also reads an optional **`image`** media property if present, but it is **not** auto-created. Add it to the Product type yourself to enable product images.

---

## Endpoints

| API | Base route | Access |
|---|---|---|
| Cart | `/api/simplecart/cart` | Anonymous, session-scoped |
| Catalog | `/api/simplecart/catalog` | Anonymous, read-only |
| Checkout | `/api/simplecart/checkout` | Anonymous |
| Shipping | `/api/simplecart/shipping` | Anonymous |
| Payment | `/api/simplecart/payment` | Anonymous |
| Adjustments | `/api/simplecart/adjustments` | Anonymous |
| Backoffice | `/umbraco/management/api/v1/utpro/simple-cart` | Authenticated (backoffice users) |

See [Cart API](cart-api/) for full method/route tables, [Orders](orders/) for checkout, and [Payments](payments/) for the payment flow.

---

## Static assets

| Asset | Path |
|---|---|
| Storefront script | `/uTPro/simplecart/simplecart.js` |
| Storefront stylesheet | `/uTPro/simplecart/simplecart.css` |
| Cart component view | `Views/Shared/Components/Cart/Default.cshtml` (overridable) |
| Product card partial | `Views/Partials/SimpleCart/_ProductCard.cshtml` (overridable) |

---

## Configuration

| Section | Key | Default |
|---|---|---|
| `uTPro:SimpleCart` | `Currency` | `USD` |
| `uTPro:SimpleCart` | `MaxQuantityPerLine` | `999` |
| `uTPro:SimpleCart` | `ShippingFlatRate` | `0` |
| `uTPro:SimpleCart` | `FreeShippingOver` | `null` |
| `uTPro:SimpleCart` | `AutoProvisionSchema` | `true` |
| `uTPro:SimpleCart` | `SessionCookieName` | `uTPro.SimpleCart.Session` |

See [Configuration](configuration/) for details.

---

## Architecture

Simple Cart is a modular commerce engine — each concern is a separate layer that plugs in via public interfaces:

| Module | Status | Description |
|--------|--------|-------------|
| **Cart** | ✅ Included | Session-scoped, server-side price resolution |
| **Catalog** | ✅ Included | Product content type, read-only JSON API, `ICatalogService` |
| **Checkout** | ✅ Included | Guest checkout, frozen order snapshot, adjustment pipeline |
| **Orders** | ✅ Included | Persistence, status workflow, backoffice dashboard |
| **Payments** | ✅ Add-ons | Pluggable `IPaymentProvider` — Stripe, VNPay, COD built-in |
| **Shipping** | ✅ Add-ons | Pluggable `IShippingProvider` — flat-rate built-in |
| **Discounts** | ✅ Add-on | Percentage / fixed-amount coupon codes |
| **Gift Cards** | ✅ Add-on | Create, bulk-create, redeem at checkout |

Each add-on registers through public interfaces and collection builders — no edits to the core package. See [Extensibility](extensibility/) for all seams.

## Planned

- `ITaxCalculator` — populate the tax total already stored on orders.
- Member-linked persistent carts and order history.
- Richer product variants / options modelling.
