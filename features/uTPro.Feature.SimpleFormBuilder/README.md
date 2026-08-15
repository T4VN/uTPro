---
layout: default
title: "uTPro.Feature.SimpleFormBuilder"
description: "A lightweight form builder for Umbraco – create and manage dynamic forms from the backoffice with no code required."
permalink: "/uTPro.Feature.SimpleFormBuilder/"
feature: true
feature_name: "Simple Form Builder"
---

# uTPro Simple Form Builder for Umbraco

A lightweight form builder — create and manage dynamic forms directly from the Umbraco backoffice with no code required for everyday use.

Works with **Umbraco 16, 17 and 18**. Database-agnostic: **SQL Server**, **SQLite** and **PostgreSQL**.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.SimpleFormBuilder.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleFormBuilder)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.SimpleFormBuilder.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleFormBuilder)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.simpleformbuilder)

![uTPro Form Builder](/screenshots/uTPro.Feature.SimpleFormBuilder/form-builder.png)

---

## Features

- Dedicated **uTPro Form** section with a left **Forms** tree
- Visual builder: groups → 12-column layout → fields, with live settings
- **Conditional field visibility** — show or hide fields based on another field's value
- **Copy / paste** groups, columns and fields across forms
- **Import / Export** form definitions as JSON
- **19 built-in field types** + custom field type extension point
- Client-side + server-side validation with multi-language messages
- **Sensitive fields encrypted at rest**, masked in the UI
- **File uploads** stored outside `wwwroot`, served via authenticated endpoint
- Entry storage with search, date-range filters, paging, CSV/ZIP export
- **Form Picker** property editor for content
- Public REST APIs for submit / render / entries
- **Anti-spam & rate limiting** + pluggable submission pipeline (`IFormSubmissionHandler`)

---

## Quick Start

```bash
dotnet add package uTPro.Feature.SimpleFormBuilder
```

Render a form anywhere:

```razor
@await Component.InvokeAsync("uTProSimpleForm", new { alias = "contact-us" })
```

| Umbraco | .NET | Target |
|---|---|---|
| 16 | .NET 9 | `net9.0` |
| 17 & 18 | .NET 10 | `net10.0` |

---

## Configuration

See [Configuration](configuration/) for all `appsettings.json` options (rate limiting, file uploads, export).

---

## Documentation

| Guide | Description |
|---|---|
| [Getting Started](getting-started/) | Install, compatibility, backoffice layout, building a form, copy/paste |
| [Rendering a Form](rendering/) | ViewComponent, parameters, template resolution, overriding views, JS hooks |
| [Form Picker](form-picker/) | Choose a form from content, Allowed-forms setting, publish validation |
| [Field Types](field-types/) | Built-in types, custom field types + custom settings, FieldHelper |
| [Conditions](conditions/) | Show/hide fields based on another field's value, operators, runtime |
| [Public APIs & Import/Export](public-apis/) | REST endpoints, submission pipeline, JSON import/export |
| [Security & Permissions](security/) | Roles, encryption, rate limiting, file uploads |
| [Configuration](configuration/) | Rate limiting, file uploads path, export settings |
| [Reference](reference/) | Project structure, database tables, migrations |

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/LICENSE.txt).

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.SimpleFormBuilder) · [GitHub](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.simpleformbuilder)
