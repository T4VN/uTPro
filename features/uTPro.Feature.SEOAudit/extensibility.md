---
layout: default
title: "Extensibility – SEO Audit"
description: "How to create custom issue checks for uTPro SEO Audit using IUrlScanIssue."
permalink: "/uTPro.Feature.SEOAudit/extensibility/"
feature: true
feature_name: "SEO Audit"
---

# Extensibility

[← Back to SEO Audit](/uTPro.Feature.SEOAudit/)

## Custom issue checks

Register a custom issue check in DI:

```csharp
services.AddScoped<IUrlScanIssue, MyCustomIssue>();
```

`IUrlScanIssue` exposes:

| Property | Type | Description |
|---|---|---|
| `Alias` | string | Unique identifier |
| `Name` | string | Display name in the Issues view |
| `Description` | string | Explanation of what's wrong |
| `Category` | string | Issue grouping (SEO, Technical, Content, etc.) |
| `Severity` | enum | Error, Warning, Info |
| `Type` | string | Issue classification |
| `Priority` | int | Sort order within category |
| `Matches(ScanResultRow row)` | bool | Whether this issue applies to the given page |

Custom checks appear automatically in the Issues view alongside built-in checks — no UI registration needed.

## Per-node SEO Audit tab

The package adds a **SEO Audit** tab inside every **Content** and **Media** editor. Shown only for users with **Settings** access, and only when the node has a public routable URL (template-less nodes are automatically excluded via `SkipNodesWithoutTemplate = true`).

Opening a node auto-audits its URL(s) in the background and shows a footer warning when an issue is found.
