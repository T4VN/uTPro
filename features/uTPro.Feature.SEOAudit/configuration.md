---
layout: default
title: "Configuration – SEO Audit"
description: "All configuration options for uTPro SEO Audit: scheduling, concurrency, link checking, Core Web Vitals, and URL patterns."
permalink: "/uTPro.Feature.SEOAudit/configuration/"
feature: true
feature_name: "SEO Audit"
---

# Configuration

[← Back to SEO Audit](/uTPro.Feature.SEOAudit/)

Bind under `uTPro:Feature:SEOAudit` in `appsettings.json`. All keys are optional — defaults shown:

```json
{
  "uTPro": {
    "Feature": {
      "SEOAudit": {
        "Enabled": true,
        "Period": "24:00:00",
        "Delay": "00:05:00",
        "MaxConcurrency": 4,
        "ThrottleDelayMs": 150,
        "SkipCloakingCheck": true,
        "AllowInternalHosts": false,
        "AllowInvalidCertificates": false,
        "RedirectWarningThreshold": 3,
        "MaxRunHistory": 20,
        "SkipNodesWithoutTemplate": true,
        "CheckLinks": true,
        "CheckExternalLinks": true,
        "CheckImages": true,
        "LinkCheckTimeoutSeconds": 15,
        "MaxLinkChecksPerRun": 5000,
        "UseIncrementalScan": true,
        "UseSitemapDiscovery": true,
        "RespectRobotsTxt": true,
        "ExcludePatterns": [],
        "IncludePatterns": [],
        "CollectCoreWebVitals": false,
        "PageSpeedApiKey": "",
        "PageSpeedStrategy": "mobile"
      }
    }
  }
}
```

---

## Reference

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `true` | Master switch for the recurring audit job |
| `Period` | `24:00:00` | How often the audit runs |
| `Delay` | `00:05:00` | Delay before the first run after startup |
| `MaxConcurrency` | `4` | Max concurrent HTTP fetches (clamped 1–20) |
| `ThrottleDelayMs` | `150` | Delay after each fetch (ms) |
| `SkipCloakingCheck` | `true` | Skip bot-vs-Chrome comparison during bulk audits (faster) |
| `AllowInternalHosts` | `false` | Relax SSRF guard for internal/dev sites |
| `AllowInvalidCertificates` | `false` | Accept self-signed / invalid TLS (dev only) |
| `RedirectWarningThreshold` | `3` | Hop count above which a redirect chain is flagged |
| `MaxRunHistory` | `20` | Runs retained before old runs are pruned |
| `SkipNodesWithoutTemplate` | `true` | Skip content nodes with no template |
| `CheckLinks` | `true` | Check link status codes |
| `CheckExternalLinks` | `true` | Also check external links |
| `CheckImages` | `true` | Check image URLs |
| `LinkCheckTimeoutSeconds` | `15` | Per-request timeout (clamped 1–60) |
| `MaxLinkChecksPerRun` | `5000` | Safety cap on distinct URLs checked per run |
| `UseIncrementalScan` | `true` | Skip unchanged pages on scheduled runs |
| `UseSitemapDiscovery` | `true` | Discover URLs from `sitemap.xml` |
| `RespectRobotsTxt` | `true` | Honour `robots.txt` disallow rules |
| `ExcludePatterns` | `[]` | URL patterns to skip (wildcards `*` / `**`) |
| `IncludePatterns` | `[]` | If set, only matching URLs are scanned |
| `CollectCoreWebVitals` | `false` | Collect via PageSpeed Insights (requires API key) |
| `PageSpeedApiKey` | `""` | Google PageSpeed Insights API key |
| `PageSpeedStrategy` | `mobile` | `mobile` or `desktop` |
