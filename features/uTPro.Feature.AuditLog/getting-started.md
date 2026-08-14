---
layout: default
title: "Getting Started – Audit Log"
description: "Install uTPro Audit Log Viewer and explore the three views: Timeline, Content Logs, and Audit Trail."
permalink: "/uTPro.Feature.AuditLog/getting-started/"
feature: true
feature_name: "Audit Log"
---

# Getting Started

[← Back to Audit Log](/uTPro.Feature.AuditLog/)

## Installation

```bash
dotnet add package uTPro.Feature.AuditLog
```

No configuration required. The package registers itself automatically.

## Requirements

- Umbraco 16, 17 or 18
- .NET 9 / .NET 10
- SQL Server, SQLite or PostgreSQL

## Where to find it

**Settings → Advanced → Audit Log Viewer** (left sidebar, below the built-in Log Viewer).

Only backoffice **administrators** can access it (requires Settings-section + Administrators group).

## The three views

| View | What it shows | Source table |
|------|---------------|--------------|
| **Timeline** | All activity merged chronologically | `umbracoAudit` + `umbracoLog` |
| **Content Logs** | Content & media actions (Save, Publish, Move, Delete…) | `umbracoLog` |
| **Audit Trail** | User & security events (sign-in, password reset, profile changes…) | `umbracoAudit` |

## Features in each view

- **Full-text search** across details, user, IP, event type, comment, node ID
- **Rich filtering** — by performing user, affected user, event type, date range
- **Quick date ranges** — This month (default), Last 30 days, Last 7 days, Today, Custom
- **Sortable columns** — click any header (server-side, fast on large tables)
- **Quick edit links** — jump from a log entry to its content editor
- **Local / UTC time toggle** — switch the whole table
- **Server-side pagination** with jump-to-page
- **Export to CSV** — up to 50,000 rows
- **Shareable filters** — active filter, sort, and page saved in URL

## Security

- **Admin-only** — requires Settings-section + Administrators group
- **CSV export neutralizes formula injection** — cells starting with `=`, `+`, `-`, `@` are quoted
- **Read-only** — creates no tables, modifies no data
- SQL `LIKE` wildcard escaping on search terms

## How it works

A self-contained Razor Class Library:
- Management API controller reads `umbracoAudit` and `umbracoLog` with parameterized SQL
- Queries are cross-database (SQL Server, SQLite, PostgreSQL) via Umbraco's `SqlSyntax` provider
- Lit-based backoffice extension with three workspace-view tabs
