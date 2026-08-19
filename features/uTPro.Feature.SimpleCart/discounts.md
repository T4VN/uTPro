---
layout: default
title: "Discounts – Simple Cart"
description: "SimpleCart Discounts add-on – percentage and fixed-amount coupon codes."
permalink: "/uTPro.Feature.SimpleCart/discounts/"
feature: true
feature_name: "Simple Cart"
---

# Discounts

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

> Add-on package: `uTPro.Feature.SimpleCart.Discounts`

Create **percentage** or **fixed-amount** coupon codes with optional minimum order, expiry and usage limits. They apply at checkout through the order-adjustment pipeline and are managed from a dedicated **Discounts** backoffice section.

## Install

```bash
dotnet add package uTPro.Feature.SimpleCart.Discounts
```

Grant the **Discounts** section to your user group. Tables are created automatically.

## Coupon types

| Type | Example | How it applies |
|------|---------|----------------|
| **Percentage** | 10% off | `subtotal × percent / 100`, capped at the subtotal |
| **Fixed amount** | $15 off | Capped at subtotal; currency must match the order |

## Conditions

| Condition | Description |
|-----------|-------------|
| Minimum subtotal | Coupon only applies if the order reaches this amount |
| Expiry date | Coupon rejected after this date |
| Usage limit | Maximum times it can be redeemed (0 = unlimited); tracked per order |

## Backoffice section

- **List**: code, value (% or amount), min. order, used / limit, expiry, status
- **Create form**: type toggle (percentage / fixed), value, min subtotal, usage limit, expiry, code (auto-generated or custom), note
- **Actions**: copy code, enable/disable, delete

## Flow

1. Shopper enters the code → `uTProSimpleCart.previewAdjustments(["SAVE10"])` shows the reduction.
2. At checkout, pass `codes: ["SAVE10"]` → `DiscountAdjustmentProvider` validates and reduces the total.
3. `DiscountUsageHandler` counts the use (once per order).

## JS client

```js
const preview = await uTProSimpleCart.previewAdjustments(["SAVE10"]);
// preview.adjustments → [{ type: "discount", label: "Discount SAVE10 (10%)", amount: -5.00 }]

await uTProSimpleCart.checkout({
  customerName: "Jane",
  customerEmail: "jane@example.com",
  codes: ["SAVE10"],
});
```

## Configuration

Fixed-amount discounts only apply to orders in their configured currency. The add-on honours the backoffice **Store settings** toggle (globally on/off). No appsettings are required.
