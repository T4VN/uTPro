---
layout: default
title: "Gift Cards"
description: "SimpleCart Gift Cards add-on – create, bulk-create and redeem gift cards at checkout."
permalink: "/uTPro.Feature.SimpleCart/gift-cards/"
---

# Gift Cards

> Add-on package: `uTPro.Feature.SimpleCart.GiftCards`

Create and bulk-create gift cards, redeem them at checkout through the order-adjustment pipeline, and manage them from a dedicated **Gift Cards** backoffice section.

## Install

```bash
dotnet add package uTPro.Feature.SimpleCart.GiftCards
```

Grant the **Gift Cards** section to your user group. Tables are created automatically.

## How it works

1. **Create cards** in the backoffice: single or **bulk** (1–1000 cards with the same balance). Codes are auto-generated (e.g. `GC-ABCD-EFGH-JKLM`) or manually set.
2. **Shopper enters code** at checkout via `codes: ["GC-..."]`.
3. The `GiftCardAdjustmentProvider` reduces the order total by the card balance (same currency, never below zero).
4. When the order is placed, `GiftCardRedemptionHandler` deducts the redeemed amount from the card balance (idempotent per card/order).

## Backoffice section

- **Stats**: Total cards, Active cards, Current value, Total redeemed
- **List**: code, balance, initial balance, expiry, status
- **Actions**: copy code, enable/disable, delete
- **Create form**: balance, currency, expiry, note, optional custom code
- **Bulk create form**: count, balance per card, expiry, note (preview shows total value)

## Configuration

- Cards honour the backoffice **Store settings** enable/disable toggle (adjustment provider globally on/off).
- Redemption currency must match the order currency.
- Balances are stored as exact integer minor units (cross-database safe).

## JS client

```js
// Preview the discount before checkout
const preview = await uTProSimpleCart.previewAdjustments(["GC-ABCD-EFGH-JKLM"]);
// preview.adjustments → [{ type: "giftcard", label: "Gift card GC-...", amount: -50.00 }]

// Apply at checkout
await uTProSimpleCart.checkout({
  customerName: "Jane",
  customerEmail: "jane@example.com",
  codes: ["GC-ABCD-EFGH-JKLM"],
});
```
