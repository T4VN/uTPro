---
layout: default
title: "Getting Started – File Manager"
description: "Install uTPro File Manager, understand the permission model, and start browsing server files."
permalink: "/uTPro.Feature.FileManager/getting-started/"
feature: true
feature_name: "File Manager"
---

# Getting Started

[← Back to File Manager](/uTPro.Feature.FileManager/)

## Installation

```bash
dotnet add package uTPro.Feature.FileManager
```

No configuration needed — auto-registers via Umbraco `IComposer`. After installation, navigate to **Settings → File Manager** in the backoffice.

## Requirements

- Umbraco 16, 17 or 18
- .NET 9 / .NET 10

## Permissions

| Role | Access |
|---|---|
| **Admin** | Full access: browse entire server root, create, edit, rename, delete, upload, extract |
| **Settings (non-admin)** | Browse `wwwroot/` tree only (view structure — no file actions) |
| **Settings + Sensitive Data** | Browse `wwwroot/` + view/edit/download file content |
| **Media Cleanup actions** | Requires Media section access (Admins always qualify) |

## UI Overview

- Windows Explorer-style navigation (back, reload, home, breadcrumb path, search)
- **List / Grid** view modes with image thumbnails
- Workspace footer actions: New ▾ (Upload, New Folder, New File, Import URL), Delete, Extract Zip
- File open: Monaco code editor with Save, Actions ▾ (Download, Rename, Delete)
