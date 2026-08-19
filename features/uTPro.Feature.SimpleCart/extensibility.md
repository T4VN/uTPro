---
layout: default
title: "Extensibility – Simple Cart"
description: "SimpleCart extension points — add your own payment, shipping, discount or event handler."
permalink: "/uTPro.Feature.SimpleCart/extensibility/"
feature: true
feature_name: "Simple Cart"
---

# Extensibility

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

SimpleCart is designed so that **third-party features plug in from your own composer — you never edit the core package**. All extension points are DI-resolved Umbraco collection builders.

## Extension points

| Interface | Builder method | Purpose |
|-----------|---------------|---------|
| `IPaymentProvider` | `builder.SimpleCartPaymentProviders()` | Add a payment gateway |
| `IShippingProvider` | `builder.SimpleCartShippingProviders()` | Add shipping rate logic |
| `IOrderAdjustmentProvider` | `builder.SimpleCartOrderAdjustmentProviders()` | Add discounts / gift cards / fees |
| `IOrderEventHandler` | `builder.SimpleCartOrderEventHandlers()` | Side-effects (email, ERP, stock) |
| `IProductResolver` | Register via `builder.Services.AddScoped<>()` | Change where products come from |
| `ICartService` | Register via `builder.Services.AddScoped<>()` | Change cart storage (session → DB) |
| `IConfigurableProvider` | Implement alongside one of the above | Expose a backoffice settings form |

## Registering from your own composer

```csharp
using uTPro.Feature.SimpleCart.Composing;

public class MyShopComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.SimpleCartPaymentProviders().Append<MyGateway>();
        builder.SimpleCartShippingProviders().Append<MyShipping>();
        builder.SimpleCartOrderAdjustmentProviders().Append<MyCouponEngine>();
        builder.SimpleCartOrderEventHandlers().Append<MyEmailHandler>();
    }
}
```

Builders are ordered collections — you can also `Insert<>`, `InsertBefore<>`, `Remove<>`.

## IPaymentProvider

```csharp
public sealed class MyGateway : IPaymentProvider, IConfigurableProvider
{
    public string Alias => "my-gateway";
    public string Name  => "My Gateway";

    public IEnumerable<ProviderSettingField> GetSettingFields() => [
        new("ApiKey", "API Key", ProviderSettingType.Password, required: true),
    ];

    public Task<PaymentInitiationResult> StartAsync(PaymentRequest request) { ... }
    public Task<PaymentCallbackResult> HandleCallbackAsync(HttpContext ctx) { ... }
}
```

## IShippingProvider

```csharp
public sealed class MyShipping : IShippingProvider
{
    public string Alias => "dhl";
    public string Name  => "DHL";

    public Task<IEnumerable<ShippingQuote>> GetQuotesAsync(ShippingContext ctx) { ... }
}
```

## IOrderAdjustmentProvider

```csharp
public sealed class MyCoupons : IOrderAdjustmentProvider
{
    public string Alias => "my-coupons";
    public string Name  => "My Coupons";

    public Task<IEnumerable<OrderAdjustment>> GetAdjustmentsAsync(AdjustmentContext ctx)
    {
        // ctx.Codes contains the shopper's codes; ctx.SubTotal the current subtotal.
        // Return adjustments with negative Amount to reduce the total.
    }
}
```

## IOrderEventHandler

```csharp
public sealed class EmailHandler : IOrderEventHandler
{
    public Task OnOrderPlacedAsync(Order order) { /* send email */ }
    public Task OnOrderStatusChangedAsync(Order order, string prev) { /* notify */ }
}
```

Handlers run best-effort: an exception in one is logged and never blocks the others.

## IConfigurableProvider (backoffice settings form)

Any provider that implements this gets an auto-generated form in **Store settings**. Field types: `Text`, `Password` (encrypted at rest, masked in UI), `Number`, `Decimal`, `Boolean`, `Select`.

Read stored values at runtime via `IProviderSettingsService`:

```csharp
var key = _settings.GetValue("my-gateway", "ApiKey");
```

Disabled providers are hidden from the storefront.
