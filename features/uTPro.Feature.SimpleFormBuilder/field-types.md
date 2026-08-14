---
layout: default
title: "Field Types – Simple Form Builder"
description: "19 built-in field types and how to create custom field types with custom settings for uTPro Simple Form Builder."
permalink: "/uTPro.Feature.SimpleFormBuilder/field-types/"
feature: true
feature_name: "Simple Form Builder"
---

# Field Types

[← Back to Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)

![Field type picker](/screenshots/uTPro.Feature.SimpleFormBuilder/field-types.png)

## Built-in field types

Ships with 19 field types out of the box:

| Type | Description |
|---|---|
| `text` | Single-line text input |
| `email` | Email input |
| `tel` | Phone number |
| `number` | Numeric input |
| `url` | URL input |
| `password` | Password input (auto-encrypted at rest) |
| `date` | Date picker |
| `file` | File upload |
| `textarea` | Multi-line text |
| `select` | Dropdown menu |
| `checkbox` | Single or multi-checkbox |
| `radio` | Radio button group |
| `hidden` | Hidden field |
| `accept` | Terms & conditions checkbox with link |
| `range` | Slider with min/max/step |
| `color` | Color picker |
| `time` | Time picker with min/max |
| `div` | HTML content block (not an input) |
| `step` | Visual step divider |

### File upload field

| Setting | Effect |
|---|---|
| **Accept** | Comma-separated allowed extensions (e.g. `.pdf,.jpg`) |
| **Max MB** | Maximum file size in megabytes |

Uploaded files are stored outside `wwwroot` and served via authenticated endpoint only.

---

## Adding a Custom Field Type

### Step 1 — Create a Razor partial

```
Views/Partials/uTProSimpleForm/Fields/{yourType}.cshtml
```

```razor
@using uTPro.Feature.SimpleFormBuilder.Helpers
@model uTPro.Feature.SimpleFormBuilder.Models.FormFieldViewModel
@{ var h = new FieldHelper(Model, ViewData); }

@h.Label()
<input type="text" id="@h.FieldId" name="@h.Name"
       placeholder="@Model.Placeholder"
       value="@Model.DefaultValue"
       @h.RequiredAttr()
       @h.DataMsgAttr() />
@h.Error()
```

### Step 2 — Register in DI

```csharp
public class MyFormFieldsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AdduTProSimpleFormFieldType("yourType", "Your Type Label");
}
```

### Step 3 (optional) — Custom settings

```csharp
builder.AdduTProSimpleFormFieldType("star-rating", "Star Rating",
    new SimpleFormFieldAttribute("max", "Max Stars", placeholder: "5", inputType: "number"));
```

Read in partial: `h.Attr("max", "5")`

---

## FieldHelper toolkit

| Call | Renders |
|---|---|
| `h.FieldId` | Unique HTML id |
| `h.Name` | Field name for submission |
| `h.Label()` | `<label>` with asterisk if required |
| `h.Error()` | Validation message span |
| `h.RequiredAttr()` | `required` attribute |
| `h.PatternAttr()` | `pattern="..."` attribute |
| `h.DataMsgAttr()` | `data-msg="..."` with dictionary tokens |
| `h.Attr("key", "default")` | Read from field attributes |
