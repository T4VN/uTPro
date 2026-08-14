---
layout: default
title: "uTPro.Feature.UrlViewer"
description: "Fetch any URL from the Umbraco backoffice – see redirect chains, response headers, HTML source, and a full SEO analysis with composite score."
permalink: "/uTPro.Feature.UrlViewer/"
feature: true
feature_name: "URL Viewer"
---

# uTPro URL Viewer for Umbraco

Fetch any URL from inside the Umbraco backoffice and see **exactly** what a search engine or browser sees — redirect chain, response headers, HTML source, and a full static analysis including a **composite SEO score**.

Supports **Umbraco 16, 17 and 18**.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.UrlViewer.svg)](https://www.nuget.org/packages/uTPro.Feature.UrlViewer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.UrlViewer.svg)](https://www.nuget.org/packages/uTPro.Feature.UrlViewer)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.urlviewer)

---

## What it does

- **Full redirect chain** — every hop with status code and raw headers
- **HTML source viewer** — line numbers, word-wrap, copy-to-clipboard
- **Composite SEO score (0–100)** — per-category sub-scores
- **On-page SEO analysis** — title, meta description, canonical, H1/H2/H3
- **Social tags** — Open Graph, Twitter Card, pixel detection
- **Technical SEO** — charset, gzip/br, caching, HTTPS, schema.org
- **Content metrics** — word count, readability, keyword density
- **Carbon estimate** — CO₂e per page view with A–F rating
- **Cloaking detection** — bot vs Chrome comparison
- **VirusTotal link** for domain check

---

## Architecture

```
T4VN.Seo.Core  (engine, no Umbraco)
      ▲                  ▲
      │                  │
uTPro.Feature.UrlViewer  uTPro.Feature.SEOAudit
(this package)           (site-wide crawler)
```

---

## Installation

```bash
dotnet add package uTPro.Feature.UrlViewer
```

---

## Configuration

| Key | Default | Description |
|-----|---------|-------------|
| `AllowInternalHosts` | `false` | Relax SSRF guard for dev sites |
| `AllowInvalidCertificates` | `false` | Accept self-signed TLS (dev only) |

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](https://github.com/T4VN/uTPro.Feature.UrlViewer/blob/main/LICENSE.txt).

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.UrlViewer) · [GitHub](https://github.com/T4VN/uTPro.Feature.UrlViewer) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.urlviewer)
