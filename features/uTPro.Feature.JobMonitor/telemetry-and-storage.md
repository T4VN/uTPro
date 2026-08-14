---
layout: default
title: "Telemetry & Storage – Job Monitor"
description: "How execution telemetry is captured, stored in-memory or durably, and shared across load-balanced nodes."
permalink: "/uTPro.Feature.JobMonitor/telemetry-and-storage/"
feature: true
feature_name: "Job Monitor"
---

# Telemetry & Storage

[← Back to Job Monitor](/uTPro.Feature.JobMonitor/)

Umbraco does not persist per-job execution history. Background Jobs Monitor captures it by wrapping
job execution and recording each run through an `IExecutionStore`. Two implementations ship with the
package, selected by the [`Storage`](../configuration/#storage) setting.

## What is recorded

Each execution produces an `ExecutionRecord`:

| Field | Notes |
|---|---|
| Start (UTC) | When the run began. |
| End (UTC) | When it finished (null while running). |
| Duration | `End - Start`. |
| Outcome | `Success` or `Failure` (null while running). |
| Source | `Scheduled` or `Manual`. |
| Initiating user | Set for manual runs (the backoffice user id). |

Records are immutable: a "running" record is replaced by a completed copy when the run finishes, so
the dashboard always reads a consistent snapshot.

## In-memory store (default)

- Bounded per-job ring buffer (`HistoryCapacity`, default 50).
- Fast, zero setup, no schema.
- **Resets when the application restarts.**
- Reflects **only the current node** — not shared across a load-balanced cluster.

The dashboard shows notices reflecting these limitations when in-memory storage is active.

## Durable store

Set `Storage` to `Durable` to persist telemetry to the Umbraco database:

```json
{ "uTPro": { "Feature": { "JobMonitor": { "Storage": "Durable" } } } }
```

- A state-keyed migration creates the **`uTProJobExecution`** table on startup (once per database).
- History **survives restarts** and is **shared across load-balanced nodes**.
- The migration is only wired when durable storage is enabled.

### The `uTProJobExecution` table

| Column | Type | Notes |
|---|---|---|
| `id` | identity | Primary key. |
| `RunId` | string(36) | Correlates the running and completed record (unique index). |
| `JobKey` | string(400) | Full type name (indexed). |
| `StartUtc` | datetime | UTC. |
| `EndUtc` | datetime? | UTC, null while running. |
| `DurationMs` | bigint? | Duration in milliseconds. |
| `Outcome` | string(20)? | `Success` / `Failure`. |
| `Source` | string(20) | `Scheduled` / `Manual`. |
| `InitiatingUserId` | int? | Set for manual runs. |

Timestamps are stored as UTC `DateTime` and durations as milliseconds for portability across
databases (which have inconsistent `DateTimeOffset` support).

## Cross-database support

Durable storage runs on **SQL Server**, **SQLite** and **PostgreSQL**. All access uses NPoco
strongly-typed queries, so table and column identifiers are quoted per provider (PostgreSQL folds
unquoted identifiers to lower-case). The schema is created via Umbraco's provider-agnostic migration
builder — the same approach used by `uTPro.Feature.AuditLog` and `uTPro.Feature.SimpleFormBuilder`.

### PostgreSQL

Install the community provider [`Our.Umbraco.PostgreSql`](https://github.com/idseefeld/PostgreSqlForUmbraco),
enable it in `Program.cs` (`.AddUmbracoPostgreSqlSupport()`), and set the provider name (`Npgsql2`)
in the connection string. Durable telemetry then works with no further changes.

## Load-balancing

- With **in-memory** storage, each node tracks only its own executions; the dashboard says history
  is not shared across nodes.
- With **durable** storage on a shared database, all nodes read and write the same history.
- Scheduled jobs only run on a **Single** node or the **SchedulingPublisher**; on a Subscriber the
  dashboard warns that scheduled jobs do not run there.
