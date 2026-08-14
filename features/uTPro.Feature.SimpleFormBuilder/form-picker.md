---
layout: default
title: "Form Picker – Simple Form Builder"
description: "Let editors pick forms from content properties using the uTPro Form Picker data type."
permalink: "/uTPro.Feature.SimpleFormBuilder/form-picker/"
feature: true
feature_name: "Simple Form Builder"
---

# Picking a Form from Content (Form Picker)

[← Back to Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)

Instead of hard-coding the alias in a template, editors can choose a form from a Content property.

The package ships a ready-to-use **uTPro Form Picker** data type (created automatically on startup).

1. Add a property using the **uTPro Form Picker** data type to any Document Type.
2. When editing content, pick a form from the dropdown — it stores the form's **alias**.

![Form Picker on a content property](/screenshots/uTPro.Feature.SimpleFormBuilder/form-picker-content.png)

3. In the template, read the alias:

```razor
@{
    var formAlias = Model.Value<string>("form");
}
@if (!string.IsNullOrWhiteSpace(formAlias))
{
    @await Component.InvokeAsync("uTProSimpleForm", new { alias = formAlias })
}
```

The dropdown only lists forms whose **Show in content picker** toggle is on.

## Restricting a picker to specific forms

![Form Picker data type — Allowed forms setting](/screenshots/uTPro.Feature.SimpleFormBuilder/form-picker.png)

When creating a **uTPro Form Picker** data type, tick the forms this picker should offer:

- **Leave empty** → every form with *Show in content picker* on
- **Tick specific forms** → only those (still requires *Show in content picker*)

## Publish validation

If a content item stores a form that later becomes unavailable, the picker shows it in red as **"— not available"**. Saving/publishing is blocked until the editor chooses another form or clears it.
