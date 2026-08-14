---
layout: default
title: "uTPro.Feature.JobMonitor"
description: "Background Jobs Monitor for Umbraco – auto-discover recurring jobs, view timing, execution telemetry, and trigger manual runs."
permalink: "/uTPro.Feature.JobMonitor/"
feature: true
feature_name: "Job Monitor"
---

# uTPro Background Jobs Monitor for Umbraco

A read-and-trigger management UI for Umbraco recurring background jobs. Surfaces every recurring job under a **Settings** dashboard — with timing parameters, execution telemetry, estimated next run, server-role awareness, and **Run now** action.

Works with **Umbraco 16, 17 and 18**. Optional durable telemetry on **SQL Server**, **SQLite** and **PostgreSQL**.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.JobMonitor.svg)](https://www.nuget.org/packages/uTPro.Feature.JobMonitor)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.JobMonitor.svg)](https://www.nuget.org/packages/uTPro.Feature.JobMonitor)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.jobmonitor)

![uTPro Job Monitor](/screenshots/uTPro.Feature.JobMonitor/dashboard.png)

---

## Features

- **Auto-discovery** of every recurring background job
- **Timing parameters** — period, delay, server roles
- **Execution telemetry** — last run, duration, outcome
- **Estimated next run**
- **Manual trigger ("Run now")** — overlap guard, role aware
- **Load-balancing aware**
- **Configurable storage** — in-memory or durable DB

---

## Quick Start

```bash
dotnet add package uTPro.Feature.JobMonitor
```

Open **Settings → Background Jobs Monitor**.

---

## Configuration

| Key | Default | Description |
|---|---|---|
| `Storage` | `InMemory` | `InMemory` or `Durable` |
| `HistoryCapacity` | `50` | Records per job |
| `DiscoveryCacheSeconds` | `30` | Cache duration |

---

## Documentation

| Guide | Description |
|---|---|
| [Getting Started](getting-started/) | Install, columns, backoffice location |
| [Configuration](configuration/) | All appsettings keys |
| [Telemetry & Storage](telemetry-and-storage/) | In-memory vs durable, load-balancing |
| [Manual Trigger](manual-trigger/) | Run now, overlap guards |
| [Security](security/) | Authorization model |
| [Reference](reference/) | API endpoints, DB schema |

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](https://github.com/T4VN/uTPro.Feature.JobMonitor/blob/main/LICENSE.txt).

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.JobMonitor) · [GitHub](https://github.com/T4VN/uTPro.Feature.JobMonitor) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.jobmonitor)
