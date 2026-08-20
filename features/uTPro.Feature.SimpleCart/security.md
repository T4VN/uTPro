---
layout: default
title: "Security – Simple Cart"
description: "The security model of uTPro Simple Cart: price-safe server-side resolution, session-scoped endpoints, checkout integrity, and encrypted provider secrets."
permalink: "/uTPro.Feature.SimpleCart/security/"
feature: true
feature_name: "Simple Cart"
---

# Security

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

---

## Price-safe by design

The single most important guarantee: **the client can never dictate a price or name.**

- Add/update requests carry only a product **key** (plus optional SKU and quantity).
- Names and prices are always resolved **server-side** from the authoritative Umbraco product content.
- Totals are computed on read, so a tampered request cannot change what a line costs.

---

## Anonymous & session-scoped endpoints

The cart and catalog APIs are intentionally **anonymous**:

- A shopper does not need to log in to browse the catalog or build a cart.
- Each cart is **session-scoped** — it belongs to a single visitor and no cross-visitor data is exposed.
- The catalog API is **read-only** and exposes only content that is already public on the site.

Because carts are functional (not tracking), the session cookie is marked essential and `HttpOnly`.

---

## Self-healing carts

If a product is unpublished or deleted, it is dropped from any cart on the next read. A cart can therefore never resurrect a price or product that is no longer available.

---

## Checkout & orders

The cart is anonymous; checkout also remains anonymous (guest checkout) but enforces server-side integrity:

- The checkout endpoint **re-reads the cart server-side** — it never accepts prices or line items from the client request.
- A **frozen price snapshot** is stored on the order so historical records are stable regardless of future product edits.
- Stock/availability is re-validated at capture time — unpublished or unavailable products are rejected.

## Payments

Payment callbacks (Stripe webhooks, VNPay IPN) are **signature-verified** by each provider before any state change. The callback endpoint is provider-specific and does not expose order data.

Provider secrets (API keys, webhook secrets) are **encrypted at rest** in the database and masked in the backoffice UI. They can also be set via appsettings (which is developer-managed, outside the database).

See [Payments](/uTPro.Feature.SimpleCart/payments/) for provider details and [Extensibility](/uTPro.Feature.SimpleCart/extensibility/) to build your own.
