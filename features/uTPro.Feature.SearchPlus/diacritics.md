---
layout: default
title: "Diacritics Support – Search Plus"
description: "How Search Plus enables diacritics-insensitive search for Vietnamese, French, Spanish, and other Latin-script languages."
permalink: "/uTPro.Feature.SearchPlus/diacritics/"
feature: true
feature_name: "Search Plus"
---

[← Back to Search Plus](/uTPro.Feature.SearchPlus/)

# Diacritics Support

## What is diacritics-insensitive search?

Diacritics are accent marks and other modifications on letters: á, ả, ã, ạ, â, ă, é, ê, ô, ơ, ư, ñ, ü, ç, etc.

**Diacritics-insensitive search** means users can type without accents and still find content that contains accented text.

| User types | Finds content containing |
|---|---|
| tham khao | tham khảo |
| cong ty | công ty |
| cafe | café |
| nino | niño |
| uber | über |

## How it works

Search Plus replaces the default Examine ExternalIndex analyzer with one that includes an **ASCII folding** step. This step converts accented characters to their ASCII base equivalents.

The folding happens at **both index time and query time**, so the matching is always consistent.

### Index time

When content is published, text is processed:

```
"Công ty TNHH ABC" → tokens: ["cong", "ty", "tnhh", "abc"]
```

### Query time

When a user searches, the same processing applies:

```
User types: "cong ty" → tokens: ["cong", "ty"] → matches!
User types: "công ty" → tokens: ["cong", "ty"] → also matches!
```

## Supported languages

Any language using Latin script with diacritical marks:

| Language | Characters folded |
|---|---|
| Vietnamese | à, á, ả, ã, ạ, â, ă, è, é, ê, ì, ò, ô, ơ, ù, ư, ỳ, đ → a, e, i, o, u, d |
| French | é, è, ê, ë, ç, à, ù, î, ô, û → e, c, a, u, i, o, u |
| Spanish | ñ, á, é, í, ó, ú, ü → n, a, e, i, o, u, u |
| Portuguese | ã, õ, á, é, ê, ó, ô, ú, ç → a, o, a, e, e, o, o, u, c |
| German | ä, ö, ü, ß → a, o, u, ss |
| Turkish | ş, ğ, ı, ö, ü, ç → s, g, i, o, u, c |
| Polish | ą, ć, ę, ł, ń, ó, ś, ź, ż → a, c, e, l, n, o, s, z, z |
| Czech/Slovak | á, č, ď, é, ě, í, ň, ó, ř, š, ť, ú, ů, ý, ž → a, c, d, e, e, i, n, o, r, s, t, u, u, y, z |
| Romanian | ă, â, î, ș, ț → a, a, i, s, t |
| Hungarian | á, é, í, ó, ö, ő, ú, ü, ű → a, e, i, o, o, o, u, u, u |
| Nordic | å, ä, ö, ø, æ → a, a, o, o, a |

## First-time setup

After installing Search Plus, you need to **rebuild the External Index once** so existing content is re-processed with the new analyzer:

1. Go to **Settings → Examine Management**
2. Find the **External** index
3. Click **Rebuild**

Future content publishes are automatically processed with the new analyzer — no manual action needed.

## Important notes

- The folding is **one-way** — content is stored in folded form in the index, but the original text in the database is unchanged
- Search results still display the original text (with diacritics) because results are resolved from the content cache, not from the index
- If Search Plus is uninstalled, rebuild the index to revert to the default analyzer
