# <img width="50" height="50" alt="Logo" src="/screenshots/logo-utpro.png" /> uTPro – Umbraco Turbo Pro

## For developers, by developers

**uTPro** is a powerful **Starter Kit Template** built to **accelerate website development on the Umbraco platform**.
It enables developers to create **enterprise-grade websites** faster, more reliably, and with a professional structure from day one.

[![Umbraco 17](https://img.shields.io/badge/Umbraco-17.5.3-3544B1)](https://umbraco.com)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> 📖 **Full documentation**: [docs/Home.md](docs/Home.md)

---

## 🚀 Quick Start

1. **Clone** the repository
2. **Configure** database connection in `appsettings.json` ([details](docs/2.-Setup.md))
3. **Build & Run** with `dotnet run`
4. **Import data** via uSync ([details](docs/2.-Setup.md#23-setup-data))
5. **Start building** your site!

---

## 🔑 Core Principles

- **Umbraco Turbo Pro** — Speed up Umbraco development with a streamlined, production-ready foundation.
- **Universal Template Project** — A flexible structure that adapts to enterprise websites, product showcases, landing pages, and more.
- **Ultimate Tech Productivity** — Reduce repetitive setup tasks and let developers focus on delivering value.

---

## ⚙️ What's Included

| Category | Highlights |
|----------|-----------|
| **Database** | PostgreSQL (default), SQL Server, SQLite — switch with two config keys |
| **Performance** | Output Cache, WebOptimizer (CSS/JS minification), WebMarkupMin (HTML + Brotli/GZip), multi-layer caching |
| **SEO** | Open Graph, Twitter Cards, JSON-LD, hreflang, canonical, sitemap.xml, robots.txt |
| **Core Web Vitals** | Critical CSS inlined, LCP preload, fonts preloaded, lazy-loaded images (WebP + srcset) |
| **Security** | Security headers (configurable from backoffice), HSTS, request limits, Data Protection key rotation |
| **Multilingual** | Cookie-based language memory, culture & hostnames, dictionary items |
| **Architecture** | Modular .NET projects (Common / Extension / Foundation / Feature / Project) |
| **Block Grid** | Live preview in backoffice, reusable Top/Bottom components, section colour control |
| **Backoffice tools** | Dashboard, URL Viewer, SEO Audit, File Manager, Job Monitor, Form Builder |

**Extensions**: [uSync](https://marketplace.umbraco.com/package/usync) · [BlockPreview](https://marketplace.umbraco.com/package/umbraco.community.blockpreview) · [SeoVisualizer](https://marketplace.umbraco.com/package/umbracoseovisualizer) · [WebMarkupMin](https://www.nuget.org/packages/WebMarkupMin.AspNetCoreLatest/) · [WebOptimizer](https://www.nuget.org/packages/LigerShark.WebOptimizer.Core)

---

## 📸 Screenshots

### Block Preview — live in the backoffice

Components render exactly as they appear on the frontend, directly in the Block Grid editor.

![Block Preview](/screenshots/readme-block-preview-live.png)

### Shared Components (Top/Bottom for layout)

Set a Top/Bottom component once on a parent and it inherits down the content tree — site-wide headers, CTAs, footers with zero duplication.

![Shared Component](/screenshots/readme-shared-component.png)

### Script Queue — CSS/JS loaded only when a component renders

Each block component registers its own scripts; they're emitted only when that block is on the page.

![Script Queue Code](/screenshots/readme-script-queue-code.png)

![Script Queue Output](/screenshots/readme-script-queue-output.png)

> 📖 See [Script Queue documentation](docs/5.-Script-Queue.md) for dependency-aware loading (jQuery-dependent vs standalone).

### SEO — production-ready from day one

Open Graph, Twitter Card, JSON-LD, hreflang, canonical — all generated automatically from content fields.

![SEO Page Source](/screenshots/readme-seo-page-source.png)

### uTPro Dashboard

Version check, site statistics, audit trail chart, and quick links — all in one backoffice tab.

![Dashboard](/screenshots/readme-dashboard.png)

### Content Editing — Block Grid with components

![Block Grid Editor](/screenshots/content-blockgrid-editor.png)

### SEO Audit — site-wide crawler (optional package)

![SEO Audit](/screenshots/tools-seo-audit.png)

---

## 🏗️ Modular Architecture

```
uTPro (solution)
├── Common          Shared models, constants, CMS-generated content models
├── Extension       Reusable services (site context, culture, URL helpers)
├── Foundation      Infrastructure (middleware, favicon, sitemap, robots.txt)
├── Feature         Optional packages (dashboard, form builder, file manager…)
└── Project         Main web application and configuration
```

---

## 📋 Tech Stack

| Component | Version |
|-----------|---------|
| Umbraco CMS | 17.5.3 |
| .NET | 10.0 |
| Database | PostgreSQL (default) · SQL Server · SQLite |
| uSync | 17.3.6 |
| BlockPreview | 5.4.3 |
| SeoVisualizer | 17.0.0 |
| WebOptimizer | 3.0.477 |
| WebMarkupMin | 2.22.0 |

---

## 🔒 Security Built-in

- CMS-driven security headers (X-Content-Type-Options, X-Frame-Options, CSP, HSTS, Referrer-Policy, Permissions-Policy)
- Request size limits (128MB upload, 4MB form value) to prevent DoS
- Domain-based access control with wildcard support
- Sync IO disabled (IIS + Kestrel)
- Data Protection keys with 90-day rotation
- Proper HTTP status codes for error pages (no soft-404s)

---

## 📖 Documentation

| # | Page | Description |
|---|------|-------------|
| 1 | [Introduction](docs/1.-Intro.md) | Overview, features, architecture |
| 2 | [Setup](docs/2.-Setup.md) | Domain, project, and data setup |
| 3 | [Project Structure](docs/3.-Project-Structure.md) | Solution architecture, middleware pipeline |
| 4 | [Configurations](docs/4.-Configurations.md) | Language, backoffice, security, performance, SEO, database |
| 5 | [Script Queue](docs/5.-Script-Queue.md) | JS/CSS loading system for block components |
| 6 | [Dashboard](docs/6.-Dashboard.md) | Backoffice dashboard & header app |
| 7 | [Content Editing](docs/7.-Content-Editing.md) | Guide for content editors |
| 8 | [Global Settings](docs/8.-Global-Settings.md) | CMS-driven settings (favicon, robots, images, forms) |
| 9 | [Developer Reference](docs/9.-Developer-Reference.md) | Razor helpers & C# extensions |
| 10 | [Backoffice Tools](docs/10.-Backoffice-Tools.md) | Block preview, error pages, optional packages |

---

## 🌐 Perfect for

- Corporate websites and enterprise portals
- Product landing pages and marketing campaigns
- Developer teams who want a consistent, professional starting point
- Agencies looking to deliver faster without sacrificing quality

---

## 📬 Contact

- 📧 Email: [thientu@t4vn.com](mailto:thientu@t4vn.com)
- 🌐 Website: [t4vn.com](https://t4vn.com)
- 📦 Repository: [github.com/T4VN/uTPro](https://github.com/T4VN/uTPro)

---

uTPro is **completely free and open source**, giving developers the freedom to **customize, extend, and innovate without limits**.
