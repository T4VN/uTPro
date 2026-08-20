---
layout: default
title: "How It Works – URL Viewer"
description: "How uTPro URL Viewer fetches URLs, detects redirect chains, scores SEO, identifies cloaking, and protects against SSRF."
permalink: "/uTPro.Feature.UrlViewer/how-it-works/"
feature: true
feature_name: "URL Viewer"
---

# How It Works

[← Back to URL Viewer](/uTPro.Feature.UrlViewer/)

---

## Fetch pipeline

When you enter a URL in the backoffice and click Fetch:

1. **SSRF guard** — the target is resolved to an IP and checked against private/local ranges. Rejected if internal (unless `AllowInternalHosts` is enabled).
2. **Request** — an HTTP request is made with the chosen user agent (Googlebot, Bingbot, Chrome, or custom) and referrer.
3. **Redirect following** — each hop is recorded (status code, Location header, response headers). The SSRF guard re-checks every hop.
4. **Response capture** — final response status, headers and HTML body are stored.
5. **Analysis** — the HTML is parsed and scored (see below).

---

## Redirect chain detection

The viewer records every 3xx redirect as a hop:

```
https://example.com (301)
 → https://www.example.com (301)
   → https://www.example.com/ (200)
```

Each hop shows:
- HTTP status code (301, 302, 307, 308)
- Full response headers
- Target URL

A chain exceeding the configurable `RedirectWarningThreshold` (default 3 hops, configurable via SEO Audit) is flagged as a potential issue.

---

## Composite SEO score

The score (0–100) is computed by the shared **T4VN.Seo.Core** engine — the same engine used by [SEO Audit](/uTPro.Feature.SEOAudit/) for site-wide crawls, so scores are always consistent.

**Categories and checks:**

| Category | Example checks |
|----------|---------------|
| **SEO** | Title present + length, meta description + length, canonical URL, H1/H2/H3 hierarchy, `noindex`/`nofollow`, `lang` attribute |
| **Technical** | Charset, compression (gzip/br), browser caching headers, HTTPS, schema.org markup, viewport, favicon |
| **Content** | Word count, paragraph count, Flesch readability, keyword density, thin-content detection |
| **Accessibility** | ARIA landmarks, skip-to-content link, heading hierarchy, `lang` attribute, inputs without labels |
| **Social** | Open Graph tags, Twitter Card, social profile links |
| **Carbon** | Page weight → CO₂e estimate with A–F rating |

Each check produces a pass/fail/warning result with actionable remediation advice shown in the UI.

---

## Cloaking detection

Cloaking is when a server returns different content to search engines vs. normal browsers. The viewer can detect this by:

1. Fetching the URL as **Googlebot** (bot user agent)
2. Fetching the same URL as **Chrome** (browser user agent)
3. Comparing: title, status code, content size, and significant HTML differences

If a meaningful discrepancy is found, it's flagged as a potential cloaking issue — useful for detecting hacked sites or accidental bot-blocking.

---

## Spam & security analysis

The HTML is scanned for common indicators of compromise:

| Check | What it looks for |
|-------|-------------------|
| Hidden elements | `display:none` / `visibility:hidden` containers with links |
| Hack injection | Pharma spam, casino keywords, Japanese/Chinese keyword stuffing |
| Obfuscation | `eval()`, `document.write`, base64 strings, encoded redirects |
| Pixel tracking | Facebook, Twitter, LinkedIn, Google Analytics/Tag Manager |

A **VirusTotal link** is provided for the fetched domain so you can cross-reference against known threat databases.

---

## User agent options

| Agent | Use case |
|-------|----------|
| **Googlebot** | See what Google's crawler sees (renders, redirects, robots directives) |
| **Bingbot** | Microsoft's crawler perspective |
| **Chrome** | Normal desktop browser |
| **Custom** | Enter any user agent string |

Combined with referrer options (Google SERP, direct, custom), this lets you test conditional redirects and geo/bot-specific behavior.

---

## REST API

The analysis is available programmatically for CI/automation:

```http
POST /umbraco/management/api/v1/utpro/url-viewer/fetch
Content-Type: application/json

{
  "url": "https://example.com",
  "userAgent": "googlebot",
  "referrer": ""
}
```

Response includes the full redirect chain, response headers, HTML source, `analysis.audit` object and `seoScore`. Requires backoffice authentication with Settings-section access.

