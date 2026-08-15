---
layout: default
title: "Configuration – Simple Form Builder"
description: "All configuration options for uTPro Simple Form Builder: rate limiting, file uploads, and export settings."
permalink: "/uTPro.Feature.SimpleFormBuilder/configuration/"
feature: true
feature_name: "Simple Form Builder"
---

# Configuration

[← Back to Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)

All settings are optional under `uTPro:Feature:Form` in `appsettings.json`:

```json
{
  "uTPro": {
    "Feature": {
      "Form": {
        "RateLimit": {
          "Enabled": true,
          "PermitLimit": 5,
          "WindowSeconds": 60
        },
        "FileUploadsPath": "",
        "MaxExportEntries": 10000
      }
    }
  }
}
```

---

## Reference

| Key | Default | Description |
|-----|---------|-------------|
| `RateLimit:Enabled` | `true` | Per-IP + per-form throttling of the public submit endpoint |
| `RateLimit:PermitLimit` | `5` | Max submissions per window per IP + form |
| `RateLimit:WindowSeconds` | `60` | Fixed-window length in seconds |
| `FileUploadsPath` | `""` | Custom upload folder (absolute or relative to content root) |
| `MaxExportEntries` | `10000` | Cap for ZIP export entries |

---

## Rate Limiting

The public submit endpoint is protected by a built-in **per-IP + per-form** fixed-window rate limiter, enabled by default. Exceeding returns **HTTP 429**.

> Behind a reverse proxy, ensure the real client IP is forwarded. In uTPro, enable the `uTPro:ForwardedHeaders` section.

---

## File Uploads Path

Files submitted through `file` fields are stored outside `wwwroot`. By default under `<ContentRoot>/umbraco/Data/uTProSimpleFormUploads`.

- An absolute path is used as-is
- A relative value resolves against the content root
- Point all apps at the same folder for load-balanced deployments

See [Security & Permissions](../security/) for the full file upload security model.
