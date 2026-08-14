---
layout: default
title: "Configuration – File Manager"
description: "All configuration options for uTPro File Manager: upload limits, editable extensions, multi-root locations, and security lists."
permalink: "/uTPro.Feature.FileManager/configuration/"
feature: true
feature_name: "File Manager"
---

# Configuration

[← Back to File Manager](/uTPro.Feature.FileManager/)

All settings are optional under `uTPro:Feature:FileManager` in `appsettings.json`:

```json
{
  "uTPro": {
    "Feature": {
      "FileManager": {
        "MaxUploadSizeMB": 50,
        "AllowedUploadExtensions": [],
        "BlockedUploadExtensions": [".exe", ".dll", ".bat"],
        "EditableExtensions": [],
        "AdditionalEditableExtensions": [".liquid"],
        "AdditionalBlockedNames": ["secrets.json"],
        "AdditionalDangerousWriteExtensions": [".phtml"],
        "MediaLargeFileThresholdMB": 100,
        "MediaScanCacheSeconds": 30,
        "IgnoredMediaIds": [],
        "MediaScanMaxFiles": 50000,
        "MediaScanTimeBudgetSeconds": 30,
        "Roots": []
      }
    }
  }
}
```

---

## Reference

| Key | Default | Description |
|-----|---------|-------------|
| `MaxUploadSizeMB` | `50` | Maximum upload size in MB |
| `AllowedUploadExtensions` | `[]` | Allow-list (empty = allow all) |
| `BlockedUploadExtensions` | `[]` | Block-list (unioned with Umbraco's disallow list) |
| `AdditionalBlockedNames` | `[]` | Extra protected file names (additive, never reduces) |
| `AdditionalDangerousWriteExtensions` | `[]` | Extra RCE-blocked extensions (additive) |
| `Roots` | `[]` | Multi-root "Locations" config |

---

## Multi-root Locations

When `Roots` is set, File Manager shows a Locations overview with one card per root:

```json
"Roots": [
  { "Key": "web", "Label": "Web root", "Path": "wwwroot", "Icon": "icon-globe", "AdminOnly": false },
  { "Key": "logs", "Label": "Logs", "Path": "umbraco/Logs", "Icon": "icon-document", "AdminOnly": true }
]
```

| Property | Description |
|---|---|
| `Key` | Stable identifier |
| `Label` | Card title |
| `Path` | Absolute or relative to content root |
| `Icon` | Umbraco icon alias |
| `AdminOnly` | Restrict to administrators (default `true`) |

---

## Security lists

The two security lists are **additive only** — config can only add protections, never remove built-in defaults:

- **`AdditionalBlockedNames`** — files that can never be viewed, edited, renamed or deleted
- **`AdditionalDangerousWriteExtensions`** — extensions blocked from create/write/rename
