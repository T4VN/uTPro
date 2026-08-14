---
layout: default
title: "Security & Permissions – Simple Form Builder"
description: "Role-based access, sensitive data encryption, file upload security, and rate limiting in uTPro Simple Form Builder."
permalink: "/uTPro.Feature.SimpleFormBuilder/security/"
feature: true
feature_name: "Simple Form Builder"
---

# Security & Permissions

[← Back to Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)

---

## Roles & Permissions

| Capability | Required permission |
|---|---|
| See the **uTPro Form** menu | Group granted the *uTPro Form* section |
| View form list, entries, export CSV/ZIP | Any user with the section |
| Download uploaded file (non-sensitive) | Any user with the section |
| Download **Sensitive Data** file | Admin or `sensitiveData` group |
| Create / edit / delete forms | `canEdit` (admin or Settings access) |
| Delete entries | `canEdit` |
| See decrypted sensitive values | Admin or `sensitiveData` group |

---

## Sensitive-data encryption

![Sensitive data masked](/screenshots/uTPro.Feature.SimpleFormBuilder/sensitive-data.png)

Uses **ASP.NET Core Data Protection** (AES-256-CBC + HMAC-SHA256).

**On submit:** sensitive fields are encrypted with prefix `uTProEncode:` before storage.

**On read:** decrypted only for admin/sensitiveData users; otherwise shows `*****`.

**Important:**
- Encryption only applies to NEW submissions made while the field is sensitive
- Back up Data Protection keys — lost keys = unreadable encrypted values
- For load-balanced setups, use a shared Data Protection key ring

---

## File uploads

- Stored **outside `wwwroot`** — never served as static content
- Download via authenticated endpoint only
- Path encrypted in entry reference (no real path exposed)
- Sensitive file fields denied for users without permission
- Files cleaned up when entry/form is deleted

### Storage location

Configure via `uTPro:Feature:Form:FileUploadsPath`:

```json
{
  "uTPro": {
    "Feature": {
      "Form": {
        "FileUploadsPath": "D:\\shared\\form-uploads"
      }
    }
  }
}
```

---

## Rate limiting & anti-spam

Per-IP + per-form fixed-window rate limiter, enabled by default:

```json
{
  "uTPro": {
    "Feature": {
      "Form": {
        "RateLimit": {
          "Enabled": true,
          "PermitLimit": 5,
          "WindowSeconds": 60
        }
      }
    }
  }
}
```

Exceeding returns **HTTP 429**.

> Behind a reverse proxy, ensure the real client IP is forwarded.

For custom anti-spam, implement `IFormSubmissionHandler` — see [Public APIs](../public-apis/).
