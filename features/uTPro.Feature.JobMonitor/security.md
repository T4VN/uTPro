---
layout: default
title: "Security & Permissions – Job Monitor"
description: "Authorization model, read-only design and reflection safety of the Job Monitor package."
permalink: "/uTPro.Feature.JobMonitor/security/"
feature: true
feature_name: "Job Monitor"
---

# Security & Permissions

[← Back to Job Monitor](/uTPro.Feature.JobMonitor/)

Background Jobs Monitor is authenticated backoffice-only and read-only by default.

## Authorization model

Every Management API endpoint is decorated with the **Settings section** policy
(`AuthorizationPolicies.SectionAccessSettings`) at the controller level, so authorization applies to
every action — an endpoint is denied by default rather than left open.

| Situation | Result |
|---|---|
| Unauthenticated request | `401 Unauthorized` |
| Authenticated but no Settings access | `403 Forbidden` |
| Authenticated with Settings access | Allowed |

Grant the **Settings** section to a user group under **Users → User groups → _group_ → Sections**.

## Read-only by default

The only state change the package makes is the manual **Run now** trigger. All other endpoints only
read discovery data and telemetry. See [Manual Trigger](../manual-trigger/).

## Reflection is metadata-only

- Legacy timing values are read from non-public fields (`_period` / `_delay`) — reading only.
- Modern jobs are unwrapped from their hosted-service wrapper to read timing and to invoke
  `RunJobAsync` **only** in response to an authorized Run now request.
- Reflection is never driven by user input, and no job type is instantiated from a client-supplied
  string — the job to run is resolved from the server-discovered set by key.

## No secrets, no outbound calls

The package handles no secrets and makes no outbound network calls; all data is local to the site.

## Accountability

Manual runs record the initiating backoffice user id. With [durable storage](../telemetry-and-storage/)
enabled, this is persisted for later review.
