---
layout: default
title: "Checkout & Orders"
description: "SimpleCart checkout flow, order model, status workflow and backoffice management."
permalink: "/uTPro.Feature.SimpleCart/orders/"
---

# Checkout & Orders

## How checkout works

1. The shopper builds a cart (session-backed, prices resolved server-side).
2. The storefront calls `POST /api/simplecart/checkout` (or `uTProSimpleCart.checkout(details)`) with customer/delivery details + optional `shippingMethod`, `paymentProvider` and `codes` (discounts/gift cards).
3. `CheckoutService` reads the **server-side cart** (never the request), resolves shipping/adjustments, freezes everything, saves the order with status `Pending`, empties the cart, and fires `OnOrderPlacedAsync` handlers.

**Important:** line items and prices come from the cart — the order total cannot be spoofed.

## Checkout request

```json
{
  "customerName": "Jane Doe",
  "customerEmail": "jane@example.com",
  "customerPhone": "+84...",
  "shippingAddress": "1 Main St",
  "notes": "Ring bell",
  "shippingMethod": "standard",
  "paymentProvider": "stripe",
  "codes": ["SAVE10", "GC-XXXX-YYYY-ZZZZ"]
}
```

Only `customerName` and `customerEmail` are required.

## Order model

Each order stores:
- **Frozen line items** (product key, name, SKU, unit price, quantity, line total)
- **Customer + delivery info**
- **Shipping** method/name/total
- **Payment** provider/name/status/reference/paid-at
- **Adjustments** (discounts, gift cards) — type, label, code, signed amount
- **Monetary totals**: SubTotal, ShippingTotal, TaxTotal, GrandTotal
- **Currency** (ISO code, stamped from config)
- **Status** (fulfilment) and **PaymentStatus** (separately tracked)

Money is stored as integer minor units (e.g. cents) for exact cross-database sorting/reporting.

## Status workflow

**Fulfilment status:** `Pending → Paid → Shipped → Completed` (or `Cancelled` at any point).

**Payment status:** `Unpaid → Paid` (or `Refunded`). Updated by the payment flow (gateway callback) or manually from the backoffice.

Status changes fire `IOrderEventHandler.OnOrderStatusChangedAsync`.

## Backoffice (Orders view)

- List with search, status filter, payment-status filter
- Detail: customer, items table, totals (incl. adjustments), payment & shipping info
- Status change (dropdown) — fires event handlers

## Programmatic access

```csharp
// Inject IOrderService
Order? order = orderService.GetOrder(id);
Order? byNumber = orderService.GetOrderByNumber("SC-20260819-...");
PagedResult<Order> page = orderService.GetOrders(skip: 0, take: 20, status: "Pending");
await orderService.UpdateStatusAsync(id, "Shipped");
await orderService.MarkPaidAsync(id, "pi_xxx");

// Inject ICheckoutService
var result = await checkoutService.CheckoutAsync(new CheckoutRequest { ... });
```

## JS client

```js
const order = await uTProSimpleCart.checkout({
  customerName: "Jane",
  customerEmail: "jane@example.com",
  shippingMethod: "standard",
  paymentProvider: "stripe",
  codes: ["SAVE10"],
});
// fires "simplecart:ordered" event
```
