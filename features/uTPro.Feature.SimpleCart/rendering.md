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

The component reads the live cart (names and prices resolved server-side in the current culture) and renders the lines, quantities and subtotal. The shipped cart view **includes the stylesheet and script itself**, so on a page that renders the cart component you don't need to add them manually.

![Cart rendered on the front-end](/screenshots/uTPro.Feature.SimpleCart/cart-render.png)

---

## Product card partial

The package also ships a reusable **product card** partial with a built-in add-to-cart button. Pass it a catalog `Product` (see [Product Catalog](catalog/)):

```razor
@await Html.PartialAsync("SimpleCart/_ProductCard", product)
```

It renders the product image (if any), a linked name, the formatted price, and either an **Add to cart** button or an **Unavailable** label based on the product's availability. Copy it into your own site at `Views/Partials/SimpleCart/_ProductCard.cshtml` to theme it.

---

## Wiring buttons with `data-*` attributes

On pages that use add-to-cart buttons but **don't** render the cart component, include the shipped script once, then annotate elements with `data-simplecart-*` attributes — the helper binds them automatically (no inline JavaScript needed):

```html
<script src="/uTPro/simplecart/simplecart.js" defer></script>
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

The shipped stylesheet lives at `/uTPro/simplecart/simplecart.css` (the cart view links it automatically). Skip it and use your own CSS if you prefer — the markup uses predictable, BEM-style class names you can target from your theme:

| Class | Element |
|---|---|
| `.utpro-cart` | Cart container |
| `.utpro-cart__empty` | "Your cart is empty" message |
| `.utpro-cart__table` | Cart line-items table |
| `.utpro-cart__qty` | Quantity input |
| `.utpro-cart__remove` | Remove-line button |
| `.utpro-cart__clear` | Clear-cart button |
| `.utpro-product-card` | Product card container |
| `.utpro-product-card__image` | Product image link |
| `.utpro-product-card__name` | Product name |
| `.utpro-product-card__price` | Product price |
| `.utpro-product-card__add` | Add-to-cart button |
| `.utpro-product-card__unavailable` | Unavailable label |
