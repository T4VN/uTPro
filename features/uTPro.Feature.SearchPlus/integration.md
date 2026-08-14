---
layout: default
title: "Integration – Search Plus"
description: "How to integrate Search Plus synonym expansion into your Umbraco site's search using ISynonymProvider."
permalink: "/uTPro.Feature.SearchPlus/integration/"
feature: true
feature_name: "Search Plus"
---

[← Back to Search Plus](/uTPro.Feature.SearchPlus/)

# Integration

## Overview

Search Plus provides `ISynonymProvider` — inject it into your controllers or views to expand search queries with synonyms before passing them to Umbraco's Examine.

## Controller example

```csharp
using Examine;
using Examine.Search;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using uTPro.Feature.SearchPlus.Services;

public class SearchController : Controller
{
    private readonly IExamineManager _examineManager;
    private readonly ISynonymProvider _synonyms;

    public SearchController(IExamineManager examineManager, ISynonymProvider synonyms)
    {
        _examineManager = examineManager;
        _synonyms = synonyms;
    }

    [HttpGet("/search")]
    public IActionResult Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new { results = Array.Empty<object>() });

        if (!_examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out var index))
            return StatusCode(500, "Index not available");

        var searcher = index.Searcher;

        // 1. Expand synonyms
        var terms = _synonyms.Expand(q.Trim());

        // 2. Build OR query
        var query = searcher.CreateQuery("content");
        var boolOp = query.ManagedQuery(terms[0]);
        for (var i = 1; i < terms.Count; i++)
        {
            boolOp = boolOp.Or().ManagedQuery(terms[i]);
        }

        // 3. Execute
        var results = boolOp.Execute(QueryOptions.SkipTake(0, 20));

        return Ok(new
        {
            total = results.TotalItemCount,
            results = results.Select(r => new { r.Id, r.Score })
        });
    }
}
```

## Razor view example

```cshtml
@using Examine
@using Examine.Search
@using Umbraco.Cms.Core
@using uTPro.Feature.SearchPlus.Services
@inject ISynonymProvider SynonymProvider
@inject IExamineManager ExamineManager

@{
    var q = Context.Request.Query["q"].FirstOrDefault() ?? "";
    var terms = SynonymProvider.Expand(q);

    if (ExamineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out var index))
    {
        var query = index.Searcher.CreateQuery("content");
        var boolOp = query.ManagedQuery(terms[0]);
        for (var i = 1; i < terms.Count; i++)
        {
            boolOp = boolOp.Or().ManagedQuery(terms[i]);
        }
        var results = boolOp.Execute();
        // render results...
    }
}
```

## Optional injection (graceful degradation)

If you want your search to work regardless of whether Search Plus is installed:

```csharp
public class SearchService
{
    private readonly IExamineManager _examineManager;
    private readonly ISynonymProvider? _synonyms; // nullable

    public SearchService(IExamineManager examineManager, ISynonymProvider? synonyms = null)
    {
        _examineManager = examineManager;
        _synonyms = synonyms;
    }

    public ISearchResults Search(string q)
    {
        // Expand if available, otherwise use original term
        var terms = _synonyms?.Expand(q) ?? new[] { q };

        // ... build and execute query
    }
}
```

## How expansion works

1. `Expand(term)` first tries an exact match (case-insensitive)
2. If not found, it folds diacritics and tries again ("cong ty" → matches "công ty" group)
3. Returns all terms in the matched group, or just the original term if no match

## Notes

- Synonym expansion is applied at **query time only** — no index rebuild needed when synonyms change
- The diacritics analyzer is applied at **index time** — rebuild once after first install
- `ISynonymProvider` is registered as `Scoped` in DI
