---
layout: default
title: "uTPro.Feature.SimpleCart"
description: "A lightweight, self-contained shopping cart for Umbraco – session-backed cart, product catalog and multi-language, price-safe pricing with no host code changes."
permalink: "/uTPro.Feature.SimpleCart/"
feature: true
feature_name: "Simple Cart"
feature_order: 8
feature_tagline: "Session-backed cart, product catalog & multi-language pricing"
---

# uTPro Simple Cart for Umbraco

> For: **Both** — Content Editors manage products as normal Umbraco content; Developers render the cart and wire up the storefront with no host code.

A lightweight, **self-contained** shopping cart — add a NuGet package and you have a working cart, a product document type, a storefront cart component and a JSON API, with **no changes to your `Program.cs`**.

Part of the uTPro `Simple*` family (alongside [Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)). Works with **Umbraco 16, 17 and 18**.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.SimpleCart.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleCart)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.SimpleCart.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleCart)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.simplecart)

![uTPro Simple Cart](/screenshots/uTPro.Feature.SimpleCart/cart-overview.png)

---

## Features

- **Self-wiring install** — services, session middleware and schema register themselves; nothing to add to `Program.cs`, and a clean uninstall leaves nothing behind
- **Product document type auto-provisioned** — a `uTProProduct` type with name, SKU, price, description and availability is created on first boot (an optional image property is read if you add one)
- **Session-backed cart** — no database, no migrations required for the cart itself
- **Price-safe by design** — names and prices are always resolved server-side from Umbraco content; the client can never send a price
- **Multi-language** — product name and price render in the visitor's current culture using Umbraco culture variants
- **Self-healing** — a product that is deleted or unpublished drops out of the cart automatically on the next read
- **Storefront cart component** — drop-in Razor ViewComponent plus a dependency-free JavaScript helper wired via `data-*` attributes
- **Public JSON APIs** — a cart API (`/api/simplecart/cart`) and a read-only catalog API (`/api/simplecart/catalog`)
- **Pluggable product source** — swap the default `IProductResolver` to source products from anywhere

---

## Quick Start

```bash
dotnet add package uTPro.Feature.SimpleCart
```

Render the cart anywhere:

```razor
@await Component.InvokeAsync("Cart")
```

Add an "Add to cart" button (the JS helper does the rest):

```html
<button data-simplecart-add="@product.Key">Add to cart</button>
<span data-simplecart-count>0</span>
```

| Umbraco | .NET | Target |
|---|---|---|
| 16 | .NET 9 | `net9.0` |
| 17 & 18 | .NET 10 | `net10.0` |

---

## Configuration

No configuration required — the cart works out of the box. See [Configuration](configuration/) for the optional `uTPro:SimpleCart` settings (max quantity per line, schema auto-provisioning, session cookie name).

---

## Documentation

| Guide | Description |
|---|---|
| [Getting Started](getting-started/) | Install, compatibility, self-wiring, where products live |
| [Product Catalog](catalog/) | The `uTProProduct` document type, property aliases, categories, catalog API |
| [Rendering the Cart](rendering/) | Cart ViewComponent, storefront JavaScript, `data-*` attributes, styling |
| [Cart API](cart-api/) | Public JSON cart endpoints and payloads |
| [Multi-language](multi-language/) | Culture variants, dictionary items, language vs. currency |
| [Configuration](configuration/) | `uTPro:SimpleCart` options in `appsettings.json` |
| [Security](security/) | Anonymous & session-scoped endpoints, price-safe model |
| [Extensibility](extensibility/) | Custom `IProductResolver` / catalog source |
| [Reference](reference/) | Product schema, feature overview, roadmap |

---

## License

Free to use (including commercially). See the package [license](https://www.nuget.org/packages/uTPro.Feature.SimpleCart/license) for details.

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.SimpleCart) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.simplecart)
