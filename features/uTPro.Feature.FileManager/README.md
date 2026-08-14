---
layout: default
title: "uTPro.Feature.FileManager"
description: "A powerful File Manager and Media Cleanup toolkit for the Umbraco backoffice – browse, upload, edit, preview and clean up media."
permalink: "/uTPro.Feature.FileManager/"
feature: true
feature_name: "File Manager"
---

# uTPro File Manager & Media Cleanup for Umbraco

A powerful **File Manager** and **Media Cleanup** toolkit for the **Umbraco 16+** backoffice. Browse, upload, download, edit, preview, rename and delete server files — and scan the media library to recycle, restore or delete unused, broken, duplicate, orphaned, large and disallowed media.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.FileManager.svg)](https://www.nuget.org/packages/uTPro.Feature.FileManager)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.FileManager.svg)](https://www.nuget.org/packages/uTPro.Feature.FileManager)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.filemanager)

---

## Features

### File Manager
- Windows Explorer-style navigation
- Upload, download, create, rename, delete
- Built-in Monaco Editor with syntax highlighting
- Media preview (images, video, audio, PDF)
- Import file via URL, Extract ZIP
- Multi-root "Locations" support

### Media Cleanup
- Unused, Broken, Duplicates, Orphaned, Large files, Recycle Bin
- Bulk actions, smart duplicate cleanup, preview

### Security
- Role-based: Admin / Settings / Sensitive Data
- RCE guard, SSRF protection, path traversal protection

---

## Installation

```bash
dotnet add package uTPro.Feature.FileManager
```

Navigate to **Settings → File Manager**. No configuration needed.

---

## Configuration

Under `uTPro:Feature:FileManager` in `appsettings.json`:

| Key | Default | Description |
|-----|---------|-------------|
| `MaxUploadSizeMB` | `50` | Max upload size |
| `MediaLargeFileThresholdMB` | `100` | Large file threshold |
| `MediaScanCacheSeconds` | `30` | Scan cache |
| `Roots` | `[]` | Multi-root locations |

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](https://github.com/T4VN/uTPro.Feature.FileManager/blob/main/LICENSE.txt).

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.FileManager) · [GitHub](https://github.com/T4VN/uTPro.Feature.FileManager) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.filemanager)
