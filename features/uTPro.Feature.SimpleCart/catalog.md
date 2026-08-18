---
layout: default
title: "Product Catalog – Simple Cart"
description: "The uTProProduct document type, its property aliases, product categories, and the read-only catalog JSON API."
permalink: "/uTPro.Feature.SimpleCart/catalog/"
feature: true
feature_name: "Simple Cart"
---

# Product Catalog

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

Products are regular Umbraco content, projected into a lightweight read model that the cart and storefront share. On first boot the package auto-provisions a **Product** document type so you can start selling immediately.

---

## The `uTProProduct` document type

The auto-provisioned Product type carries these properties. If you build your own product type, use the **same property aliases** (or replace the resolver — see [Extensibility](extensibility/)).

| Property | Alias | Purpose |
|---|---|---|
| Product name | `productName` | Localized display name |
| SKU | `sku` | Stock-keeping unit / variant code |
| Price | `price` | Authoritative unit price |
| Description | `description` | Localized description (optional) |
| Image | `image` | Primary image (optional) |
| Is available | `isAvailable` | Whether the product can currently be purchased |

![The uTProProduct document type](/screenshots/uTPro.Feature.SimpleCart/product-doctype.png)

> Make `productName`, `price` and `description` **culture-variant** so each language can be translated. See [Multi-language](multi-language/).

---

## Categories

There is no separate "category" type — any content node that has product children acts as a category. The [catalog API](#catalog-api) can return all products under a given parent node, so your existing content structure becomes the catalog structure.

---

## Availability & self-healing

- A product with **Is available** unchecked cannot be added to the cart.
- If a product is **unpublished or deleted**, it drops out of any cart automatically on the next read — no stale lines, no orphaned prices.

---

## Catalog API

Read-only and anonymous (product content is already public on the site). Served at `/api/simplecart/catalog`.

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/simplecart/catalog/products` | List products |
| `GET` | `/api/simplecart/catalog/products?parentKey={guid}` | List products under a parent/category node |
| `GET` | `/api/simplecart/catalog/products/{key}` | Get a single product by content key |

Each product is returned with its key, localized name, SKU, price, optional description, URL, image URL and availability — all resolved in the **current request culture**.

```http
GET /api/simplecart/catalog/products?parentKey=8f3c...e21a
```

Use this API to build product grids and detail pages for a headless or hybrid storefront, then hand the product **key** to the [cart API](cart-api/) to add items.
