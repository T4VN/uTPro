---
layout: default
title: "Site Audit – SEO Audit"
description: "How the site-wide crawler works: broken links, health scores, SEO scores, issues, incremental scans, and CSV export."
permalink: "/uTPro.Feature.SEOAudit/site-audit/"
feature: true
feature_name: "SEO Audit"
---

# Site Audit

[← Back to SEO Audit](/uTPro.Feature.SEOAudit/)

A recurring background job crawls every **Content** and **Media** URL and, optionally, URLs in `sitemap.xml`.

---

## What it checks

- **Broken links, images & resources** — `<a>`, `<img>`, stylesheets, scripts, fonts checked for a valid status code
- **Health score** per run + **Overview** of totals (pages, links, images, resources, broken, orphaned, duplicate)
- **Composite SEO score** per page and **Avg SEO Score** across the run
- **Issues** grouped by category with severity, type and priority
- **Orphaned pages** (no internal link) and **duplicate content** (identical HTML hash) detection
- **Incremental scans** — unchanged pages (ETag / Last-Modified) are skipped; previous result is carried forward
- **Respects `robots.txt`** (disallow rules + `Sitemap:` discovery); supports **include/exclude URL patterns**
- **Core Web Vitals** (optional) via the Google PageSpeed Insights API — no local browser needed
- **CSV export** of any run
- **Extensible checks** — implement `IUrlScanIssue` and register in DI

---

## Built-in issue checks

HTTP errors, server errors, broken links, broken resources, missing/too-long meta description, missing H1, missing H2, `nofollow`, `noindex`, canonicalised URLs, orphaned pages, thin content, duplicate content, images missing alt text, high carbon intensity, spam/hack keywords, cloaking, JavaScript errors, poor Core Web Vitals.

---

## Per-page detail (modal)

Click any page in the audit results to see:

- **SEO** — title (+ length), meta description (+ length), canonical, H1/H2/H3, `noindex`, `nofollow`, `lang`
- **Social** — Open Graph, Twitter Card, pixel detection, social-profile links
- **Technical SEO** — charset, gzip/br, browser caching, HTTPS, schema.org, viewport, favicon
- **Content** — word/paragraph count, Flesch readability, keyword density, thin-content flag
- **Accessibility** — aria counts, skip-to-content, heading structure, `lang`, inputs without label
- **Carbon** — CO₂e estimate with A–F rating
- **Core Web Vitals** (optional) — Lighthouse score, LCP, CLS, FCP, TBT, Speed Index + real-user field data
