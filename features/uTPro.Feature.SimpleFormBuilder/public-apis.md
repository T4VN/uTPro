---
layout: default
title: "Public APIs – Simple Form Builder"
description: "REST APIs for submitting forms, rendering definitions, retrieving entries, file downloads, and extending the submission pipeline."
permalink: "/uTPro.Feature.SimpleFormBuilder/public-apis/"
feature: true
feature_name: "Simple Form Builder"
---

# Public APIs & Import/Export

[← Back to Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)

REST endpoints for headless or hybrid use cases.

---

## Submit a form (always available)

```http
POST /api/utpro/simple-form/submit
Content-Type: application/json

{
  "alias": "contact-us",
  "data": { "name": "Jane", "email": "jane@example.com", "message": "Hello!" }
}
```

### With file uploads (multipart)

```http
POST /api/utpro/simple-form/submit
Content-Type: multipart/form-data

alias=contact-us
data={"name":"Jane","email":"jane@example.com"}
file:cv=<binary>
```

---

## Get form definition (opt-in per form)

```http
GET /api/utpro/simple-form/render/{alias}
```

Enable via form settings: **Enable Render API**.

## Get entries (opt-in per form)

```http
GET /api/utpro/simple-form/entries/{alias}?skip=0&take=20
```

---

## Backoffice-only endpoints

**Download uploaded file:**

```http
GET /umbraco/management/api/v1/utpro/simple-form/entry-file?entryId={id}&fieldName={name}
```

**Export entries as ZIP:**

```http
POST /umbraco/management/api/v1/utpro/simple-form/export-entries-zip
Content-Type: application/json

{ "formId": 1, "search": null, "dateFrom": null, "dateTo": null }
```

---

## Submission pipeline (`IFormSubmissionHandler`)

Every submission runs through handlers before storage. Register your own:

```csharp
public sealed class TurnstileHandler : IFormSubmissionHandler
{
    public int Order => 100;

    public async Task<FormSubmissionResult> HandleAsync(
        FormSubmissionContext context, CancellationToken ct)
    {
        var token = context.Data.TryGetValue("cf-turnstile-response", out var t) ? t : null;
        if (string.IsNullOrEmpty(token))
            return FormSubmissionResult.Reject("Captcha verification failed.");
        return FormSubmissionResult.Continue;
    }
}

// Register in DI
builder.Services.AddTransient<IFormSubmissionHandler, TurnstileHandler>();
```

---

## Import / Export

![Import / Export](/screenshots/uTPro.Feature.SimpleFormBuilder/import-export.png)

- **Export** — downloads `{alias}.form.json` (definition only, no entries)
- **Import** — creates a new form; duplicate aliases auto-suffixed
