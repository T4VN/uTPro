---
layout: default
title: "API Reference – Search Plus"
description: "REST API endpoints for managing synonym groups in Search Plus."
permalink: "/uTPro.Feature.SearchPlus/api-reference/"
feature: true
feature_name: "Search Plus"
---

[← Back to Search Plus](/uTPro.Feature.SearchPlus/)

# API Reference

All endpoints are authenticated and require **Settings section access** in the Umbraco backoffice.

Base path: `/umbraco/management/api/v1/utpro/search-plus/synonyms`

---

## List all groups

```
GET /umbraco/management/api/v1/utpro/search-plus/synonyms
```

**Response:** `200 OK`

```json
[
  {
    "id": "a1b2c3d4-0001-4000-8000-000000000001",
    "terms": ["máy tính", "laptop", "pc", "computer", "notebook"]
  },
  {
    "id": "a1b2c3d4-0001-4000-8000-000000000002",
    "terms": ["điện thoại", "smartphone", "mobile", "phone", "di động"]
  }
]
```

---

## Create a group

```
POST /umbraco/management/api/v1/utpro/search-plus/synonyms
Content-Type: application/json

{
  "terms": ["máy tính", "laptop", "pc", "computer"]
}
```

**Response:** `201 Created`

```json
{
  "id": "generated-guid",
  "terms": ["máy tính", "laptop", "pc", "computer"]
}
```

**Validation:** Minimum 2 terms required.

---

## Update a group

```
PUT /umbraco/management/api/v1/utpro/search-plus/synonyms/{id}
Content-Type: application/json

{
  "terms": ["máy tính", "laptop", "pc", "computer", "notebook"]
}
```

**Response:** `200 OK` or `404 Not Found`

---

## Delete a group

```
DELETE /umbraco/management/api/v1/utpro/search-plus/synonyms/{id}
```

**Response:** `200 OK` or `404 Not Found`

---

## Test expansion

```
GET /umbraco/management/api/v1/utpro/search-plus/synonyms/expand?term=laptop
```

**Response:** `200 OK`

```json
{
  "term": "laptop",
  "expanded": ["máy tính", "laptop", "pc", "computer", "notebook"]
}
```

If no synonyms are found, returns the original term:

```json
{
  "term": "unknown",
  "expanded": ["unknown"]
}
```

The expansion is **diacritics-insensitive**: `?term=cong ty` will match the "công ty" group.

---

## Suggest groups (partial match)

```
GET /umbraco/management/api/v1/utpro/search-plus/synonyms/suggest?term=may
```

**Response:** `200 OK`

```json
[
  {
    "id": "a1b2c3d4-0001-4000-8000-000000000001",
    "terms": ["máy tính", "laptop", "pc", "computer", "notebook"]
  },
  {
    "id": "a1b2c3d4-0001-4000-8000-000000000003",
    "terms": ["máy tính bảng", "tablet", "ipad"]
  }
]
```

Returns groups where any term partially contains the query (diacritics-insensitive). Maximum 10 results.

---

## Authentication

All endpoints require a valid Umbraco backoffice authentication token. The API is scoped to the **Settings section** — users must have Settings access to call these endpoints.

Use the standard Umbraco Management API authentication flow (Bearer token from `/umbraco/management/api/v1/security/back-office/authorize`).

---

## Swagger

When running in development, the API is documented under the **utpro-search-plus** definition in the Swagger UI at `/umbraco/swagger`.
