---
layout: default
title: "uTPro.Feature.SimpleCart"
description: "A lightweight e-commerce engine for Umbraco – cart, checkout, orders, payments, shipping, gift cards and discounts. Fully pluggable, cross-database, multi-language."
permalink: "/uTPro.Feature.SimpleCart/"
feature: true
feature_name: "Simple Cart"
feature_order: 8
feature_tagline: "Cart → checkout → orders → payments → shipping — fully pluggable e-commerce for Umbraco"
---

# uTPro Simple Cart for Umbraco

> For: **Both** — Content Editors manage products as normal Umbraco content; Developers wire the storefront and extend via plugins.

A lightweight, **self-contained** e-commerce engine for Umbraco 16, 17 and 18 (net9.0 / net10.0). Install the NuGet package and you have a working **cart, checkout, orders, dashboard and a backoffice section** — with **no changes to your `Program.cs`**. Payments, shipping, discounts and gift cards are **pluggable add-ons** you install separately.

Part of the uTPro `Simple*` family (alongside [Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)).

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.SimpleCart.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleCart)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.SimpleCart.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleCart)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.simplecart)

---

## Features (core)

- **Self-wiring install** — services, session middleware, migrations and backoffice UI register themselves; clean uninstall leaves nothing behind
- **Session-backed cart** with a public JSON API — prices resolved server-side (cannot be spoofed)
- **Multi-language** — product names/prices via Umbraco culture variants; cart resolves in the current culture
- **Checkout** — turns the cart into a persisted order; line items & prices are frozen from the server-side cart
- **Orders** — stored cross-database (SQL Server, SQLite, PostgreSQL) via Umbraco migrations; status workflow (Pending → Paid → Shipped → Completed / Cancelled)
- **Backoffice section** (uTPro Cart) — Overview with revenue analytics, Products, Orders (detail + status management), Store settings (provider config)
- **Provider Settings UI** — enable/disable and configure payment & shipping providers from the backoffice (auto-generated forms, secrets encrypted)
- **Order adjustment pipeline** — pluggable seam for discounts / gift cards / fees applied at checkout
- **Pluggable IProductResolver** — change where products come from without touching the cart

## Add-ons (separate packages)

| Package | Description |
|---------|-------------|
| [`uTPro.Feature.SimpleCart.Payments.Stripe`](/uTPro.Feature.SimpleCart/payments/) | Stripe Checkout + signed webhooks |
| [`uTPro.Feature.SimpleCart.Payments.VnPay`](/uTPro.Feature.SimpleCart/payments/) | VNPay redirect + HMAC-SHA512 IPN (Vietnam) |
| [`uTPro.Feature.SimpleCart.GiftCards`](/uTPro.Feature.SimpleCart/gift-cards/) | Create / bulk-create / redeem gift cards |
| [`uTPro.Feature.SimpleCart.Discounts`](/uTPro.Feature.SimpleCart/discounts/) | Percentage / fixed-amount coupon codes |

All add-ons register through public interfaces and collection builders — **no edits to the core package or your host**.

---

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](/uTPro.Feature.SimpleCart/getting-started/) | Install, first boot, create products |
| [Cart API](/uTPro.Feature.SimpleCart/cart-api/) | Public cart endpoints + JS client |
| [Checkout & Orders](/uTPro.Feature.SimpleCart/orders/) | Checkout flow, order model, status workflow |
| [Payments](/uTPro.Feature.SimpleCart/payments/) | Payment providers, Stripe & VNPay add-ons, callback flow |
| [Shipping](/uTPro.Feature.SimpleCart/shipping/) | Shipping providers, quotes, flat-rate built-in |
| [Gift Cards](/uTPro.Feature.SimpleCart/gift-cards/) | Gift-card add-on: create, redeem, backoffice section |
| [Discounts](/uTPro.Feature.SimpleCart/discounts/) | Discount/coupon add-on: create, redeem, limits |
| [Extensibility](/uTPro.Feature.SimpleCart/extensibility/) | All extension points + how to build your own add-on |
| [Configuration](/uTPro.Feature.SimpleCart/configuration/) | appsettings reference |
| [Catalog](/uTPro.Feature.SimpleCart/catalog/) | Product document type, CatalogService |
| [Multi-language](/uTPro.Feature.SimpleCart/multi-language/) | Culture variants, cart localization |
| [Security](/uTPro.Feature.SimpleCart/security/) | Price safety, session scoping, encrypted secrets |
| [Reference](/uTPro.Feature.SimpleCart/reference/) | Service interfaces & DI overview |

---

## Quick Start

```bash
dotnet add package uTPro.Feature.SimpleCart
```

That's it. On first boot, the product document type and the orders table are created automatically. Create products in **Content**, enable the **uTPro Cart** section for your user group, and call the cart API from your storefront.

For payments, install an add-on (e.g. Stripe):

```bash
dotnet add package uTPro.Feature.SimpleCart.Payments.Stripe
```

Configure keys in the backoffice **Store settings** (or appsettings) and you have online checkout.

---

## Architecture

```
┌─────────────────────────────────────────┐
│ Core: uTPro.Feature.SimpleCart           │
│ ┌─────┐ ┌────────┐ ┌──────┐ ┌────────┐ │
│ │Cart │→│Checkout│→│Orders│→│ Events │ │
│ └─────┘ └───┬────┘ └──────┘ └────────┘ │
│             │                            │
│   ┌─────────┼──────────┐                │
│   │Adjustments│Shipping│ Payment        │
│   │ pipeline  │Service │ Service        │
│   └─────────┴─────────┴────────────────│
└─────────────────────────────────────────┘
         ▲           ▲           ▲
         │           │           │
  ┌──────┴──┐ ┌─────┴────┐ ┌───┴──────┐
  │Discounts│ │FlatRate   │ │Stripe    │
  │GiftCards│ │YourOwn    │ │VnPay     │
  │  (add-on)│ │  (add-on) │ │  (add-on)│
  └─────────┘ └──────────┘ └──────────┘
```

Everything above the line is the core package. Everything below is a **separate NuGet add-on** that
plugs in via `IOrderAdjustmentProvider`, `IShippingProvider` or `IPaymentProvider` + a composer.
