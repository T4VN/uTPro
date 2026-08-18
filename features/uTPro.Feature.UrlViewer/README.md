---
layout: default
title: "uTPro.Feature.UrlViewer"
description: "Fetch any URL from the Umbraco backoffice – see redirect chains, response headers, HTML source, and a full SEO analysis with composite score."
permalink: "/uTPro.Feature.UrlViewer/"
feature: true
feature_name: "URL Viewer"
feature_order: 2
feature_tagline: "Fetch URLs, redirect chains & SEO score analysis"
---

# uTPro URL Viewer for Umbraco

> For: **Both** — Content Editors check SEO & redirects; Developers configure SSRF guard and use the API.

Fetch any URL from inside the Umbraco backoffice and see **exactly** what a search engine or browser sees — redirect chain, response headers, HTML source, and a full static analysis including a **composite SEO score**.

Supports **Umbraco 16, 17 and 18**.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.UrlViewer.svg)](https://www.nuget.org/packages/uTPro.Feature.UrlViewer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.UrlViewer.svg)](https://www.nuget.org/packages/uTPro.Feature.UrlViewer)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.urlviewer)

![uTPro URL Viewer](/screenshots/uTPro.Feature.UrlViewer/3.0.0/ScanUrl.png)

---

## What it does

Fetch any URL from the backoffice and see redirect chain, headers, HTML source, and a complete SEO analysis with composite score.

---

## Quick Start

```bash
dotnet add package uTPro.Feature.UrlViewer
```

---

## Configuration

See [Configuration](configuration/) for SSRF guard options.

---

## Documentation

| Guide | Description |
|---|---|
| [Getting Started](getting-started/) | Install, architecture, security |
| [SEO Analysis](analysis/) | Redirect chain, score breakdown, social, technical, carbon, API |
| [Configuration](configuration/) | SSRF guard relaxation for internal/dev hosts |

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](https://github.com/T4VN/uTPro.Feature.UrlViewer/blob/main/LICENSE.txt).

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.UrlViewer) · [GitHub](https://github.com/T4VN/uTPro.Feature.UrlViewer) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.urlviewer)
