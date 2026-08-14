---
layout: default
title: "Getting Started – URL Viewer"
description: "Install uTPro URL Viewer, understand the architecture, and start fetching URLs from the Umbraco backoffice."
permalink: "/uTPro.Feature.UrlViewer/getting-started/"
feature: true
feature_name: "URL Viewer"
---

# Getting Started

[← Back to URL Viewer](/uTPro.Feature.UrlViewer/)

## Installation

```bash
dotnet add package uTPro.Feature.UrlViewer
```

## Requirements

- Umbraco 16, 17 or 18
- .NET 9 / .NET 10

## Where it lives

After install, a **URL Viewer** section appears in the backoffice navigation. Grant the section to user groups under **Users → User groups → Sections**.

## Architecture

```
T4VN.Seo.Core  (engine, no Umbraco)
      ▲                  ▲
      │                  │
uTPro.Feature.UrlViewer  uTPro.Feature.SEOAudit
(this package)           (site-wide crawler — optional)
```

Built on the framework-agnostic **T4VN.Seo.Core** engine which provides the fetch pipeline, all analysis, and `SeoScorer`. The companion site crawler (`uTPro.Feature.SEOAudit`) shares the same engine, so scores are identical.

## Security

All API endpoints are backoffice-secured under `/umbraco/management/api/v1/utpro/...` and require Settings-section access. Server-side fetches run behind an **SSRF guard** that blocks private/local addresses and re-checks on every redirect hop.

## Configuration

No configuration required. For internal/dev hosts:

| Key | Default | Description |
|-----|---------|-------------|
| `AllowInternalHosts` | `false` | Relax SSRF guard for internal/dev sites |
| `AllowInvalidCertificates` | `false` | Accept self-signed TLS (dev only) |

```json
{
  "uTPro": {
    "Feature": {
      "UrlViewer": {
        "AllowInternalHosts": false,
        "AllowInvalidCertificates": false
      }
    }
  }
}
```
