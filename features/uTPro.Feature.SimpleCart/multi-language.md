---
layout: default
title: "Multi-language – Simple Cart"
description: "How uTPro Simple Cart renders product names and prices in the visitor's culture using Umbraco culture variants and dictionary items."
permalink: "/uTPro.Feature.SimpleCart/multi-language/"
feature: true
feature_name: "Simple Cart"
---

# Multi-language

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

Localization layers onto Umbraco's built-in multilingual features — there is no bespoke i18n engine to learn. Because the cart stores only the product **key** and quantity (never a frozen name or price), it always renders in the visitor's current culture.

---

## How it fits together

![Product with culture variants](/screenshots/uTPro.Feature.SimpleCart/culture-variants.png)

1. **Product content** — make the `productName`, `price` and `description` properties **culture-variant** on the Product document type, and translate them per language. See [Product Catalog](catalog/).
2. **The cart** — resolves name and price from product content honouring the ambient culture, so a `vi-VN` request yields the Vietnamese name automatically, an `en-US` request the English one.
3. **Storefront UI strings** — use Umbraco Dictionary items for your own labels (buttons, headings), e.g. `@Umbraco.GetDictionaryValue("Cart.AddToCart")`.

Standard Umbraco culture routing (domains or path prefixes) decides which culture a request is in — the cart simply follows it.

---

## Language ≠ currency

Language and currency are deliberately kept separate. Product prices are stored as plain numeric values; the store currency is configured globally (`uTPro:SimpleCart:Currency`). This means an `en-NL` shopper can read English but still pay in the store's configured currency (e.g. EUR). Multi-currency (market-specific pricing) is on the roadmap as a future enhancement.

> **Live vs. frozen:** the cart resolves names and prices **live** on every read. At checkout, the order **freezes a snapshot** — product names, prices and line totals are stored permanently so historical orders always reflect the price the customer actually paid, regardless of future edits to the product content.
