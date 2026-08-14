---
layout: default
title: "uTPro.Feature.AuditLog"
description: "The missing audit log & content log viewer for the Umbraco backoffice – browse, search, filter, sort, and export every action."
permalink: "/uTPro.Feature.AuditLog/"
feature: true
feature_name: "Audit Log"
---

# uTPro Audit Log Viewer for Umbraco

The missing audit log & content log viewer for the Umbraco backoffice. Browse, search, filter, sort, and export every login, save, publish, move, and delete in your Umbraco site.

Supports **Umbraco 16, 17 and 18**. Works on **SQL Server**, **SQLite** and **PostgreSQL**.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.AuditLog.svg)](https://www.nuget.org/packages/uTPro.Feature.AuditLog)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.AuditLog.svg)](https://www.nuget.org/packages/uTPro.Feature.AuditLog)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.auditlog)

![uTPro Audit Log](/screenshots/uTPro.Feature.AuditLog/Screenshot-1.png)

---

## Key features

- **Three views** — Timeline, Content Logs, Audit Trail
- **Full-text search** across all fields
- **Rich filtering** — by user, event type, date range
- **Quick date ranges** — This month, Last 30/7 days, Today, Custom
- **Sortable columns** — server-side
- **Quick edit links** — jump to content editor
- **Local / UTC time toggle**
- **Export to CSV** — up to 50,000 rows
- **Shareable & bookmarkable filters**
- **Admin-only access**
- **Read-only** — no tables created, no data modified

---

## Installation

```bash
dotnet add package uTPro.Feature.AuditLog
```

Navigate to **Settings → Advanced → Audit Log Viewer**. No configuration required.

---

## The three views

| View | Source |
|------|--------|
| **Timeline** | `umbracoAudit` + `umbracoLog` merged |
| **Content Logs** | `umbracoLog` |
| **Audit Trail** | `umbracoAudit` |

---

## Security

- Admin-only (Settings + Administrators group)
- CSV export neutralizes formula injection
- Read-only — zero schema changes

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](https://github.com/T4VN/uTPro.Feature.AuditLog/blob/main/LICENSE.txt).

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.AuditLog) · [GitHub](https://github.com/T4VN/uTPro.Feature.AuditLog) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.auditlog)
