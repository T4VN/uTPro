---
layout: default
title: "Getting Started – Search Plus"
description: "Install and configure Search Plus for Umbraco with diacritics-insensitive search and synonym expansion."
permalink: "/uTPro.Feature.SearchPlus/getting-started/"
feature: true
feature_name: "Search Plus"
---

[← Back to Search Plus](/uTPro.Feature.SearchPlus/)

# Getting Started

## Installation

```bash
dotnet add package uTPro.Feature.SearchPlus
```

Or via the NuGet Package Manager in Visual Studio, search for `uTPro.Feature.SearchPlus`.

## Requirements

- Umbraco 17+
- .NET 10
- Any supported database: SQL Server, SQLite, or PostgreSQL

## What happens on first startup

After installing and restarting Umbraco, the package automatically:

1. **Creates database tables** — `uTProSynonymGroup` and `uTProSynonymTerm` (migration runs once)
2. **Seeds default data** — 25 common Vietnamese–English synonym groups are inserted
3. **Configures the Examine index** — the ExternalIndex analyzer is replaced with one that folds diacritics to ASCII
4. **Registers the backoffice UI** — a "Search Plus" workspace appears under **Settings → uTPro Feature**

## First-time setup

After the first startup completes:

1. Go to **Settings → Examine Management** in the Umbraco backoffice
2. Find the **External** index
3. Click **Rebuild** — this re-processes all existing content with the new diacritics-insensitive analyzer

This step is only needed once. Future content publishes are automatically indexed with the new analyzer.

## Backoffice location

Navigate to **Settings** in the left sidebar. Scroll down to the **uTPro Feature** group and click **Search Plus**.

## Verifying it works

1. In the Search Plus workspace, type "laptop" in the search bar
2. You should see it expand to: máy tính, laptop, pc, computer, notebook
3. Type "cong ty" (without accents) — it should match the "công ty" group

## Next steps

- [Synonym Management](../synonym-management/) — creating and managing synonym groups
- [How It Works](../how-it-works/) — understand the indexing and query flow
- [Integration](../integration/) — connecting to your site's search
