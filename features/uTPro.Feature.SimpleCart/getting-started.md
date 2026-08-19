---
layout: default
title: "Getting Started – Simple Cart"
description: "Install and run SimpleCart in under 5 minutes."
permalink: "/uTPro.Feature.SimpleCart/getting-started/"
feature: true
feature_name: "Simple Cart"
---

# Getting Started

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)


## 1. Install

```bash
dotnet add package uTPro.Feature.SimpleCart
```

No `Program.cs` changes are required — the package self-wires its services, session middleware, migrations and backoffice UI through Umbraco composers.

## 2. First boot

On first run, SimpleCart automatically provisions:
- An **orders table** (`uTProSimpleCartOrder`) via an idempotent Umbraco migration (cross-database: SQL Server, SQLite, PostgreSQL).
- A **Product** document type (`uTProProduct`) with properties: Product Name, SKU, Price, Description, Available.

Disable auto-provisioning with `uTPro:SimpleCart:AutoProvisionSchema = false` if you manage the schema yourself.

## 3. Create products

In the **Content** section, create nodes of type **Product** and publish them. Enable *Allow vary by culture* for multi-language names/prices.

## 4. Enable the backoffice section

Go to **Users → User Groups → (your group) → Sections**, check **uTPro Cart Section**, and save. The section then appears in the top navigation bar.

## 5. Add a payment gateway (optional)

```bash
dotnet add package uTPro.Feature.SimpleCart.Payments.Stripe
# or
dotnet add package uTPro.Feature.SimpleCart.Payments.VnPay
```

Configure keys in **Store settings** (or appsettings). See [Payments](/uTPro.Feature.SimpleCart/payments/).

## 6. Add discounts / gift cards (optional)

```bash
dotnet add package uTPro.Feature.SimpleCart.Discounts
dotnet add package uTPro.Feature.SimpleCart.GiftCards
```

Grant the new sections to your user group. See [Discounts](/uTPro.Feature.SimpleCart/discounts/) and [Gift Cards](/uTPro.Feature.SimpleCart/gift-cards/).

## 7. Render the storefront

Include the bundled JS client and call the API:

```html
<script src="/uTPro/simplecart/simplecart.js"></script>
```

```js
uTProSimpleCart.add(productKey);          // add to cart
uTProSimpleCart.get();                     // read cart
uTProSimpleCart.shippingQuotes({});        // get shipping options
uTProSimpleCart.previewAdjustments(codes); // preview discount/gift-card
uTProSimpleCart.checkout({ customerName, customerEmail, shippingMethod, paymentProvider, codes });
uTProSimpleCart.startPayment({ orderNumber, returnUrl, cancelUrl }); // redirect to gateway
```

Or use the Razor view component:

```cshtml
@await Component.InvokeAsync("Cart")
```

## 8. Configuration (optional)

```json
{
  "uTPro": {
    "SimpleCart": {
      "Currency": "USD",
      "MaxQuantityPerLine": 999,
      "ShippingFlatRate": 0,
      "FreeShippingOver": null,
      "AutoProvisionSchema": true,
      "SessionCookieName": "uTPro.SimpleCart.Session"
    }
  }
}
```

See [Configuration](/uTPro.Feature.SimpleCart/configuration/) for details.
