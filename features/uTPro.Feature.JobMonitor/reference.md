---
layout: default
title: "Reference – Job Monitor"
description: "Project structure, Management API endpoints, discovery internals, services and database schema reference."
permalink: "/uTPro.Feature.JobMonitor/reference/"
feature: true
feature_name: "Job Monitor"
---

# Reference

[← Back to Job Monitor](/uTPro.Feature.JobMonitor/)

## Project structure

```
uTPro.Feature.JobMonitor/
├── src/uTPro.Feature.JobMonitor/            # the shipped library (net9.0;net10.0)
│   ├── Controllers/JobMonitorApiController.cs
│   ├── Migrations/                          # durable-storage schema + handler
│   │   ├── InitJobMonitor.cs
│   │   └── JobMonitorMigrationHandler.cs
│   ├── Models/                              # enums, Available<T>, DTOs, view models, options
│   ├── Services/
│   │   ├── JobMonitorComposer.cs
│   │   ├── JobDiscoveryService.cs / ITimingReaderService...
│   │   ├── ExecutionTracker.cs
│   │   ├── InMemoryExecutionStore.cs / DurableExecutionStore.cs
│   │   ├── NextRunEstimator.cs
│   │   └── ManualTriggerService.cs
│   └── wwwroot/                             # backoffice dashboard (Lit) + umbraco-package.json
└── src/uTPro.Feature.JobMonitor.TestSite/   # runnable Umbraco test site with example jobs
```

## Management API

Base route: `/umbraco/management/api/v1/utpro/job-monitor`. All endpoints require Settings access.

| Method | Route | Returns |
|---|---|---|
| `GET` | `jobs` | `JobListResponse` — jobs, count, current server role, whether the role runs jobs, storage mode. |
| `GET` | `jobs/{key}/history` | Recent `ExecutionRecordViewModel[]` for one job. |
| `POST` | `jobs/{key}/run` | `202` started / `409` already-running / `409` role-not-permitted / `404` not found. |

## Discovery internals

- Modern jobs are found by scanning registered `IHostedService`s for
  `RecurringBackgroundJobHostedService<T>` wrappers and unwrapping the inner
  `IRecurringBackgroundJob` (also supplemented by any directly-registered `IRecurringBackgroundJob`).
- Legacy jobs are the `RecurringHostedServiceBase` derivations that are not modern-job wrappers.
- Plain `IHostedService`s are excluded.
- Each job gets a stable `JobKey` (full type name); results are de-duplicated and cached briefly.

## Services & lifetimes

| Service | Lifetime | Role |
|---|---|---|
| `IExecutionStore` | Singleton | `InMemoryExecutionStore` or `DurableExecutionStore` (per config). |
| `IExecutionTracker` | Singleton | Records start/end telemetry through the store. |
| `IJobDiscoveryService` | Singleton | Enumerates + classifies jobs (short-lived cache). |
| `ITimingReaderService` | Singleton | Reads timing tolerantly (never throws). |
| `INextRunEstimator` | Singleton | `lastStart + period`. |
| `IManualTriggerService` | Singleton | Overlap/role-guarded on-demand run. |

## Database schema (durable storage only)

See [Telemetry & Storage → the `uTProJobExecution` table](../telemetry-and-storage/#the-utprojobexecution-table).

## Configuration

See [Configuration](../configuration/) — section `uTPro:Feature:JobMonitor`.
