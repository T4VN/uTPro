---
layout: default
title: "Shipping – Simple Cart"
description: "SimpleCart shipping providers – quotes, flat-rate built-in, and building your own."
permalink: "/uTPro.Feature.SimpleCart/shipping/"
feature: true
feature_name: "Simple Cart"
---

# Shipping

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)


Shipping rate calculation is **pluggable** via `IShippingProvider`. Each provider returns one or more `ShippingQuote`s for the current cart; all providers' quotes are merged for the shopper.

## Built-in: Flat rate

A configurable flat-rate provider ships enabled by default. Configure via backoffice **Store settings** (or appsettings):

| Setting | Description |
|---------|-------------|
| `ShippingFlatRate` | Price charged for "Standard shipping" (default 0) |
| `FreeShippingOver` | Subtotal threshold for free shipping (null = disabled) |

## Endpoint

```
POST /api/simplecart/shipping/quotes
Body: { "countryCode": "VN", "address": "optional" }
→ [{ "methodAlias": "standard", "methodName": "Standard shipping", "price": 5.00, "providerAlias": "flat-rate" }]
```

Pass the chosen `methodAlias` to checkout as `shippingMethod`.

## Build your own

```csharp
public sealed class ExpressShippingProvider : IShippingProvider
{
    public string Alias => "express";
    public string Name => "Express courier";

    public Task<IEnumerable<ShippingQuote>> GetQuotesAsync(ShippingContext context)
        => Task.FromResult<IEnumerable<ShippingQuote>>(
        [
            new ShippingQuote { MethodAlias = "express", MethodName = "Express (1–2 days)",
                                Price = 12.50m, ProviderAlias = Alias }
        ]);
}

// In your composer:
builder.SimpleCartShippingProviders().Append<ExpressShippingProvider>();
```

The provider is DI-resolved, so it can inject its own options/HTTP clients. Add `IConfigurableProvider` for a backoffice settings form.

## JS client

```js
const quotes = await uTProSimpleCart.shippingQuotes({ countryCode: 'VN' });
```
