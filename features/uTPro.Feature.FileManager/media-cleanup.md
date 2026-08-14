---
layout: default
title: "Media Cleanup – File Manager"
description: "Scan and clean up unused, broken, duplicate, orphaned, large, and disallowed media in the Umbraco media library."
permalink: "/uTPro.Feature.FileManager/media-cleanup/"
feature: true
feature_name: "File Manager"
---

# Media Cleanup

[← Back to File Manager](/uTPro.Feature.FileManager/)

The **Media Cleanup** tab (Settings → File Manager → Media Cleanup) scans automatically and reports media across seven categories:

---

## Categories

| Category | What it finds |
|---|---|
| **Unused media** | Media items not referenced by any content |
| **Broken media** | Media items whose backing file is missing |
| **Duplicates** | Files sharing the same SHA-256 hash |
| **Orphaned files** | Files in media storage not linked to any media item |
| **Large files** | Files above configurable threshold (default 100 MB) |
| **Disallowed** | Files with extensions in Umbraco's disallow list |
| **Recycle Bin** | Media items currently in the bin |

---

## Actions

| Category | Available actions |
|---|---|
| Unused / Broken / Duplicates / Large | **Move to recycle bin** (recoverable) |
| Orphaned files | **Delete file** directly |
| Recycle Bin | **Restore** or **Delete permanently** + **Empty bin** |

**Bulk actions** — tick rows and act on many at once.

**Smart duplicates** — "Recycle dupes (keep 1)" recycles every copy except the first in each hash group.

**Preview** — click an image to preview before deciding.

---

## Configuration

| Key | Default | Description |
|---|---|---|
| `MediaLargeFileThresholdMB` | `100` | Large file threshold |
| `MediaScanCacheSeconds` | `30` | Scan cache duration |
| `IgnoredMediaIds` | `[]` | IDs to silence false positives |
| `MediaScanMaxFiles` | `50000` | Stop scan after this many files |
| `MediaScanTimeBudgetSeconds` | `30` | Stop scan after this time |

---

## Security

- **Scan report** is visible to any Settings user
- **Actions** (recycle/restore/delete) require **Media section access**
- Works with any storage provider (disk, Azure Blob, S3, …)
