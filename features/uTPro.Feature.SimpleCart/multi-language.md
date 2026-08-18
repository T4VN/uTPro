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

Language and currency are deliberately kept separate. Product prices are stored as plain numeric values; formatting them into a currency (and any currency conversion or market-specific pricing) is a **market** concern on the roadmap. That separation means an `en-NL` shopper can read English but still pay in EUR.

> **Live vs. frozen:** the cart resolves names and prices **live** on every read. Order capture (roadmap) will instead **freeze a snapshot** at purchase time so historical orders keep the price the customer actually paid.
