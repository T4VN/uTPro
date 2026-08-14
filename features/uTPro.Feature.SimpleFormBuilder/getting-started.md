---
layout: default
title: "Getting Started – Simple Form Builder"
description: "Install uTPro Simple Form Builder, set up permissions, and build your first form in the Umbraco backoffice."
permalink: "/uTPro.Feature.SimpleFormBuilder/getting-started/"
feature: true
feature_name: "Simple Form Builder"
---

# Getting Started

[← Back to Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)

## Install via NuGet

```bash
dotnet add package uTPro.Feature.SimpleFormBuilder
```

On first run, uTPro Form automatically creates its database tables and seeds a sample **Contact Us** form. No manual SQL or configuration needed.

## Framework / Umbraco compatibility

| Umbraco | .NET | Package target |
|---|---|---|
| 16 | .NET 9 | `net9.0` |
| 17 & 18 | .NET 10 | `net10.0` |

The package multi-targets both, so the correct dependencies are restored automatically for your project.

## Where It Lives in the Backoffice

After install, a new **uTPro Form** item appears in the top (blue) section menu.

> **Access:** custom sections are governed by user-group permissions. Grant the **uTPro Form** section to a user group under **Users → User groups → _group_ → Sections → Choose → uTPro Form**, then reload the backoffice. See [Security & Permissions](../security/).

The section uses a familiar two-pane layout:

![Forms list](/screenshots/uTPro.Feature.SimpleFormBuilder/form-list.png)

- **Left panel (Forms tree)** — lists all forms. A **+** button creates a new form, and a **⋯ (Options)** menu offers Reload, Create, Import.
- **Main area** — a **Create** button, an **Import** button, a **filter** box, and the forms table.

## Building your first form

1. Open the **uTPro Form** section
2. Click **Create** (main area) or **+** (left panel)
3. Give it a **Name** and **Alias** (the alias is what you use in code)
4. Add **Groups** to organize fields into sections
5. Inside each group, add **Columns** (12-column grid) and drop **Fields** into them
6. Configure each field: type, label, placeholder, validation, etc.
7. Set the **Success Message**, optional **Redirect URL**, and **Email Notification**
8. Save

![Form settings](/screenshots/uTPro.Feature.SimpleFormBuilder/form-settings.png)

Your form is ready to [render on the front-end](../rendering/). Submissions land in the **Entries** view with search, date-range filters, paging and CSV export:

![Entries](/screenshots/uTPro.Feature.SimpleFormBuilder/entries.png)

## Copy / Paste (Groups, Columns, Fields)

The builder can copy a whole **group**, **column** or **field** and paste it elsewhere — even into a different form.

- Copy buttons sit on each group/column/field; **Paste** buttons appear only when the clipboard holds a matching item.
- Backed by the browser's `localStorage`, so the copied item survives navigating between forms and even a full page reload.
- On paste, all internal IDs are regenerated and colliding field `name`s are de-duped so submissions never clash.
