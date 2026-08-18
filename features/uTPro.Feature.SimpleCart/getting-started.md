---
layout: default
title: "Getting Started – Simple Cart"
description: "Install uTPro Simple Cart, understand the self-wiring install, and find where products live in the Umbraco backoffice."
permalink: "/uTPro.Feature.SimpleCart/getting-started/"
feature: true
feature_name: "Simple Cart"
---

# Getting Started

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

## Install via NuGet

```bash
dotnet add package uTPro.Feature.SimpleCart
```

That's it. The package **self-wires** its services and session middleware through an Umbraco composer, so there are **no changes to your `Program.cs`**. Uninstalling the NuGet package removes everything cleanly — nothing is left behind in the host.

## Framework / Umbraco compatibility

| Umbraco | .NET | Package target |
|---|---|---|
| 16 | .NET 9 | `net9.0` |
| 17 & 18 | .NET 10 | `net10.0` |

The package multi-targets both, so the correct dependencies are restored automatically for your project.

## What happens on first run

On first boot the package prepares everything the cart needs — with **no manual SQL, no migrations and no configuration**:

- **Product document type** — a `uTProProduct` type (with name, SKU, price, description, image and availability properties) is auto-provisioned, so you can start adding products immediately. This is idempotent and safe across restarts.
- **Session** — a distributed-memory-backed session is enabled to hold each visitor's cart. The cart itself needs no database.
- **APIs & storefront** — the [cart API](cart-api/), the [catalog API](catalog/) and the [cart component](rendering/) become available.

> If you already have your own product schema, you can turn off auto-provisioning and point the cart at your content — see [Configuration](configuration/) and [Extensibility](extensibility/).

## Where products live

Products are ordinary Umbraco content nodes based on the auto-provisioned `uTProProduct` document type. Create them anywhere in the content tree and publish them like any other page.

1. Open the **Content** section
2. Create a node of type **Product** (`uTProProduct`)
3. Fill in the name, price, SKU and availability, then **Save and publish**

To organize products into "categories", nest them under a parent node — the [catalog API](catalog/) can list the products under any parent. See [Product Catalog](catalog/) for the full field list and property aliases.

## Build your first cart

1. Add products in **Content** (above)
2. On a product listing or detail template, add an **Add to cart** button using the `data-simplecart-add` attribute
3. Render the cart with the `Cart` ViewComponent on your basket page

See [Rendering the Cart](rendering/) for the storefront wiring, and [Multi-language](multi-language/) to make names and prices follow the visitor's culture.
