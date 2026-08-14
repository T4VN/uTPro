---
layout: default
title: "Synonym Management – Search Plus"
description: "Create, edit, and manage synonym groups for expanded search queries in Search Plus."
permalink: "/uTPro.Feature.SearchPlus/synonym-management/"
feature: true
feature_name: "Search Plus"
---

[← Back to Search Plus](/uTPro.Feature.SearchPlus/)

# Synonym Management

## Overview

Synonym groups define sets of equivalent terms. When a visitor searches for any term in a group, the query expands to include all terms in that group.

**Example:** Group ["máy tính", "laptop", "pc", "computer", "notebook"]
- User searches "laptop" → finds content containing any of: máy tính, laptop, pc, computer, notebook

## Accessing the UI

1. Open the Umbraco backoffice
2. Go to **Settings** (left sidebar)
3. Under the **uTPro Feature** group, click **Search Plus**

## Creating a group

1. Click the **+ Add Group** button (top right)
2. Enter terms separated by commas — minimum 2 terms per group
3. Press Enter or click **Add Group**

**Example input:** `máy tính, laptop, pc, computer, notebook`

## Editing a group

1. Find the group in the list (use the search bar to filter)
2. Click **Edit** on the group card
3. Modify the comma-separated terms
4. Press Enter or click **Save**

## Deleting a group

1. Find the group in the list
2. Click **Delete**
3. Confirm the deletion

## Searching and filtering

The search bar at the top serves dual purpose:

- **Test expansion** — type any term to see if it has synonyms configured and what it expands to
- **Filter groups** — the list below filters to show only groups containing matching terms

The search is **diacritics-insensitive**: typing "may" will find groups containing "máy".

## Suggestions

When you search a term that has no exact synonym match, the UI shows:

- **"No synonyms configured"** message
- **Similar groups** — groups that contain terms partially matching your search
- **Quick add button** — pre-fills a new group form with the searched term

## Pre-loaded groups

On first install, 25 common groups are seeded:

| Category | Examples |
|---|---|
| Technology | máy tính/laptop/pc, điện thoại/smartphone/mobile |
| E-commerce | mua/order/purchase, giá/price/cost |
| Contact | liên hệ/contact/support, địa chỉ/address/location |
| Content | tin tức/news/article, hướng dẫn/guide/tutorial |
| Services | dịch vụ/service/solution, sản phẩm/product/item |
| Delivery | giao hàng/delivery/shipping, thanh toán/payment |
| Organization | công ty/company/business, tuyển dụng/recruitment/career |

These can be edited or deleted as needed.

## Tips

- Keep groups focused — terms in a group should be genuinely interchangeable in meaning
- Include both Vietnamese and English equivalents for bilingual sites
- Changes take effect immediately — no index rebuild needed
- Use the search bar to verify your groups work as expected before deploying
