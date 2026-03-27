# SimpleForm Custom Field Types

To add a custom field type (e.g. Cloudflare Turnstile, encrypted field, rating stars, etc.):

## 1. Create a partial view

Create a `.cshtml` file in this folder named after your field type:

```
Fields/turnstile.cshtml
Fields/rating.cshtml
Fields/encrypted.cshtml
```

## 2. Partial view receives

- `@model uTPro.Feature.SimpleForm.Models.FormFieldViewModel`
- `ViewData["FormId"]` — the form element ID (string)
- `Model.Attributes` — Dictionary<string,string> for custom config (e.g. siteKey, theme)

## 3. Example: Cloudflare Turnstile

Create `Fields/turnstile.cshtml`:

```html
@model uTPro.Feature.SimpleForm.Models.FormFieldViewModel
@{ var siteKey = Model.Attributes?.GetValueOrDefault("siteKey") ?? ""; }

<div class="cf-turnstile" data-sitekey="@siteKey" data-callback="onTurnstile_@Model.Name"></div>
<input type="hidden" name="@Model.Name" id="sf-@Model.Name" />
<span class="sf-error" data-for="@Model.Name"></span>

<script>
function onTurnstile_@Model.Name(token) {
    document.getElementById('sf-@Model.Name').value = token;
}
</script>
```

Then in backoffice Form Builder, add a field with:
- Type: `turnstile`
- Attributes: `siteKey` = `0x4AAAAAAA...`

## 4. JS Hooks

You can hook into form submission from any custom field:

```js
// Called before submit — return false to cancel, or object to merge extra data
window.__sfBeforeSubmit = async function(alias, data, formEl) {
    // e.g. validate turnstile token
    if (!data['cf-turnstile']) return false;
    return data;
};

// Called after successful submit
window.__sfAfterSubmit = function(alias, success, result) {
    // e.g. reset turnstile widget
};
```

## 5. Register in backoffice (optional)

To make your custom type appear in the backoffice field type dropdown,
add it via the `/field-types` API endpoint in `SimpleFormApiController.cs`.
