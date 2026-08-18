---
layout: default
title: "Configuration – Simple Cart"
description: "Optional appsettings.json configuration for uTPro Simple Cart: max quantity per line, schema auto-provisioning, and the session cookie name."
permalink: "/uTPro.Feature.SimpleCart/configuration/"
feature: true
feature_name: "Simple Cart"
---

# Configuration

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

All settings are optional under `uTPro:SimpleCart` in `appsettings.json`. The cart works out of the box with sensible defaults, so configuration is only needed to change behaviour.

```json
{
  "uTPro": {
    "SimpleCart": {
      "MaxQuantityPerLine": 999,
      "AutoProvisionSchema": true,
      "SessionCookieName": "uTPro.SimpleCart.Session"
    }
  }
}
```

---

## Reference

| Key | Default | Description |
|-----|---------|-------------|
| `MaxQuantityPerLine` | `999` | Maximum quantity allowed for a single cart line. Adds and updates are clamped to this value. |
| `AutoProvisionSchema` | `true` | When true, the `uTProProduct` document type and its data types are auto-provisioned on first boot. Set to `false` if you provide your own product schema (and typically a custom `IProductResolver`). |
| `SessionCookieName` | `uTPro.SimpleCart.Session` | Name of the session cookie used to persist the cart. |

---

## Bringing your own product schema

If you already have a product content model, set `AutoProvisionSchema` to `false` so the package does not create the `uTProProduct` type. In that case either:

- reuse the same property aliases (`productName`, `price`, `sku`, `description`, `image`, `isAvailable`), or
- register a custom `IProductResolver` to map your own content — see [Extensibility](extensibility/).

---

## Sessions & load balancing

The cart is session-backed. Out of the box it uses a distributed-memory cache, which is per-instance. For a load-balanced deployment, register a shared distributed cache (for example Redis or SQL Server) in your host so sessions — and therefore carts — are shared across instances.
