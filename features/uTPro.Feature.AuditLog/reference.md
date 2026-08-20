---
layout: default
title: "Reference – Audit Log"
description: "Audit Log Viewer reference — views breakdown, column reference, filtering, export, and API endpoint."
permalink: "/uTPro.Feature.AuditLog/reference/"
feature: true
feature_name: "Audit Log"
---

# Reference

[← Back to Audit Log](/uTPro.Feature.AuditLog/)

---

## Views

### Timeline

Merges **all** activity (content + security) into one chronological stream. Best for answering "what happened in the last hour?" across the board.

| Column | Description |
|--------|-------------|
| Date/Time | When the event occurred (toggle Local/UTC) |
| User | The backoffice user who performed the action |
| Event Type | Save, Publish, Unpublish, Move, Delete, Copy, Sign in, Password Reset, etc. |
| Details | Human-readable description of the action |
| Node | Content/media node affected (with quick-edit link) |

### Content Logs

Content & media operations only. Filtered from Umbraco's `umbracoLog` table.

| Column | Description |
|--------|-------------|
| Date/Time | Event timestamp |
| User | Performing user |
| Log Type | Umbraco log type (Save, Publish, Move, Delete, Sort, etc.) |
| Entity ID | Content/media node ID (with quick-edit link) |
| Comment | Action detail / reason |

### Audit Trail

User & security events. Filtered from Umbraco's `umbracoAudit` table.

| Column | Description |
|--------|-------------|
| Date/Time | Event timestamp |
| Performing User | Who triggered the event |
| Affected User | Target user (e.g. whose password was reset) |
| Event Type | Sign in, Sign in failed, Password reset, Profile save, etc. |
| IP Address | Origin IP of the request |
| Details | Additional context |

---

## Filtering

All views share the same filter panel:

| Filter | Options |
|--------|---------|
| **Date range** | This month (default), Last 30 days, Last 7 days, Today, Custom date picker |
| **User** | Dropdown of all backoffice users |
| **Event type** | Dropdown populated from actual data |
| **Search** | Full-text across all visible columns |

Active filters, sort column, sort direction and current page are persisted in the URL — bookmarkable and shareable.

---

## Export

- **CSV** — exports up to 50,000 rows matching current filters
- Formula-injection protection: cells starting with `=`, `+`, `-`, `@` are prefixed with a single quote
- File name includes the view name and date range

---

## API Endpoint

The backoffice section communicates via a single management API controller:

```
POST /umbraco/management/api/v1/utpro/audit-log/{action}
```

| Action | Body | Purpose |
|--------|------|---------|
| `timeline` | `{ skip, take, search?, userId?, dateFrom?, dateTo?, sortBy?, sortDir? }` | Merged timeline |
| `content-logs` | Same shape | Content/media logs |
| `audit-trail` | Same shape + `affectedUserId?`, `eventType?` | Security audit |
| `export` | Same shape + `view` | CSV generation |
| `metadata` | — | Available users and event types for filter dropdowns |

All endpoints require **Settings section access + Administrators group**. Queries use parameterized SQL (injection-safe) with cross-database syntax (SQL Server, SQLite, PostgreSQL).

---

## Database

The package creates **no tables** and modifies **no data**. It reads from Umbraco's built-in tables:

| Table | Used by |
|-------|---------|
| `umbracoLog` | Content Logs, Timeline |
| `umbracoAudit` | Audit Trail, Timeline |
| `umbracoUser` | User display names |

