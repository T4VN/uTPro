---
layout: default
title: "Getting Started"
description: "Get started with uTPro – quick start guide, documentation overview, and contact information for the Umbraco Starter Kit by T4VN."
permalink: "/getting-started/"
---

# Getting Started

## 🙏 Thank You for Choosing uTPro

Thank you for trusting and using **uTPro - Umbraco Turbo Pro**. This project is built with care and passion to help developers create enterprise-grade websites faster on the Umbraco platform.

Your feedback — comments, reviews, or suggestions — is incredibly valuable. It helps improve the project and shape future updates. If you have any questions or ideas, feel free to reach out!

**SPECIAL:** We also offer a **LOW COST premium** version for those who want exclusive customization tailored to their personal style. Your support means a lot to us!

- 📧 Email: [thientu@t4vn.com](mailto:thientu@t4vn.com)  
- 🌐 Website: [t4vn.com](https://t4vn.com)  

---

## 📖 Documentation

### For Content Editors

If you manage website content in the backoffice, start here:

| # | Page | Description |
|---|------|-------------|
| 6 | [Dashboard](/6-Dashboard/) | Backoffice dashboard — version check, site statistics |
| 7 | [Content Editing](/7-Content-Editing/) | Create/edit pages, blocks, SEO fields, multilingual content |
| 8 | [Global Settings](/8-Global-Settings/) | Site settings — favicon, robots, images, forms, security headers |
| 10 | [Backoffice Tools](/10-Backoffice-Tools/) | Tools — block preview, SEO audit, file manager |
| 11 | [Search](/11-Search/) | Site search setup and configuration |

### For Developers

If you're setting up, customizing or extending uTPro, read in order:

| # | Page | Description |
|---|------|-------------|
| 1 | [Introduction](/1-Intro/) | Overview, features, architecture, tech stack |
| 2 | [Setup](/2-Setup/) | Domain, project, database, uSync setup |
| 3 | [Project Structure](/3-Project-Structure/) | Solution architecture, middleware pipeline, Program.cs |
| 4 | [Configurations](/4-Configurations/) | Language, backoffice, security, performance, SEO, load balancing, database |
| 5 | [Script Queue](/5-Script-Queue/) | JS loading system for block components |
| 9 | [Developer Reference](/9-Developer-Reference/) | Razor helpers & C# extensions |

### Feature Packages (Optional)

Detailed documentation for each standalone feature package:

| Package | Description |
|---------|-------------|
| [SEO Audit](/uTPro.Feature.SEOAudit/) | Site-wide SEO health check, broken link detection, CSV export |
| [URL Viewer](/uTPro.Feature.UrlViewer/) | Fetch URLs, redirect chain analysis, SEO score |
| [Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/) | Visual form builder with entries, file uploads, API |
| [Search Plus](/uTPro.Feature.SearchPlus/) | Synonym expansion & diacritics-insensitive search |
| [File Manager](/uTPro.Feature.FileManager/) | Server file browser and media cleanup toolkit |
| [Job Monitor](/uTPro.Feature.JobMonitor/) | Background jobs dashboard with telemetry |
| [Audit Log](/uTPro.Feature.AuditLog/) | Detailed activity and audit trail viewer |

---

## 🚀 Quick Start (Developer)

1. **Clone** the repository
2. **Configure** database connection in `appsettings.json` ([details](/2-Setup/))
3. **Build & Run** with `dotnet run`
4. **Import data** via uSync ([details](/2-Setup/#23-setup-data))
5. **Start building** your site!

See [2. Setup](/2-Setup/) for the full guide.

## 🚀 Quick Start (Content Editor)

1. Navigate to `/umbraco` on your website
2. Sign in with your backoffice credentials
3. Go to the **Content** section to create/edit pages
4. Read [7. Content Editing](/7-Content-Editing/) to learn about Block Grid, SEO fields, and multilingual content
