---
layout: default
title: "How It Works – Search Plus"
description: "Understand the diacritics-insensitive indexing and synonym expansion mechanisms in Search Plus."
permalink: "/uTPro.Feature.SearchPlus/how-it-works/"
feature: true
feature_name: "Search Plus"
---

[← Back to Search Plus](/uTPro.Feature.SearchPlus/)

# How It Works

This page explains the two core mechanisms that Search Plus adds to Umbraco's search pipeline.

---

## 1. Diacritics-Insensitive Indexing

When content is published, Umbraco's Examine sends text to a Lucene index. Search Plus replaces
the default analyzer on the ExternalIndex with one that includes an **ASCII folding** step.

### Index time (when content is published/rebuilt)

```
Content: "Tham khảo cho Lập trình viên"
         │
         ▼
    Tokenizer  →  ["Tham", "khảo", "cho", "Lập", "trình", "viên"]
         │
         ▼
    Lowercase   →  ["tham", "khảo", "cho", "lập", "trình", "viên"]
         │
         ▼
    ASCII Fold  →  ["tham", "khao", "cho", "lap", "trinh", "vien"]
         │
         ▼
    Stored in Lucene index (on disk)
```

### Query time (when user searches)

The **same analyzer** processes the search input:

```
User types: "tham khao"
         │
         ▼
    Tokenizer  →  ["tham", "khao"]
         │
         ▼
    Lowercase   →  ["tham", "khao"]
         │
         ▼
    ASCII Fold  →  ["tham", "khao"]  (already ASCII, no change)
         │
         ▼
    Match against index → ✓ finds "Tham khảo cho Lập trình viên"
```

Because both sides (index and query) fold to ASCII, the matching is diacritics-insensitive.

### Supported languages

Any language that uses Latin script with accents/diacritics:

| Language | Example | Folded |
|---|---|---|
| Vietnamese | công ty, khảo | cong ty, khao |
| French | café, naïve | cafe, naive |
| Spanish | niño, señor | nino, senor |
| German | über, straße | uber, strasse |
| Portuguese | ação, coração | acao, coracao |
| Turkish | şehir, güneş | sehir, gunes |
| Polish | źródło, łódź | zrodlo, lodz |
| Czech | příliš, žluťoučký | prilis, zlutoucky |

---

## 2. Synonym Expansion

Synonyms are stored in the Umbraco database and loaded into an in-memory cache on startup.
They are **not part of the search index** — they are applied at query time.

### Query time flow

```
User searches: "laptop"
         │
         ▼
    ① Synonym lookup (from DB cache)
       "laptop" → found in group: ["máy tính", "laptop", "pc", "computer", "notebook"]
         │
         ▼
    ② Build expanded query (OR logic)
       Search("máy tính") OR Search("laptop") OR Search("pc")
       OR Search("computer") OR Search("notebook")
         │
         ▼
    ③ Each term goes through the same ASCII-folding analyzer
       "máy tính" → tokens: ["may", "tinh"]
       "laptop"   → tokens: ["laptop"]
       "pc"       → tokens: ["pc"]
       ...
         │
         ▼
    ④ Lucene matches all tokens against the index
         │
         ▼
    ⑤ Results merged, scored by relevance, deduplicated
```

### Diacritics-insensitive synonym lookup

The synonym lookup itself is also diacritics-insensitive:

```
User types: "cong ty" (no accents)
         │
         ▼
    Lookup "cong ty" → not found (exact)
         │
         ▼
    Fold and retry → "cong ty" matches cached key for "công ty"
         │
         ▼
    Returns: ["công ty", "company", "doanh nghiệp", "enterprise", "business"]
```

---

## Key Implications

| Aspect | Behavior |
|---|---|
| **Add/edit/delete synonyms** | Takes effect immediately — no index rebuild needed |
| **First install (diacritics)** | Requires one index rebuild so existing content is re-processed with the new analyzer |
| **Uninstall** | Index reverts to default analyzer on next rebuild; synonym expansion stops |
| **Multi-instance** | Synonyms are in the shared database — all nodes see the same data |
| **Performance** | Synonym lookup is O(1) dictionary lookup from memory; no DB query per search |
