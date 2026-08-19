---
layout: default
title: "Configuration"
description: "SimpleCart appsettings reference."
permalink: "/uTPro.Feature.SimpleCart/configuration/"
---

# Configuration

All options are in `appsettings.json` under `uTPro:SimpleCart`. Every value has a sensible default, so configuration is optional.

```json
{
  "uTPro": {
    "SimpleCart": {
      "Currency": "USD",
      "MaxQuantityPerLine": 999,
      "ShippingFlatRate": 0,
      "FreeShippingOver": null,
      "AutoProvisionSchema": true,
      "SessionCookieName": "uTPro.SimpleCart.Session"
    }
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `Currency` | `USD` | ISO currency code stamped onto orders. Single currency per storefront. |
| `MaxQuantityPerLine` | `999` | Maximum quantity allowed for a single cart line. |
| `ShippingFlatRate` | `0` | Price used by the built-in flat-rate shipping provider. |
| `FreeShippingOver` | `null` | Subtotal threshold for free shipping (null = disabled). |
| `AutoProvisionSchema` | `true` | Auto-create the Product document type + data types on first boot. |
| `SessionCookieName` | `uTPro.SimpleCart.Session` | The name of the session cookie. |

## Add-on configuration

Payment/shipping add-ons have their own config sections that can also be managed from the backoffice **Store settings** (encrypted, per-provider):

```json
{
  "uTPro": {
    "SimpleCart": {
      "Stripe": { "SecretKey": "sk_test_...", "WebhookSecret": "whsec_..." },
      "VnPay": { "TmnCode": "...", "HashSecret": "...", "BaseUrl": "https://sandbox..." }
    }
  }
}
```

Backoffice values (from **Store settings**) take priority over appsettings when set.

## Provider enable/disable

When a provider is disabled in **Store settings**, it is hidden from the storefront payment methods / shipping quotes and does not participate in adjustment calculation.
