---
layout: default
title: "Reference – Simple Form Builder"
description: "Project structure, database tables, migrations, and static assets for uTPro Simple Form Builder."
permalink: "/uTPro.Feature.SimpleFormBuilder/reference/"
feature: true
feature_name: "Simple Form Builder"
---

# Reference

[← Back to Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)

---

## Database Tables

Created automatically on first startup.

### `uTProSimpleForm` — form definitions

| Column | Type | Purpose |
|---|---|---|
| Id | int (PK) | Auto-increment |
| Name | nvarchar(255) | Display name |
| Alias | nvarchar(255) | Unique identifier used in code |
| GroupsJson | ntext | Groups → Columns → Fields as JSON |
| SuccessMessage | nvarchar(1000) | Shown after submission |
| RedirectUrl | nvarchar(500) | Optional redirect |
| EmailTo | nvarchar(500) | Notification recipient |
| StoreEntries | bit | Whether to save submissions |
| IsEnabled | bit | Active or disabled |
| EnableRenderApi | bit | Allow public render API |
| EnableEntriesApi | bit | Allow public entries API |
| ShowInPicker | bit | Appears in Form Picker |

### `uTProSimpleFormEntry` — submissions

| Column | Type | Purpose |
|---|---|---|
| Id | int (PK) | Auto-increment |
| FormId | int | Links to form |
| DataJson | ntext | Submitted data (sensitive encrypted) |
| IpAddress | nvarchar(100) | Submitter's IP |
| UserAgent | nvarchar(500) | Submitter's browser |
| CreatedUtc | datetime | Submission timestamp |

---

## Configuration

| Section | Key | Default | Purpose |
|---|---|---|---|
| `uTPro:Feature:Form:RateLimit` | `Enabled` | `true` | Per-IP throttling |
| `uTPro:Feature:Form:RateLimit` | `PermitLimit` | `5` | Max per window |
| `uTPro:Feature:Form:RateLimit` | `WindowSeconds` | `60` | Window length |
| `uTPro:Feature:Form` | `FileUploadsPath` | `""` | Custom upload folder |
| `uTPro:Feature:Form` | `MaxExportEntries` | `10000` | ZIP export cap |

---

## File storage

Files are written to:

```
App_Data/umbraco/Data/uTProSimpleFormUploads/{formAlias}/{yyyyMM}/{guid}{ext}
```

Configurable via `FileUploadsPath`. Never served as static content.

---

## Migrations

Run automatically on startup via Umbraco's migration system:

1. `utprosimpleform-init` — creates tables + seeds sample form
2. `utprosimpleform-showinpicker` — adds ShowInPicker column (idempotent)

Also ensures a **uTPro Form Picker** data type exists.
