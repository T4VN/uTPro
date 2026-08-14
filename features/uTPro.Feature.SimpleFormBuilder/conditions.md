---
layout: default
title: "Conditions – Simple Form Builder"
description: "Conditional field visibility – show or hide fields dynamically based on another field's value in uTPro Simple Form Builder."
permalink: "/uTPro.Feature.SimpleFormBuilder/conditions/"
feature: true
feature_name: "Simple Form Builder"
---

# Conditional Field Visibility

[← Back to Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/)

*Available since v5.0.0*

Fields can show or hide themselves at runtime based on the value of other fields in the same form.

---

## Setting up conditions (Backoffice)

1. Open **Field Settings** for the target field
2. Scroll to the **Conditions** section
3. Toggle **On**
4. Choose **Show** or **Hide** action
5. Choose match logic: **all** or **any**
6. Add rules

### Operators

| Operator | Description |
|---|---|
| `is` | Exact match |
| `is not` | Does not match |
| `contains` | Contains substring |
| `does not contain` | Does not contain |
| `starts with` | Starts with target |
| `ends with` | Ends with target |
| `is greater than` | Numeric: field > target |
| `is less than` | Numeric: field < target |
| `is empty` | No value |
| `is not empty` | Has any value |

### Smart value picker

When the referenced field has predefined options (Dropdown, Radio, Checkbox), the **Value** column renders as a dropdown populated with those options.

---

## Runtime behaviour (Frontend)

- Conditions are evaluated **client-side** in real time
- Hidden fields are:
  - **Skipped during validation**
  - **Excluded from submitted data**
- On page load, conditions are evaluated once for correct initial state

---

## Data model

Stored inside the field's JSON in `GroupsJson` — no database migration required:

```json
{
  "conditions": {
    "enabled": true,
    "actionType": "show",
    "logicType": "all",
    "rules": [
      { "field": "queryType", "operator": "is", "value": "contract" }
    ]
  }
}
```

---

## Example: branching by query type

| Field | Condition |
|---|---|
| Contract Helptext | **Show** if `queryType` **is** `contract` |
| NID Helptext | **Show** if `queryType` **is** `nid` |
| Card Helptext | **Show** if `queryType` **is** `card` |

Switching the dropdown instantly shows the appropriate help text.
