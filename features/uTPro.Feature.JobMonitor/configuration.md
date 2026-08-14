---
layout: default
title: "Configuration – Job Monitor"
description: "Configure storage mode, history capacity and discovery caching for the Job Monitor package."
permalink: "/uTPro.Feature.JobMonitor/configuration/"
feature: true
feature_name: "Job Monitor"
---

# Configuration

[← Back to Job Monitor](/uTPro.Feature.JobMonitor/)

All settings are optional and live under the `uTPro:Feature:JobMonitor` section in
`appsettings.json`. The defaults give a working, read-only, in-memory monitor with zero config.

```json
{
  "uTPro": {
    "Feature": {
      "JobMonitor": {
        "Storage": "InMemory",
        "HistoryCapacity": 50,
        "DiscoveryCacheSeconds": 30
      }
    }
  }
}
```

## Keys

### `Storage`

| Value | Behaviour |
|---|---|
| `InMemory` *(default)* | Execution records are kept in a bounded per-job ring buffer for the lifetime of the process. History **resets on restart** and reflects **only the current node**. |
| `Durable` | Records are persisted to the Umbraco database (`uTProJobExecution` table). History **survives restarts** and is **shared across load-balanced nodes**. A migration creates the table on startup. |

See [Telemetry & Storage](../telemetry-and-storage/) for details, including PostgreSQL.

### `HistoryCapacity`

Maximum number of execution records retained **per job**.

- In-memory: the ring-buffer size (older records are dropped).
- Durable: the number of rows returned by the history endpoint.

Values `<= 0` fall back to the default of `50`.

### `DiscoveryCacheSeconds`

How long (in seconds) a discovery result is cached before the package re-enumerates registered
jobs. Discovery resolves DI registrations and reads timing, so caching avoids repeating that work
on every dashboard request.

- `30` *(default)* — cache for 30 seconds.
- `0` — disable caching (always enumerate fresh).

> New jobs are registered at application start, so a short cache never hides them beyond a restart.

## Notes

- Configuration is read at composition time to decide which storage backend to register, and is
  also bound to `IOptions<JobMonitorOptions>` for runtime values.
- Changing `Storage` between `InMemory` and `Durable` does not migrate existing records; durable
  history begins accumulating once durable storage is enabled.
