---
layout: default
title: "Security – Simple Cart"
description: "The security model of uTPro Simple Cart: price-safe server-side resolution, anonymous session-scoped endpoints, and roadmap notes for checkout."
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

## Roadmap: checkout, orders & payments

The current package is the **cart** slice. Future checkout, order and payment endpoints are a different security posture and, when added, must:

- add authentication where appropriate (the cart stays anonymous; capturing an order does not),
- re-validate stock and pricing server-side at capture time, and
- **freeze a price snapshot** on the order so historical records are stable.

See [Reference](reference/) for the full roadmap.
