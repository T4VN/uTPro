---
layout: default
title: "uTPro.Feature.SEOAudit"
description: "Site-wide SEO & content audit for Umbraco – crawls every URL, checks for broken links/images, produces composite SEO scores, health scores, prioritised issues, and CSV export."
permalink: "/uTPro.Feature.SEOAudit/"
feature: true
feature_name: "SEO Audit"
---

# uTPro SEO Audit for Umbraco

> For: **Both** — Content Editors run audits & view results; Developers configure scheduling and write custom checks.

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

## Configuration

See [Configuration](configuration/) for all `appsettings.json` options.

---

## Documentation

| Guide | Description |
|---|---|
| [Getting Started](getting-started/) | Install, architecture, backoffice location, security |
| [Site Audit](site-audit/) | How the crawler works, checks, per-page detail, CSV export |
| [Configuration](configuration/) | Scheduling, concurrency, links, Core Web Vitals, URL patterns |
| [Extensibility](extensibility/) | Custom issue checks with IUrlScanIssue, per-node tab |

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](https://github.com/T4VN/uTPro.Feature.SEOAudit/blob/main/LICENSE.txt).

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.SEOAudit) · [GitHub](https://github.com/T4VN/uTPro.Feature.SEOAudit) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.seoaudit)
