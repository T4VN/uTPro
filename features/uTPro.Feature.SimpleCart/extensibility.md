---
layout: default
title: "Extensibility – Simple Cart"
description: "Override where products come from in uTPro Simple Cart by registering a custom IProductResolver, and swap cart storage later without a rewrite."
permalink: "/uTPro.Feature.SimpleCart/extensibility/"
feature: true
feature_name: "Simple Cart"
---

# Extensibility

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

The cart is built around small, replaceable seams so you can adapt it to your own catalog or storage without rewriting anything.

---

## Custom product source (`IProductResolver`)

By default the cart reads products from Umbraco content using the conventional `uTProProduct` aliases. To source products from somewhere else — a different document type, an external PIM, or the future Catalog module — register your own `IProductResolver` in a composer:

```csharp
public sealed class MyProductResolverComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IProductResolver, MyProductResolver>();
}
```

The resolver's job is simple: given a product **key** (and optional SKU), return the authoritative, culture-resolved **name**, **price** and **availability** — or `null` if the product should not be added / should drop from the cart. Because the cart reads everything through this one seam, your prices stay server-side and [price-safe](security/).

> When you supply your own product model, consider setting `AutoProvisionSchema` to `false` so the default `uTProProduct` type is not created. See [Configuration](configuration/).

---

## Swapping cart storage later

The cart stores only the product key, SKU and quantity, behind a cart service seam. The default storage is session-backed, but moving to a database later is an **implementation swap, not a rewrite** — the storefront, API and resolver stay exactly the same.

---

## Roadmap seams

The same approach extends to the planned modules — pluggable payment and shipping providers, and an order-capture pipeline — so each concern can be added or replaced independently. See [Reference](reference/) for the roadmap.
