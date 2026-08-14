---
layout: default
title: "uTPro.Feature.SearchPlus"
description: "Enhance Umbraco site search with synonym expansion and diacritics-insensitive matching – managed from the backoffice."
permalink: "/uTPro.Feature.SearchPlus/"
feature: true
feature_name: "Search Plus"
---

# uTPro Search Plus for Umbraco

Enhance site search with **synonym expansion** and **diacritics-insensitive matching** — managed directly from the Umbraco backoffice.

Works with **Umbraco 17**. Database support: **SQL Server**, **SQLite** and **PostgreSQL**.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.SearchPlus.svg)](https://www.nuget.org/packages/uTPro.Feature.SearchPlus)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.SearchPlus.svg)](https://www.nuget.org/packages/uTPro.Feature.SearchPlus)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.searchplus)

![uTPro Search Plus](/screenshots/search-plus-backoffice.png)

---

## Features

- **Synonym groups** — define sets of equivalent terms
- **Diacritics-insensitive indexing** — ASCII-folding analyzer
- **Backoffice management UI** — create, edit, delete and test
- **Pre-loaded defaults** — 25 common synonym groups
- **Automatic integration** — works with Umbraco's Examine search
- **REST API** — full CRUD + expansion test
- **Database storage** — multi-instance / load-balanced support

---

## Quick Start

```bash
dotnet add package uTPro.Feature.SearchPlus
```

After installing, **Rebuild the External Index** from Examine Management.

---

## How it works

```
User searches "laptop"
  → SearchPlus expands: ["máy tính", "laptop", "pc", "computer", "notebook"]
  → Examine queries all terms (OR)
```

---

## Documentation

| Guide | Description |
|---|---|
| [Getting Started](getting-started/) | Install, backoffice location, first-time setup |
| [How It Works](how-it-works/) | Diacritics folding, synonym expansion flow |
| [Synonym Management](synonym-management/) | Creating and managing groups |
| [Diacritics Support](diacritics/) | ASCII-folding analyzer, supported languages |
| [Integration](integration/) | Using ISynonymProvider in custom code |
| [API Reference](api-reference/) | Full REST API documentation |

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](https://github.com/T4VN/uTPro.Feature.SearchPlus/blob/main/LICENSE.txt).

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.SearchPlus) · [GitHub](https://github.com/T4VN/uTPro.Feature.SearchPlus) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.searchplus)
