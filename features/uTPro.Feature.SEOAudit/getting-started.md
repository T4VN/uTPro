---
layout: default
title: "Getting Started – SEO Audit"
description: "Install uTPro SEO Audit, understand the architecture, and run your first site-wide crawl."
permalink: "/uTPro.Feature.SEOAudit/getting-started/"
feature: true
feature_name: "SEO Audit"
---

# Getting Started

[← Back to SEO Audit](/uTPro.Feature.SEOAudit/)

## Installation

```bash
dotnet add package uTPro.Feature.SEOAudit
```

> **Tip:** install `uTPro.Feature.UrlViewer` as well to get the manual URL Viewer fetch tool in the same section.

## Requirements

- Umbraco 16, 17 or 18
- .NET 9 / .NET 10

## What happens on install

After installing and restarting Umbraco:

1. The package creates a **SEO Audit** section in the backoffice (or extends it if `uTPro.Feature.UrlViewer` already created it)
2. A recurring background job is registered (auto-discovered by **uTPro Job Monitor** if installed)
3. The first audit runs after the configured delay (default 5 minutes)

## Architecture

```
T4VN.Seo.Core  (engine, no Umbraco)
      ▲                  ▲
      │                  │
uTPro.Feature.UrlViewer  uTPro.Feature.SEOAudit
(URL Viewer tab +        (this package — site crawler
 shared UI + section)     + node audit tab)
```

Both packages share the same **T4VN.Seo.Core** engine and `SeoScorer` — the single source of truth for the SEO score. Install order and combination are flexible; each package works independently.

## Where it lives in the backoffice

- **Site Audit** dashboard — full site crawl results
- **Error URLs** dashboard — standing list of failing URLs
- **Content/Media editors** — SEO Audit tab per node

## Security

Backoffice-secured under `/umbraco/management/api/v1/utpro/url-scan`, requires Settings-section access. Crawl fetches run behind an SSRF guard (RFC-1918 / localhost / `.local` blocked, re-checked on every redirect hop).
