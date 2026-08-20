---
layout: default
title: "Security – File Manager"
description: "File Manager security model — RCE prevention, SSRF protection, path traversal defense, role-based access, and extension blocking."
permalink: "/uTPro.Feature.FileManager/security/"
feature: true
feature_name: "File Manager"
---

# Security

[← Back to File Manager](/uTPro.Feature.FileManager/)

The File Manager is a powerful tool that exposes the server filesystem. Its security model is designed around **defense in depth** — multiple overlapping safeguards that assume any single layer can fail.

---

## Role-based access

| Role | Capability |
|------|------------|
| **Admin** | Full access: browse all roots, create, edit, rename, delete, upload, extract |
| **Settings (non-admin)** | Browse `wwwroot/` tree only — view structure, no file actions |
| **Settings + Sensitive Data** | Browse `wwwroot/` + view, edit, download file content |
| **Media section** | Required for Media Cleanup actions (recycle/restore/delete media) |

Admins always have full capability regardless of other group memberships.

---

## Remote Code Execution (RCE) prevention

Server-side file write operations are blocked for **dangerous extensions** — files that could be executed by the web server if written to the webroot:

**Built-in blocked extensions:** `.aspx`, `.cshtml`, `.vbhtml`, `.ashx`, `.asmx`, `.config`, `.cs`, `.vb`, `.dll`, `.exe`, `.bat`, `.cmd`, `.ps1`, `.sh`

Additional extensions can be added via `AdditionalDangerousWriteExtensions` in config — this list is **additive only** and cannot reduce the built-in protections.

The guard applies to:
- File creation (New File)
- File editing (Save)
- File renaming (cannot rename *to* a dangerous extension)
- ZIP extraction (dangerous files inside archives are skipped)

---

## Path traversal defense

- All file operations resolve the **canonical path** and verify it remains within the allowed root(s)
- `..` segments, symbolic links, and junction points that escape the sandbox are rejected
- Path validation runs on every operation (browse, read, write, delete, rename, extract)

---

## SSRF protection (Import URL)

The "Import URL" feature (download a remote file to the server) enforces the same SSRF guard used by URL Viewer and SEO Audit:

- Blocks private addresses (RFC-1918, CGNAT 100.64.0.0/10)
- Blocks loopback (localhost, 127.0.0.0/8, ::1)
- Blocks link-local and IPv6 ULA
- Blocks `.local` hostnames
- **Re-checks on every redirect hop** — a redirect to an internal address cannot bypass it
- Can be relaxed via `AllowInternalHosts: true` (development environments only)

---

## Protected file names

Certain file names are protected from view, edit, rename and delete:

**Built-in:** `web.config`, `appsettings.json`, `appsettings.*.json`, `.env`, `*.pfx`, `*.key`

Additional names can be added via `AdditionalBlockedNames` in config — **additive only**.

---

## Upload restrictions

| Control | Description |
|---------|-------------|
| `MaxUploadSizeMB` | Hard limit on individual file uploads (default 50 MB) |
| `AllowedUploadExtensions` | Allow-list (empty = allow all except blocked) |
| `BlockedUploadExtensions` | Block-list, unioned with Umbraco's disallow list + RCE extensions |

The effective block list is: Umbraco's built-in disallow list ∪ config `BlockedUploadExtensions` ∪ RCE-dangerous extensions. There is no way to reduce it via configuration.

---

## ZIP extraction safety

- Maximum extracted size limit prevents zip bombs
- Each extracted file path is validated against path traversal
- Dangerous extensions are skipped (not extracted)
- Extraction only allowed in the current directory (no absolute paths honored from the archive)

---

## API security

All management API endpoints are secured at `/umbraco/management/api/v1/utpro/file-manager/...` and require authenticated backoffice users with appropriate role.

