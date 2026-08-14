---
layout: default
title: "Rendering a Form – Simple Form Builder"
description: "Render uTPro forms on the front-end with ViewComponent, custom templates, multi-language support, and JavaScript hooks."
permalink: "/uTPro.Feature.SimpleFormBuilder/rendering/"
feature: true
feature_name: "Simple Form Builder"
---

# Rendering a Form

[← Back to Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)

Drop this into any Razor view or block component:

```razor
@await Component.InvokeAsync("uTProSimpleForm", new { alias = "contact-us" })
```

The `alias` matches the form you created in the **uTPro Form** section.

![Rendered form on the front-end](/screenshots/uTPro.Feature.SimpleFormBuilder/form-render.png)

## Optional parameters

```razor
@await Component.InvokeAsync("uTProSimpleForm", new {
    alias = "contact-us",
    template = "MyLayout",       // use a custom Razor template
    cssClass = "my-form",        // add a CSS class to the <form> tag
    submitBtnText = "Send",      // change the submit button text
    showReset = true,            // show or hide the reset button
    resetBtnText = "Clear"       // change the reset button text
})
```

## Template resolution order

1. `Views/Partials/uTProSimpleForm/{template}.cshtml` — if a `template` parameter was passed
2. `Views/Partials/uTProSimpleForm/{alias}.cshtml` — a view named after the form alias
3. `Views/Partials/uTProSimpleForm/Default.cshtml` — the built-in default

## Overriding Views (NuGet users)

When installed via NuGet, all Razor views are compiled into the package DLL. To customize any view, create a file at the same path in your web project:

```
YourWebProject/
  Views/Partials/uTProSimpleForm/
    Default.cshtml                  ← overrides the form layout
    Fields/
      textarea.cshtml               ← overrides just the textarea field
      star-rating.cshtml            ← adds a brand new field type
```

## Multi-language (dictionary tokens)

Any user-facing text on a form can be translated per culture using the `{% raw %}{{ DictionaryKey }}{% endraw %}` token syntax. At render time each token is replaced with the matching **Umbraco Dictionary** value for the current culture.

Supported fields: Label, Placeholder, Options text, Validation message, Group title, Button text, Success message, Accept field text, Step divider title, Content block.

## JavaScript Hooks

```javascript
// Runs before submission. Return false to cancel.
window.__uTProFormBeforeSubmit = async function (alias, data, formElement) {
    if (alias === 'contact-us') {
        data.source = 'homepage';
    }
    return data;
};

// Runs after a successful submission.
window.__uTProFormAfterSubmit = function (alias, success, result) {
    console.log('Submitted:', alias, result.message);
};
```

## Styling / CSS classes

The rendered form uses the `uTProForm` class prefix. Override them in your own CSS:

| Class | Element |
|---|---|
| `.uTProForm` | The `<form>` element |
| `.uTProForm-group` | Fieldset group |
| `.uTProForm-error` | Inline validation message |
| `.uTProForm-message` | Submit result banner |
| `.uTProForm-success` / `.uTProForm-fail` | Success/fail states |
