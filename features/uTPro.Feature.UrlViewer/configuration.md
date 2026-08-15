---
layout: default
title: "Configuration – URL Viewer"
description: "Configuration options for uTPro URL Viewer: SSRF guard relaxation for internal/dev hosts."
permalink: "/uTPro.Feature.UrlViewer/configuration/"
feature: true
feature_name: "URL Viewer"
---

# Configuration

[← Back to URL Viewer](/uTPro.Feature.UrlViewer/)

No configuration is required — the package works out of the box. For internal/dev hosts only:

```json
{
  "uTPro": {
    "Feature": {
      "UrlViewer": {
        "AllowInternalHosts": false,
        "AllowInvalidCertificates": false
      }
    }
  }
}
```

---

## Reference

| Key | Default | Description |
|-----|---------|-------------|
| `AllowInternalHosts` | `false` | Relax the SSRF guard to allow fetching private/local addresses (RFC-1918, localhost, `.local`). Only for internal dev sites. |
| `AllowInvalidCertificates` | `false` | Accept self-signed / invalid TLS certificates. Only for internal dev sites. |

---

## Security Note

By default, all server-side fetches run behind an **SSRF guard** that blocks:
- Private addresses (RFC-1918, CGNAT)
- Loopback (localhost, 127.0.0.0/8)
- Link-local, IPv6 ULA/loopback
- `.local` hostnames

The guard **re-checks on every redirect hop** — a redirect to an internal address cannot bypass it.

Only relax these settings in controlled development environments.
