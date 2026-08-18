---
layout: default
title: "Rendering the Cart – Simple Cart"
description: "Render the cart with a Razor ViewComponent, wire up buttons with data-* attributes, use the JavaScript helper, and override views and styles."
permalink: "/uTPro.Feature.SimpleCart/rendering/"
feature: true
feature_name: "Simple Cart"
---

# Rendering the Cart

[← Back to Simple Cart](/uTPro.Feature.SimpleCart/)

The package ships a drop-in cart component and a dependency-free JavaScript helper, so a basic storefront works with almost no code.

---

## Cart ViewComponent

Render the current visitor's cart in any Razor view or block:

```razor
@await Component.InvokeAsync("Cart")
```

The component reads the live cart (names and prices resolved server-side in the current culture) and renders the lines, quantities and subtotal.

![Cart rendered on the front-end](/screenshots/uTPro.Feature.SimpleCart/cart-render.png)

---

## Wiring buttons with `data-*` attributes

Include the shipped script, then annotate elements with `data-simplecart-*` attributes — the helper binds them automatically (no inline JavaScript needed):

```html
<script src="/uTPro/simplecart/simplecart.js"></script>
```

| Attribute | Element | Behaviour |
|---|---|---|
| `data-simplecart-add="<productKey>"` | button / link | Adds 1 (or the value of a paired quantity input) |
| `data-simplecart-addqty` | input | Optional quantity input next to an add button |
| `data-simplecart-qty="<productKey>"` | input | Sets the absolute quantity on change |
| `data-simplecart-remove="<productKey>"` | button / link | Removes the line |
| `data-simplecart-clear` | button / link | Empties the cart |
| `data-simplecart-count` | any element | Text is set to the total quantity after each change |
| `data-simplecart-root` | container | Marks a page as the cart page (re-renders on change) |

Example add-to-cart control:

```html
<div>
    <input type="number" value="1" min="1" data-simplecart-addqty>
    <button data-simplecart-add="@product.Key">Add to cart</button>
</div>

<a href="/cart">Cart (<span data-simplecart-count>0</span>)</a>
```

![Add to cart on a product listing](/screenshots/uTPro.Feature.SimpleCart/add-to-cart.png)

---

## JavaScript API

The helper also exposes a small programmatic API on `window.uTProSimpleCart` for SPA or custom flows:

```javascript
uTProSimpleCart.get();                       // current cart
uTProSimpleCart.add(productKey, quantity, sku);
uTProSimpleCart.update(productKey, quantity); // absolute quantity
uTProSimpleCart.remove(productKey);
uTProSimpleCart.clear();
```

After every change a `simplecart:changed` DOM event is dispatched with the updated cart, so a custom UI can update without a page reload:

```javascript
document.addEventListener("simplecart:changed", function (e) {
    console.log("New total quantity:", e.detail && e.detail.totalQuantity);
});
```

---

## Overriding the view

When installed via NuGet the cart view is compiled into the package. To customize the markup, create a file at the same path in your web project:

```
YourWebProject/
  Views/Shared/Components/Cart/
    Default.cshtml        ← overrides the cart layout
```

---

## Styling

The shipped stylesheet lives at `/uTPro/simplecart/simplecart.css`. Include it, or skip it and style the cart with your own CSS — the markup uses plain, predictable class names you can target from your theme.
