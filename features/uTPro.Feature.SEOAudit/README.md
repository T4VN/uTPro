---
layout: default
title: "uTPro.Feature.SEOAudit"
description: "Site-wide SEO & content audit for Umbraco – crawls every URL, checks for broken links/images, produces composite SEO scores, health scores, prioritised issues, and CSV export."
permalink: "/uTPro.Feature.SEOAudit/"
feature: true
feature_name: "SEO Audit"
---

# uTPro SEO Audit for Umbraco

Site-wide SEO & content audit for **Umbraco 16, 17 and 18**. Crawls every Content and Media URL, checks for broken links/images/resources, and audits each page — producing a **composite SEO score**, a **health score**, prioritised **issues**, per-page details and **CSV export**, all in a dedicated backoffice section.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.SEOAudit.svg)](https://www.nuget.org/packages/uTPro.Feature.SEOAudit)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.SEOAudit.svg)](https://www.nuget.org/packages/uTPro.Feature.SEOAudit)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.seoaudit)

![uTPro SEO Audit](/screenshots/uTPro.Feature.SEOAudit/SiteScan.png)

---

## What it adds

| Where | What |
|---|---|
| **Site Audit** dashboard | Full site crawl, health score, SEO score, issues, per-page detail modal, CSV export |
| **Error URLs** dashboard | Standing list of failing URLs with single or bulk re-scan |
| Content/Media editors | **SEO Audit** tab per node (auto-scans on open, warns on issues) |

---

## Architecture

```
T4VN.Seo.Core  (engine, no Umbraco)
      ▲                  ▲
      │                  │
uTPro.Feature.UrlViewer  uTPro.Feature.SEOAudit
(URL Viewer tab +        (this package — site crawler
 shared UI + section)     + node audit tab)
```

---

## Site Audit

- **Broken links, images & resources** — checked for valid status codes
- **Health score** per run + **Overview** of totals
- **Composite SEO score** per page and **Avg SEO Score** across the run
- **Issues** grouped by category with severity, type and priority
- **Orphaned pages** and **duplicate content** detection
- **Incremental scans** — unchanged pages skipped
- **Respects `robots.txt`** and supports **include/exclude URL patterns**
- **Core Web Vitals** (optional) via Google PageSpeed Insights API
- **CSV export** of any run
- **Extensible checks** — implement `IUrlScanIssue`

---

## Per-page audit

- **SEO** — title, meta description, canonical, H1/H2/H3, noindex/nofollow, lang
- **Social** — Open Graph, Twitter Card, pixel detection
- **Technical** — charset, gzip/br, caching, HTTPS, schema.org, viewport, favicon
- **Content** — word count, readability, keyword density, thin-content
- **Accessibility** — aria counts, skip-to-content, heading structure
- **Carbon** — CO₂e estimate with A–F rating
- **Core Web Vitals** (optional) — LCP, CLS, FCP, TBT, Speed Index

---

## Installation

```bash
dotnet add package uTPro.Feature.SEOAudit
```

---

## Configuration

Under `uTPro:Feature:SEOAudit` in `appsettings.json`:

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `true` | Master switch |
| `Period` | `24:00:00` | Audit frequency |
| `MaxConcurrency` | `4` | Concurrent fetches (1–20) |
| `UseIncrementalScan` | `true` | Skip unchanged pages |
| `CollectCoreWebVitals` | `false` | PageSpeed Insights (requires API key) |
| `CheckLinks` | `true` | Check link status codes |
| `CheckExternalLinks` | `true` | Also check external links |

---

## Extensibility

```csharp
services.AddScoped<IUrlScanIssue, MyCustomIssue>();
```

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](https://github.com/T4VN/uTPro.Feature.SEOAudit/blob/main/LICENSE.txt).

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.SEOAudit) · [GitHub](https://github.com/T4VN/uTPro.Feature.SEOAudit) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.seoaudit)
