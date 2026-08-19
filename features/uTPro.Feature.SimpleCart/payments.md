---
layout: default
title: "Payments – Simple Cart"
description: "SimpleCart payment providers – Stripe, VNPay and building your own."
permalink: "/uTPro.Feature.SimpleCart/payments/"
feature: true
feature_name: "Simple Cart"
---

# Payments

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)


Payment gateways are **add-on packages** that implement `IPaymentProvider` and register themselves via a composer — no edits to the core. The storefront then lists available methods, starts payment, and handles callbacks.

## Available add-ons

| Package | Gateway | Market |
|---------|---------|--------|
| `uTPro.Feature.SimpleCart.Payments.Stripe` | Stripe Checkout (hosted) + signed webhooks | International |
| `uTPro.Feature.SimpleCart.Payments.VnPay` | VNPay redirect + HMAC-SHA512 IPN | Vietnam (VND) |

Built-in (no extra package): **Cash on Delivery** (`cod`) — offline, no online step.

## Flow

1. Checkout creates an order with `paymentProvider: "stripe"` (or chosen alias).
2. Storefront calls `POST /api/simplecart/payment/start` → provider returns a redirect URL.
3. `uTProSimpleCart.startPayment({ orderNumber, returnUrl, cancelUrl })` sends the shopper to the gateway.
4. Gateway calls back to `GET|POST /api/simplecart/payment/callback/{alias}` → provider verifies signature → SimpleCart marks the order **Paid**.

`PaymentAction` on the result: `Redirect` (online gateway), `Completed` (settled immediately), `Pending` (offline).

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/simplecart/payment/methods` | List enabled payment methods |
| `POST` | `/api/simplecart/payment/start` | Begin payment (`{ orderNumber, providerAlias?, returnUrl?, cancelUrl? }`) |
| `GET\|POST` | `/api/simplecart/payment/callback/{alias}` | Gateway callback/webhook |

## Install Stripe

```bash
dotnet add package uTPro.Feature.SimpleCart.Payments.Stripe
```

Configure in backoffice **Store settings** (or appsettings `uTPro:SimpleCart:Stripe`):
- SecretKey (`sk_test_…`)
- WebhookSecret (`whsec_…`)

Point a Stripe webhook at `https://your-site/api/simplecart/payment/callback/stripe` for `checkout.session.completed`.

## Install VNPay

```bash
dotnet add package uTPro.Feature.SimpleCart.Payments.VnPay
```

Configure: `TmnCode`, `HashSecret`, `BaseUrl` (sandbox or production), `Locale`. Set `Currency: "VND"`.

Configure IPN URL in the VNPay merchant portal to `https://your-site/api/simplecart/payment/callback/vnpay`.

## Build your own

Implement `IPaymentProvider` (+ optionally `IConfigurableProvider` for backoffice settings form) and register it:

```csharp
builder.SimpleCartPaymentProviders().Append<MyGatewayProvider>();
```

See [Extensibility](/uTPro.Feature.SimpleCart/extensibility/) for the full interface and a VNPay-style example.
