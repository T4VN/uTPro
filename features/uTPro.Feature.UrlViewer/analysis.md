---
layout: default
title: "SEO Analysis – URL Viewer"
description: "What the URL Viewer analysis includes: redirect chains, SEO score, social tags, technical SEO, content metrics, and more."
permalink: "/uTPro.Feature.UrlViewer/analysis/"
feature: true
feature_name: "URL Viewer"
---

# SEO Analysis

[← Back to URL Viewer](/uTPro.Feature.UrlViewer/)

Enter any URL, pick the scheme (HTTP/HTTPS), user agent and referrer, then fetch. You get a complete analysis:

---

## Redirect Chain

Full redirect chain with every hop showing status code and raw response headers.

## HTML Source Viewer

Line numbers, word-wrap toggle, copy-to-clipboard, spam-word highlighting.

## Composite SEO Score (0–100)

Lighthouse-style checklist with per-category sub-scores:
- **SEO** — title, meta description, canonical, H1/H2/H3, `noindex`/`nofollow`, `lang`
- **Technical** — charset, gzip/br, browser caching, HTTPS, schema.org, viewport, favicon
- **Content** — word/paragraph count, Flesch readability, keyword density, thin-content flag
- **Accessibility** — aria counts, skip-to-content, heading structure, inputs without label

Each check shows pass/fail + remediation advice.

## Social Tags

Open Graph and Twitter Card metadata, social-profile links, pixel detection (Facebook, Twitter, LinkedIn, Google Analytics).

## Carbon Estimate

CO₂e per page view with an A–F rating.

## Spam & Security Analysis

- Common hack injection patterns, hidden elements
- `eval` / `document.write` / base64 obfuscation
- **Cloaking detection** — compares bot vs Chrome response (title, status code, content-size difference)
- **VirusTotal link** for the fetched domain

## REST API

Every fetch is also available as authenticated JSON:

```http
POST /umbraco/management/api/v1/utpro/url-viewer/fetch
```

Useful for CI pipelines or external tooling. The response includes the full `analysis.audit` object and the `seoScore`.
