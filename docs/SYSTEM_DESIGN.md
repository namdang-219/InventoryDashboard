# System Design Document: Intelligent Inventory Dashboard (IID)

**System:** Intelligent Inventory Dashboard  
**Domain:** Automotive Supply & Dealership Inventory Management  
**Author / Solutions Architect:** Nam Dang  
**Target Environment:** Docker Engine & Docker Compose (with Azure SQL Database / MSSQL 2022)  
**Observability Stack:** OpenTelemetry & OpenObserve  
**Date:** September 2026  

---

## Executive Summary

The **Intelligent Inventory Dashboard (IID)** is an enterprise supply-domain platform engineered to provide automotive dealership general managers, sales executives, and inventory controllers with real-time, decision-grade visibility and actionable workflow controls over their vehicle assets.

### The Business Challenge: Carrying Costs & Lot Depreciation
In modern automotive retail, vehicle inventory represents a dealership's largest working capital investment. However, physical vehicles parked on the lot are rapidly depreciating assets subject to severe **floor-plan financing costs** (typically running between **$25 to $40 per vehicle per day** in floor-plan interest, lot insurance, physical wear, and lot space opportunity costs).

When a vehicle surpasses **90 calendar days on the lot ("Aging Stock")**, its probability of retail sale drops precipitously. Retail gross margins erode by 10% to 25%, turning potentially profitable units into capital sinks. Traditional Dealership Management Systems (DMS) fail to solve this challenge due to key operational gaps:
- **Siloed & Batch-Oriented Architecture:** Critical inventory data is buried in overnight batch reports or static tabular exports reviewed only during month-end accounting reconciliations.
- **Lack of Decision-Grade Prioritization:** Inventory managers cannot easily cross-correlate lot tenure with real-time market demand metrics.
- **Disconnected Operational Remediation:** Identifying an aging vehicle does not trigger a collaborative, trackable workflow to remediate pricing or route the unit to wholesale auction.

The Intelligent Inventory Dashboard directly bridges the gap between real-time data streaming and executive decision-making across three primary business workflows:

---

### 1. Executive Inventory & Portfolio Cockpit
The primary executive interface delivers an instant, real-time health assessment of total dealership working capital, lot turnaround velocity, and capital risk exposure.

![Executive Inventory Dashboard & Portfolio Overview](./images/dashboard.png)

- **Total Working Capital Visibility:** Real-time valuation of the dealership's asset base (e.g., **$3,146,400** portfolio value across 72 units at Silverstone Motor Cars, with an average unit valuation of **$46,961**).
- **Turnaround Velocity Tracking:** Instant measurement of the lot's overall turnaround velocity (**63 days average on lot**) against targeted inventory turnover benchmarks.
- **Critical Aging Alerts:** Instant visibility into capital at risk—flagging **12 units (>90 days)** requiring immediate managerial intervention.
- **Action Center & Smart Recommendations:** Proactively prioritizes at-risk units (e.g., vehicles with 268+ days on lot paired with low demand scores) alongside monthly sales velocity trends, powertrain mix distributions (64% Petrol, 18% Hybrid, 15% Electric), and live status breakdowns (50 Available, 15 Pending, 7 Sold).

---

### 2. Targeted Aging Stock Management & Prioritization
For lot managers and pricing directors, the platform provides a dedicated aging stock command center designed to eliminate guesswork, spreadsheet tracking, and manual lot walks.

![Aging Stock Management Table & Critical Units](./images/agingstock.png)

- **Lot Tenure Outlier Detection:** Surfaces high-risk aging units (e.g., 2020 Ford Bronco at **268 days**, 2020 Honda Pilot at **267 days**, 2020 Toyota Prius at **266 days**) with exact lot day counters and visual critical badges.
- **Market Demand & Pricing Alignment:** Displays vehicle asking price, mileage, and fuel type alongside proprietary demand scores (e.g., `Low Demand 10`), enabling managers to evaluate carrying costs against market willingness to pay.
- **Direct Operational Triggers:** Provides single-click actions from any row to inspect, reprice, transfer, or log mitigations directly within the primary operational view.

---

### 3. Auditable Remediation Workflows (Action Logging)
Insight without rapid operational execution produces zero ROI. IID closes the loop between analytics and remediation through an integrated action logging engine.

![Vehicle Action Logging Workflow](./images/action.png)

- **Standardized Mitigation Types:** Managers select from defined business actions, including **Price Reduction Planned**, **Wholesale Transfer**, **Ad Campaign Boost**, or **General Manager Review**, preventing untracked pricing changes or lost inventory oversight.
- **Suggested Operational Quick-Notes:** Accelerates workflow execution with predefined templates (e.g., *"Reduce 5%"*, *"Wholesale transfer"*, *"Ad Campaign"*).
- **Immutable Audit Trail (`Action History`):** Every recorded action logs the authenticated manager identity, UTC timestamp, and operational notes into an immutable history, ensuring organizational governance and audit compliance.
- **Real-Time Synchronized Broadcast:** Submitting an action automatically stages domain events and publishes WebSocket updates to all active managerial sessions via SignalR, ensuring the entire sales floor and finance desk operate on identical, real-time data without page refreshes.

---

### Quantifiable Business Impact & Architectural Foundation
By combining automated aging detection with streamlined action logging, the Intelligent Inventory Dashboard delivers measurable financial and operational ROI:
1. **Accelerated Cash-to-Cash Cycle:** Reduces average days-on-lot by enabling rapid price adjustments and auction transfers before carrying costs exceed profit margins.
2. **Floor-Plan Interest Reduction:** Mitigates hundreds of dollars in daily interest expenses across aging inventory lines.
3. **Data-Driven Governance:** Replaces gut-feel lot management with auditable, metric-backed inventory policies.

Technologically, the platform is engineered as a high-performance, containerized micro-service stack using **Clean Architecture** and **Domain-Driven Design (DDD)** in **C# ASP.NET Core (.NET 10 LTS)** with **FastEndpoints**, persisting to **Azure SQL Database / MSSQL 2022** via **Entity Framework Core 10**, synchronized to an **Angular 19 SPA** via **SignalR**, and comprehensively monitored through **OpenTelemetry** and **OpenObserve**.

---

## 1. Architecture Diagram

The following architecture diagram illustrates the end-to-end architecture, depicting the presentation clients, ingress and hosting environment, application layers, data persistence, and the observability pipeline:

![Intelligent Inventory Dashboard Architecture](./images/comprehensive.png)

---

## 2. Component Descriptions

| Component | Architecture Role & Responsibilities |
|---|---|
| **Client-Side Presentation Layer** | Provides responsive user interfaces and developer interaction channels. The primary interface is the **Angular 19 SPA (`IID.ClientApp`)** providing interactive inventory tables, aging badges, visual analytics, and action modals. For headless and partner interactions, the layer is mocked and explored via **FastEndpoints Swagger (OpenAPI v3)** and scriptable cURL commands. |
| **Docker & Docker Compose** | Containerized runtime environment orchestrating the **`IID.Api`** service container using a multi-stage Alpine build (`src/IID.Api/Dockerfile`). Provides process isolation, reproducible environments, automatic health monitoring, resource limits (CPU/RAM reservations), and unified service networking alongside the database. |
| **FastEndpoints Presentation Engine** | High-performance, vertical-slice REPR (Request-Endpoint-Response) presentation architecture. Replaces heavy ASP.NET Core MVC controllers with discrete endpoints such as `ListVehiclesEndpoint`, `GetDashboardBundleEndpoint`, and `LogVehicleActionEndpoint`. |
| **MediatR CQRS Pipeline** | Coordinates application flow using Command Query Responsibility Segregation (CQRS). Enforces pre-execution validation via `ValidationBehavior` (FluentValidation) and SLA performance tracking via `PerformanceBehavior`. |
| **SignalR Real-Time Hub** | Encapsulated in `InventoryHub` and orchestrated by `SignalRVehicleHubNotifier`. Maintains bidirectional WebSocket tunnels with connected clients, pushing real-time events (`VehicleAging`, `VehicleActionLogged`, `VehicleAdded`) without requiring browser polling. |
| **Domain Engine & Aggregates** | Encapsulates the supply business logic and invariants within DDD boundaries. Aggregates include `Vehicle` (lot status, age, pricing) and `VehicleAction` (action log entries). Enforces business policies through value objects (`Vin`, `Money`) and domain services (`AgingStockIdentifier`). |
| **Persistence Infrastructure (EF Core 10)** | Implemented in `IidDbContext`. Manages SQL Server mapping, optimistic concurrency checking via byte array `RowVersion`, compiled LINQ expressions, and soft-delete query filters (`WHERE DeletedAtUtc IS NULL`). |
| **Domain Event Interceptor** | Implemented as `DomainEventDispatchInterceptor`. Intercepts EF Core database commits to collect domain events from mutated entities and publishes them through MediatR only after successful database transactions. |
| **Aging Stock Monitor Worker** | Background hosted service (`AgingStockMonitorService`) running continuously. Periodically evaluates active inventory against `InventoryPolicy.AgingStockThresholdDays` (90 days) to flag newly aging vehicles and trigger push alerts. |
| **Azure SQL Database** | High-availability cloud relational persistence tier. Houses normalized entity tables, filtered unique indexes (`UX_Vehicle_Vin_Active`), audit history (`VehicleInventoryHistory`), and pre-indexed views for fast dashboard aggregation. |
| **OpenTelemetry & OpenObserve** | Observability pipeline composed of in-process OpenTelemetry instrumentation packages (`OpenTelemetry.Instrumentation.AspNetCore`, `Http`, `Runtime`, `Process`), Serilog OTLP sinks, and the **OpenObserve** platform for centralized real-time logs, APM distributed traces, and customizable metrics dashboards. |


---

## 3. Assumptions

To establish concrete architectural boundaries and resolve ambiguities in the supply domain requirements, the following business and technical assumptions were made:

1. **Single Dealership Scope per Instance (Business Assumption):**  
   While the domain aggregate `Vehicle` includes a `DealershipId` foreign key and supports cross-dealership inventory transfers, version 1 assumes a single-dealership tenancy deployment model. User roles are divided strictly between **Dealership Managers** (authorized for inventory CRUD, vehicle sales, and action logging) and **Sales Staff** (read-only access to available and aging stock). Multi-tenant logical isolation across independent automotive franchises is deferred to future milestones.
2. **Deterministic Calendar-Day Aging Policy (Business Assumption):**  
   The aging threshold is calculated strictly as **90 calendar days** ($T_{\text{now}} - T_{\text{added}} > 90 \text{ days}$), based on the UTC timestamp when the vehicle was formally registered in the system (`DateAddedToInventory`). Business operating days, holiday schedules, and transit/reconditioning delays are not deducted from the lot aging calculation.
3. **Isolated Container Network Security (Technical Assumption):**  
   The backend API container and database communicate over an isolated internal Docker bridge network (`iid` network). The database port (1433) is restricted to the internal network or secured via TLS encrypted connections, with credentials injected via secure environment variables.
4. **Contract-First Client Decoupling (Technical Assumption):**  
   While a high-fidelity Angular 19 SPA (`IID.ClientApp`) is provided, the backend API is strictly decoupled through OpenAPI specifications. Downstream and external dealership systems (e.g., Dealer Management Systems / DMS) interact with the platform purely via standard RESTful endpoints and WebSockets, validated independently via Swagger UI and cURL scripts.

---

## 4. Data Flow

The core workflow of the Intelligent Inventory Dashboard scenario centers on:
> **Core Scenario:** A Dealership Manager identifies a vehicle that has exceeded the 90-day threshold, evaluates its carrying cost, and logs an actionable proposed status (*"Price Reduction Planned"*), updating the system and notifying all active managers in real time.

![Core Use Case Sequence Diagram](./images/main-flow.png)

### Detailed Step-by-Step Explanation:

1. **Authentication & Dispatch:** The manager accesses the dashboard and triggers an action log submission. The client crafts an authenticated HTTP request `POST /api/v1/vehicles/{id}/actions` with a signed JWT bearer token containing manager claims (`role: Manager`, `sub: {userId}`).
2. **Ingress & Network Dispatch:** The reverse proxy / host port mapping routes incoming traffic directly into the isolated `iid-api` Docker container on port 8080 over the internal bridge network.
3. **Endpoint Routing & Contract Validation:** `LogVehicleActionEndpoint` binds the route ID and request body. The integrated FluentValidator immediately verifies constraints (non-empty ID, valid action type enum, notes capped at 2,000 characters).
4. **Pipeline Interception:** MediatR passes the command through `ValidationBehavior` and `PerformanceBehavior`, starting an OpenTelemetry Activity span (`LogVehicleActionHandler`).
5. **Domain Invariant Enforcement:** `LogVehicleActionHandler` fetches the target vehicle from `VehicleRepository`. It verifies that the vehicle exists and is active (`DeletedAtUtc == null`). It invokes `AgingStockIdentifier` to confirm the vehicle's lot tenure ($> 90$ days).
6. **Factory Construction & Event Staging:** The domain entity `VehicleAction.Log` instantiates the record, stamps the server-side UTC timestamp, binds the authenticated caller ID, and stages a `VehicleActionLogged` domain event.
7. **Transactional Persistence:** EF Core 10 writes the record to `dbo.VehicleAction` in Azure SQL Database. The `DomainEventDispatchInterceptor` intercepts the execution, waiting for the commit to succeed before publishing.
8. **Real-Time WebSocket Push:** Upon successful commit, the interceptor publishes the event through MediatR to `SignalRVehicleHubNotifier`. The `InventoryHub` broadcasts the new action to the `inventory-dashboard` group, dynamically updating all open manager screens without a browser refresh.
9. **Correlated Observability Emission:** In-process OpenTelemetry collectors capture the end-to-end trace context (HTTP span, EF Core SQL span, MediatR execution span). The Serilog logger emits a structured log enriched with `TraceId` and `SpanId`, which is pushed via OTLP to OpenObserve.

---

## 5. Technology Choices & Justifications

### 5.1 Backend Architecture & Technology Choices

The backend architecture is engineered around the principles of **Clean Architecture**, **Vertical-Slice REPR (Request-Endpoint-Response)**, and **Domain-Driven Design (DDD)**, deployed as containerized micro-services on .NET 10 LTS.

![Backend Architecture & Request Flow Overview](./images/overview.png)

#### 5.1.1 Architectural Highlights & Design Philosophy
- **Clean Architecture Boundary Isolation:** The domain core (`IID.Domain`) is entirely independent of external frameworks, libraries, database drivers, or presentation engines. Dependencies flow inward toward the domain, ensuring long-term maintainability and testability.

![Clean Architecture & Layer Dependency Flow](./images/backend.png)
- **Vertical-Slice REPR Architecture with FastEndpoints:** Replaces monolithic ASP.NET Core MVC controllers with discrete, isolated endpoint classes. By eliminating controller reflection scanning, complex filter pipelines, and dynamic route compilation, FastEndpoints drastically decreases request allocation overhead and improves throughput.
- **CQRS via MediatR:** Commands (which mutate inventory state) and Queries (which fetch read-optimized projections) are strictly decoupled. This guarantees single-responsibility handlers, deterministic side-effects, and independent optimization paths for reads and writes.
- **Cross-Cutting Pipeline Behaviors:** MediatR pipeline behaviors enforce cross-cutting operational concerns without polluting use-case logic:
  - `ValidationBehavior`: Automatically triggers FluentValidation before reaching handlers, rejecting invalid payloads immediately.
  - `PerformanceBehavior`: Wraps request execution with high-precision stopwatch metrics, emitting OpenTelemetry activity spans and logging warnings if handler execution exceeds SLA thresholds (500 ms).

#### 5.1.2 End-to-End Request Flow & Lifecycle
To trace how requests traverse the backend architecture, consider the lifecycle of an action log request:
1. **Ingress & Transport:** Client sends an authenticated HTTP request (e.g., `POST /api/v1/vehicles/{id}/actions`) over TLS. The Docker bridge network routes the packet directly into the Kestrel web server on port 8080.
2. **Endpoint Resolution & Deserialization:** FastEndpoints matches the URI template, binds path parameters and the JSON body into a strongly-typed request DTO, and invokes the endpoint's configured validator.
3. **Pipeline Interception:** The request is mapped into a MediatR command (`LogVehicleActionCommand`) and sent through the MediatR pipeline:
   - `ValidationBehavior` verifies parameter constraints (non-empty IDs, valid action enum, maximum note length). If invalid, a standardized ProblemDetails response is returned immediately.
   - `PerformanceBehavior` starts an OpenTelemetry span (`LogVehicleActionHandler`) linked to the incoming W3C `traceparent`.
4. **Application Orchestration:** The command handler (`LogVehicleActionHandler`) retrieves the aggregate root (`Vehicle`) from the repository, invoking database query filters to ensure the entity is active (`DeletedAtUtc IS NULL`).
5. **Domain Invariant Enforcement:** The handler executes domain methods on the aggregate. The aggregate verifies business rules (e.g., confirming the vehicle has surpassed the 90-day threshold via `AgingStockIdentifier`). Upon validation, a new `VehicleAction` domain entity is created, and a `VehicleActionLoggedDomainEvent` is staged in the aggregate's event collection.
6. **Transactional Persistence:** EF Core 10 writes changes to SQL Server inside a database transaction. EF Core verifies the `RowVersion` concurrency token.
7. **Post-Commit Event Dispatch:** The `DomainEventDispatchInterceptor` intercepts the commit. Upon successful commit, staged domain events are published via MediatR to decoupled event listeners.
8. **Real-Time Push Notification:** The event handler triggers `SignalRVehicleHubNotifier`, which pushes a WebSocket payload (`VehicleActionLogged`) through `InventoryHub` to all connected manager dashboard sessions.
9. **Observability Emission:** OpenTelemetry activity completes, and Serilog logs the structured outcome (enriched with `TraceId` and `SpanId`), exporting data to OpenObserve via OTLP.

#### 5.1.3 Domain-Driven Design (DDD) & Invariant Protection
- **Aggregate Roots (`Vehicle`, `VehicleAction`):** Enforce strict consistency boundaries. Entities have private parameterless constructors and private setters. State can only be mutated through explicit domain methods (`Vehicle.Create`, `Vehicle.UpdatePrice`, `VehicleAction.Log`), guaranteeing that entities never enter invalid or corrupted states.
- **Value Objects (`Vin`, `Money`, `VehicleStatus`):** Model immutable domain concepts with self-contained validation. Implemented using C# `readonly record struct` (zero heap allocation). Value objects prevent primitive obsession by guaranteeing that invalid VINs or negative currency amounts cannot exist anywhere in the application.
- **Domain Services (`AgingStockIdentifier`, `InventoryPolicy`):** Encapsulate domain calculations and multi-entity policies that do not belong to a single entity, such as determining aging stock thresholds (90 days) and evaluating carrying cost exposure.
- **Functional Result Pattern (`Result<T>`, `ErrorKind`):** Business logic returns functional `Result<T>` structures containing strongly-typed errors (`NotFound`, `Validation`, `Conflict`, `Forbidden`) rather than throwing expensive CLR exceptions. Exceptions are reserved strictly for exceptional system failures (database drop, network outage).

#### 5.1.4 Data Persistence & Optimization (EF Core 10 & Azure SQL)
- **Filtered Indexes:** 
  - `UX_Vehicle_Vin_Active`: Enforces global uniqueness on VINs while filtering out soft-deleted records (`WHERE DeletedAtUtc IS NULL`).
  - `IX_Vehicle_Aging_Active`: Covering index that includes `DaysInStock`, `Status`, `Make`, and `Model` to serve dashboard queries in under 5 ms without table scans.
- **Optimistic Concurrency Control:** Implemented with a SQL `rowversion` column mapped to `byte[] RowVersion`. When multiple managers concurrently attempt to take action or reprice a vehicle, the second update fails safely with a concurrency conflict, preventing lost updates.
- **Global Soft-Delete Query Filters:** Configured in `IidDbContext` using EF Core's `HasQueryFilter(v => v.DeletedAtUtc == null)`, guaranteeing that soft-deleted entities are never exposed to standard queries unless explicitly requested.

#### 5.1.5 High-Performance Source-Generated Logging (Zero-Allocation Architecture)
Logging in enterprise backend systems is frequently an unexamined performance bottleneck. Under heavy concurrent workloads (such as morning lot intake sweeps or load testing at 150+ req/s), traditional logging patterns using `ILogger.LogInformation("...", arg1, arg2)` suffer from substantial runtime overhead:
1. **Value Type Boxing:** Primitive values such as `Guid` identifiers, `int` page counters, or `decimal` currency amounts are boxed onto the managed heap as `object`.
2. **Heap Allocations:** The params argument (`params object[] args`) allocates a new array on every logging invocation, generating garbage collection (GC) pressure.
3. **Runtime Template Parsing:** The message format template must be parsed at runtime on every invocation, consuming CPU cycles even for repetitive informational events.

To eradicate this overhead, IID strictly implements the official Microsoft architectural guidance for [High-performance logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/high-performance-logging) using the compile-time **`[LoggerMessage]` source generator** in `Microsoft.Extensions.Logging`.

##### Implementation Architecture (`IID.Application.Logging.LogMessage`)
All MediatR handlers and application workflows emit log events through centralized, source-generated partial extension methods defined in [`src/IID.Application/Logging/LogMessage.cs`](../src/IID.Application/Logging/LogMessage.cs):

```csharp
namespace IID.Application.Logging;

/// <summary>
/// Centralized source-generated log messages for the Application layer.
/// All handlers should invoke these extension methods on their ILogger
/// rather than declaring private [LoggerMessage] methods inline.
/// </summary>
public static partial class LogMessage
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle action logged {ActionId} on {VehicleId}")]
    public static partial void VehicleActionLogged(this ILogger logger, Guid actionId, Guid vehicleId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle created {VehicleId} VIN={Vin}")]
    public static partial void VehicleCreated(this ILogger logger, Guid vehicleId, string vin);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle sold {VehicleId} VIN={Vin} amount={Amount}")]
    public static partial void VehicleSold(this ILogger logger, Guid vehicleId, string vin, decimal amount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed vehicles total={Total} page={Page} limit={Limit}")]
    public static partial void VehiclesListed(this ILogger logger, int total, int page, int limit);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched aging-stock page={Page} limit={Limit} total={Total}")]
    public static partial void AgingStockFetched(this ILogger logger, int total, int page, int limit);
}
```

##### Architectural Benefits & Benchmark Rationale
- **Zero Heap Allocations:** Value types (`Guid`, `int`, `decimal`) are passed directly via strongly-typed parameters with zero boxing and zero `params object[]` array instantiation.
- **Precomputed Template Parsing:** The Roslyn compiler generates static logging delegates during build time, eliminating runtime string analysis.
- **Built-in `IsEnabled()` Early Exit:** The source-generated code automatically embeds an `if (!logger.IsEnabled(level)) return;` check before executing any logic. If a log level (e.g., `LogLevel.Debug`) is disabled in production, execution exits with zero performance penalty.
- **Direct Integration with OpenTelemetry:** Emitted logs automatically correlate with OpenTelemetry `Activity.Current` (`trace_id` and `span_id`), feeding structured events directly to OpenObserve without serialization lag.
- **Measured Impact:** Contributed directly to achieving **sub-27 ms p95 latencies** and **0% error rates** during continuous 150 req/s load tests by keeping Gen 0/1 garbage collections near zero.

#### 5.1.6 Containerization & Load Test Benchmarks
- **Multi-Stage Docker Packaging:** Builds an ultra-slim container image based on `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`. The container runs as a non-root user, starts up in < 500 ms, and has an idle memory footprint of < 150 MB.
- **Proven High Throughput:** Validated under automated k6 load testing executing **89,960 requests at a sustained rate of 150 requests/second with 0% error rate and 0 dropped requests**. The p95 response time was clocked at **26.94 ms** (median: 11.99 ms).

![k6 Load Test Benchmark Metrics](./images/loadtest_load.png)

#### 5.1.7 Backend Justification Matrix

| Dimension | C# ASP.NET Core (.NET 10 LTS) & FastEndpoints | Docker & Containerization | EF Core 10 & Azure SQL |
|---|---|---|---|
| **Scalability** | Non-blocking Kestrel asynchronous I/O engine handles thousands of concurrent requests per core with minimal thread context-switching. | Multi-container architecture easily scales horizontally behind reverse proxies with resource reservation boundaries. | Azure SQL elastic query pooling and auto-scaling vCores accommodate dynamic traffic spikes. |
| **Performance** | FastEndpoints removes MVC reflection overhead. C# 14 zero-allocation record structs, compiled LINQ queries, and source-generated `[LoggerMessage]` eliminate boxing and GC pressure. | Multi-stage Alpine container image provides sub-second cold starts and minimal container overhead. | Filtered covering indexes (`IX_Vehicle_Aging_Active`) enable sub-millisecond lookups for aging inventory queries. |
| **Reliability** | Clean Architecture, domain invariants, and typed `Result<T>` error handling eliminate unhandled runtime exceptions. | Healthcheck probes (`SELECT 1`), automatic restart policies (`restart: unless-stopped`), and isolated container networking. | ACID transaction guarantees with optimistic concurrency tokens (`RowVersion`) prevent race conditions. |
| **Maintainability** | Vertical-slice architecture isolates features into discrete folders; modifying an endpoint has zero side-effects on others. | Infrastructure-as-code parity: identical containerized stack across development, staging, and production environments. | Code-First migrations versioned in Git enable reproducible, automated schema deployments. |

---

### 5.2 Frontend Architecture & Technology Choices

The frontend tier is implemented as a single-page application (SPA) built with **Angular 19**, designed to provide dealership managers with instant inventory visibility, interactive analytics, and responsive real-time workflow controls.

![Frontend Architecture & Layer Interactions](./images/frontend.png)

#### 5.2.1 Core Framework & Highlights (Angular 19 SPA)
- **Standalone Component Architecture:** Implemented entirely using Angular 19 standalone components (`standalone: true`). By bypassing legacy `NgModule` declarations, the application achieves cleaner component hierarchies, optimized tree-shaking, and smaller initial download bundles.
- **Signals & Reactive State Management:** Utilizes Angular Signals (`signal()`, `computed()`) for component-level reactive state combined with RxJS for asynchronous event pipelines. Signals provide fine-grained reactivity, updating only the specific DOM nodes affected by state changes without requiring zone-wide change detection passes.
- **Strict TypeScript Typing:** Complete end-to-end type safety across domain interfaces, API request/response contracts, and reactive forms. Ensures any schema evolution in backend DTOs is immediately verified at compile-time on the client.
- **Lazy-Loaded Routing:** Application routes (`auth`, `dashboard`, `vehicles`, `dealerships`) are lazily loaded on-demand, reducing initial First Contentful Paint (FCP) and preserving client memory.

#### 5.2.2 Real-Time Synchronization & SignalR Integration
- **Persistent Bi-directional WebSockets (`RealtimeService`):** Establishes an active WebSocket connection to the backend `InventoryHub` using `@microsoft/signalr`.
- **Automatic Reconnection with Backoff:** Configured with `withAutomaticReconnect()` utilizing exponential backoff retry delays. If network connectivity drops momentarily (e.g., tablet roaming across dealership Wi-Fi access points), the service seamlessly reconnects and resynchronizes state without requiring a browser reload.
- **Instant Event Propagation:** Listens for real-time events published by the backend domain interceptor:
  - `VehicleAging`: Notifies the UI when a background monitor flags a vehicle exceeding 90 days.
  - `VehicleActionLogged`: Dynamically appends new manager action proposals to the vehicle's activity history and updates inventory status badges across all concurrent manager screens.
  - `VehicleAdded`: Automatically incorporates newly ingested vehicles into inventory counts and KPI widgets.

#### 5.2.3 User Experience & Dashboard Capabilities
- **Tiered Aging Stock Badges:** Visual indicators dynamically color-code inventory based on lot tenure:
  - **Fresh Stock (< 60 Days):** Green badge indicating healthy turnover.
  - **Watchlist (60–89 Days):** Amber badge warning managers of approaching holding cost escalation.
  - **Aging Stock (90+ Days):** Pulsing red badge indicating capital at risk requiring urgent management action under policy.
- **Executive KPI Cards & Analytics:** Instant high-level summaries displaying total inventory count, total aging vehicles, estimated cumulative carrying costs, and average lot tenure.
- **Action Workflow Modals:** Streamlined workflow dialogs allowing managers to log proposed mitigation actions (*"Price Reduction Planned"*, *"Wholesale Auction"*, *"Lot Transfer"*, *"Mechanical Inspection"*) with instant client-side validation and immediate feedback toasts.
- **Theme & Ergonomics:** Built-in dark/light mode toggle (`ThemeService`) and responsive layout accommodating desktop workstations and tablet floor inspections.

#### 5.2.4 Client-Side Security, Resilience & Interceptors
- **JWT Authentication & Bearer Interceptor (`auth.interceptor.ts`):** Transparently injects the `Authorization: Bearer <token>` header on all outgoing HTTP requests. Automatically traps `401 Unauthorized` responses to clear invalid sessions and redirect users to the authentication screen.
- **Role-Based Route Guards (`auth.guard.ts`):** Evaluates user claims before activating routes, ensuring dealership managers have access to inventory action workflows while restricting view-only users.
- **Centralized Error Handling & Toast Notifications (`ToastService`):** HTTP interceptor transforms backend `ProblemDetails` errors into friendly, dismissible toast notifications, preventing silent failures.

#### 5.2.5 Headless & Mocked Client Layer Alternatives
- **FastEndpoints Swagger / OpenAPI v3:** Integrated interactive documentation accessible at `/swagger`. Enables developers, quality assurance teams, and external systems to inspect endpoints, schemas, and simulate requests directly from the browser.
- **Scriptable cURL & CLI Scripts:** Fully documented cURL requests allowing headless automated workflows, CI/CD smoke tests, and developer terminal debugging without requiring the GUI.

#### 5.2.6 Frontend Justification Matrix

| Dimension | Angular 19 SPA | SignalR Real-Time Client | Headless Alternatives (Swagger / cURL) |
|---|---|---|---|
| **Scalability** | Static single-page application bundle deployable to edge CDNs with zero client-side server rendering load. | Push-based WebSocket events eliminate costly periodic polling from hundreds of active dealership browser tabs. | Lightweight REST endpoints easily consumed by microservices, batch scripts, and automation runners. |
| **Performance** | Angular Signals minimize DOM re-renders; lazy loading minimizes initial JavaScript bundle payload. | Low-overhead binary WebSocket framing delivers millisecond-latency UI updates upon database commits. | Direct HTTP invocations with minimal JSON overhead for automated testing and CI/CD validation. |
| **Reliability** | TypeScript strict mode and reactive form validation catch input defects before requests leave the client. | Built-in automatic reconnection policy gracefully handles network hiccups and device sleep/wake cycles. | Deterministic OpenAPI v3 contracts guarantee strict schema adherence across all integration channels. |
| **Maintainability** | Standalone component architecture eliminates NgModule boilerplate; modular structure isolates feature code. | Centralized `RealtimeService` abstracts connection management and event subscriptions behind clean RxJS observables. | Auto-generated OpenAPI specifications from backend code ensure documentation stays 100% in sync with API changes. |

---

### 5.3 Automated Testing Strategy & Unit Test Architecture

The Intelligent Inventory Dashboard employs a rigorous **Test-Driven & Verification-First** engineering discipline. To maintain high velocity without compromising business correctness or performance, automated testing is organized around the **Clean Architecture Test Pyramid**, ensuring every layer—from immutable domain invariants to reactive frontend signals—is verified in complete isolation with deterministic, fast-running test suites.

![Clean Architecture Test Pyramid Hierarchy](./images/testing.png)

#### 5.3.1 Clean Architecture Test Layering & Isolation
Each architectural layer has a dedicated test project mirroring the system's structural boundaries:

1. **`IID.Domain.Tests` (123 Tests — 59 ms execution):**
   - **Zero Mocks Principle:** The domain layer contains zero external dependencies. Tests execute purely against in-memory entity instantiations, value objects, and domain services.
   - **Invariant Protection:** Verifies that aggregate roots (`Vehicle`, `VehicleAction`) never transition into invalid states, constructor guards reject nulls, and domain events stage correctly.
   - **Boundary & Specification Testing:** Validates ISO 3779 VIN formats, currency mathematical precision (`Money`), and calendar-day lot tenure calculations (`AgingStockIdentifier`).

2. **`IID.Application.Tests` (108 Tests — 188 ms execution):**
   - **Isolated CQRS Use-Case Verification:** Commands and Queries are tested against their respective handlers (`LogVehicleActionHandler`, `ListVehiclesHandler`, `UpdateVehiclePriceHandler`) by mocking all secondary ports (`IVehicleRepository`, `IUnitOfWork`, `IVehicleHubNotifier`, `ICurrentUser`, `IClock`).
   - **Result Pattern Validation:** Asserts that business rule failures return strongly-typed functional results (`ErrorKind.NotFound`, `ErrorKind.Unauthorized`, `ErrorKind.Conflict`) rather than throwing unhandled exceptions.
   - **Pre-Execution Pipeline Validation:** Leverages `FluentValidation.TestHelper` to verify validation rules before handlers execute.

3. **`IID.Infrastructure.Tests` (12 Tests — 831 ms execution):**
   - **EF Core Persistence & Query Semantics:** Utilizes `Microsoft.EntityFrameworkCore.InMemory` to verify repository query logic, filtered LINQ expressions, and soft-delete filters (`WHERE DeletedAtUtc IS NULL`).
   - **Database Seeder Verification:** Tests data initialization routines and lot aging calculations against seeded vehicle datasets.

4. **`IID.Api.Tests` (8 Tests — 120 ms execution):**
   - **Endpoint Contract & Presentation Testing:** Verifies FastEndpoints routing, HTTP status code translation (mapping `ErrorKind.NotFound` to `404 ProblemDetails`), and middleware execution.

5. **`IID.ClientApp` (Frontend Jasmine/Karma Unit Tests):**
   - **Signals & Reactive State Verification:** Tests Angular 19 signals (`signal()`, `computed()`) to ensure reactive state transitions correctly when stock actions are triggered.
   - **Service & Interceptor Mocks:** Employs `HttpClientTestingModule` and mock `RealtimeService` to verify JWT bearer token injection and WebSocket event dispatching without live network connections.

---

#### 5.3.2 Testing Tooling & Technology Stack Choices

| Technology | Role & Responsibility | Architectural Justification |
|---|---|---|
| **xUnit 2.9.2** | Core Backend Test Framework | Parallel test runner with isolated test class instantiation per test, eliminating shared static state and test pollution. Supports parameterized `[Theory]` and `[InlineData]` for edge-case fuzzing. |
| **FluentAssertions 6.12.1** | Readable Assertion Engine | Provides expressive, intention-revealing assertion syntax (`result.IsSuccess.Should().BeTrue()`, `action.Should().NotBeNull()`). Produces rich, contextual failure messages that pinpoint exact diffs. |
| **Moq 4.20.72** | Behavioral Mocking Library | Configures deterministic stubs and verifies critical side-effects on outbound interfaces (e.g., verifying `_notifier.Verify(n => n.VehicleActionLoggedAsync(...), Times.Once)`). |
| **EF Core In-Memory 10.0.0** | Ephemeral Persistence Provider | Allows repository query logic and LINQ filters to run in-memory within milliseconds without spinning up heavyweight SQL Server containers for unit-level verification. |
| **FluentValidation.TestHelper** | Contract Validation Assertions | Provides dedicated `.ShouldHaveValidationErrorFor()` and `.ShouldNotHaveValidationErrorFor()` extensions for validating command rules independently of handler execution. |
| **Jasmine & Karma** | Frontend Unit Testing Stack | Standard Angular testing toolchain integrated with Angular TestBed for headless component rendering, change detection cycle verification, and RxJS stream validation. |

---

#### 5.3.3 Implementation Patterns & Concrete Code Practices

##### 1. The Arrange-Act-Assert (AAA) & SUT Factory Pattern
To ensure maintainability and insulate test methods against constructor signature changes, all application tests utilize the **SUT (Subject Under Test) Factory Pattern**:

```csharp
public class LogVehicleActionHandlerTests
{
    private readonly Mock<IVehicleRepository> _vehicles = new();
    private readonly Mock<IVehicleActionRepository> _actions = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IVehicleHubNotifier> _notifier = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ICurrentUser> _user = new();

    // Centralized SUT instantiation with default stubs
    private LogVehicleActionHandler CreateSut()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        _user.SetupGet(u => u.Id).Returns("manager-guid-001");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));

        return new LogVehicleActionHandler(
            _vehicles.Object, _actions.Object, _uow.Object,
            _notifier.Object, _clock.Object, _user.Object,
            NullLogger<LogVehicleActionHandler>.Instance);
    }
}
```

##### 2. Domain Invariant & Edge-Case Parameterized Testing
Domain unit tests strictly enforce business invariants. Value objects reject malformed inputs at construction time, preventing illegal states from ever entering the system:

```csharp
public class VinTests
{
    [Theory]
    [InlineData("5YJ3E1EA7JF000001")] // Standard 17-character alphanumeric
    [InlineData("JH4DA1760HS000001")]
    public void Accepts_valid_vin(string raw)
    {
        var vin = Vin.Parse(raw);
        Assert.Equal(raw.Length, vin.Value.Length);
    }

    [Theory]
    [InlineData("")]                    // Empty
    [InlineData("5YJ3E1EA7JF00000I")]   // 'I' forbidden under ISO 3779 standard
    [InlineData("5YJ3E1EA7JF00000")]    // 16 chars (too short)
    [InlineData("5YJ3E1EA7JF0000012")]  // 18 chars (too long)
    public void Rejects_invalid_vin(string raw)
    {
        Assert.Throws<ArgumentException>(() => Vin.Parse(raw));
    }
}
```

##### 3. Handler Verification & Side-Effect Assertion
Application handler unit tests verify three essential outcomes:
1. **State Mutation:** Target entity is retrieved, business rules are executed, and changes are committed via `IUnitOfWork`.
2. **Notification Dispatch:** Real-time push notifications are dispatched to active WebSocket clients via `_notifier.Verify(..., Times.Once)`.
3. **Deterministic Error Mapping:** Unauthenticated callers receive `ErrorKind.Unauthorized`, and missing records return `ErrorKind.NotFound`:

```csharp
[Fact]
public async Task Handle_Should_NotifyHub_OnSuccess()
{
    // Arrange
    var vehicle = CreateVehicle();
    _vehicles.Setup(v => v.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(vehicle);

    // Act
    var command = new LogVehicleActionCommand(vehicle.Id, VehicleActionType.PriceReductionPlanned, "Lower price 5%");
    var result = await CreateSut().Handle(command, CancellationToken.None);

    // Assert: Verify business success and real-time SignalR push
    result.IsSuccess.Should().BeTrue();
    _actions.Verify(a => a.AddAsync(It.IsAny<VehicleAction>(), It.IsAny<CancellationToken>()), Times.Once);
    _notifier.Verify(n => n.VehicleActionLoggedAsync(
        It.IsAny<VehicleAction>(), It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

---

#### 5.3.4 Frontend Unit Testing Approach (Angular 19 SPA)
Frontend testing mirrors backend rigor by verifying reactive state propagation and HTTP contract fidelity:
- **Angular Signal Reactivity:** Verifies that when `VehicleStore` updates its `vehicles` signal, computed signals (`agingStockCount`, `totalPortfolioValue`) update synchronously without triggering manual change detection.
- **Mocking Real-Time WebSockets:** Uses a stubbed `RealtimeService` emitting RxJS Subjects to simulate incoming `VehicleActionLogged` events, asserting that UI toast notifications and action badges update in real time.
- **HTTP Interceptor Verification:** Verifies that `auth.interceptor.ts` transparently injects the bearer token and traps `401 Unauthorized` responses to trigger clean session termination.

---

#### 5.3.5 Automated Test Suite Execution Metrics & Justification Matrix

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                             Automated Test Suite Summary                         │
├────────────────────────┬─────────────┬───────────┬──────────────┬────────────────┤
│ Test Project           │ Total Tests │ Passed    │ Failed/Skip  │ Run Duration   │
├────────────────────────┼─────────────┼───────────┼──────────────┼────────────────┤
│ IID.Domain.Tests       │ 123         │ 123 (100%)│ 0            │ 59 ms          │
│ IID.Application.Tests  │ 108         │ 108 (100%)│ 0            │ 188 ms         │
│ IID.Infrastructure.Tests│ 12         │ 12 (100%) │ 0            │ 831 ms         │
│ IID.Api.Tests          │ 8           │ 8 (100%)  │ 0            │ 120 ms         │
├────────────────────────┼─────────────┼───────────┼──────────────┼────────────────┤
│ Total Solution Tests   │ 251         │ 251 (100%)│ 0            │ ~1.20 Seconds  │
└────────────────────────┴─────────────┴───────────┴──────────────┴────────────────┘
```

| Dimension | Domain Tests | Application Tests | Infrastructure Tests | Frontend Unit Tests |
|---|---|---|---|---|
| **Speed & Feedback** | Instant (< 60 ms). Zero I/O overhead enables continuous test running during active coding. | Ultra-fast (< 200 ms). Moq stubs eliminate database access while testing full business flows. | Fast (< 1 sec). In-memory EF Core database avoids network roundtrips. | Fast (< 2 sec). Headless Chrome execution tests component reactivity. |
| **Isolation** | 100% pure C#. No framework, database, or DI container coupling. | Isolated per use-case. External ports abstracted behind clean interfaces. | Validates EF Core mapping and LINQ translation in process. | Components tested independently of server uptime using TestBed. |
| **Maintainability** | High resilience. Tests change only when underlying business rules or policies change. | SUT Factory pattern insulates tests against constructor parameter refactoring. | In-memory provider requires zero Docker environment orchestration. | Standalone components reduce test setup boilerplate. |
| **Regression Safety** | Catches edge cases in VIN parsing, monetary math, and aging calculations. | Guarantees CQRS commands and queries handle validation, authorization, and notifications. | Prevents silent bugs in EF Core soft-delete query filters or indexes. | Prevents broken UI bindings, signal regressions, and styling glitches. |

---

## 6. Observability Strategy

Production readiness requires comprehensive observability across the three core pillars: **Metrics**, **Traces**, and **Logs**, unified under the OpenTelemetry standard and visualized within OpenObserve.

![OpenTelemetry & OpenObserve Architecture Pipeline](./images/openobserve.png)

### 6.1 Distributed Tracing (APM)
- **Standard & Instrumentation:** Employs the OpenTelemetry .NET SDK with automatic instrumentation for ASP.NET Core (`OpenTelemetry.Instrumentation.AspNetCore`), outgoing HTTP calls (`OpenTelemetry.Instrumentation.Http`), and database commands.
- **Trace Context Propagation:** Adheres to the W3C Trace Context specification (`traceparent`, `tracestate`). Incoming requests from clients or API gateways carry trace IDs that flow through MediatR pipeline behaviors and into EF Core SQL queries as SQL comment tags (`/*traceparent='...'*/`).
- **OpenObserve Integration:** Distributed traces are forwarded to OpenObserve via standard OTLP HTTP/Protobuf (`/v1/traces`). OpenObserve constructs real-time waterfall distributed traces detailing execution time spent across middleware, handlers, SQL transactions, and SignalR socket pushes.

![OpenObserve Live Telemetry & Structured Log Stream](./images/oo.png)

### 6.2 Metrics & Service Level Objectives (SLOs)
The system tracks metrics adhering to the **RED Method** (Rate, Errors, Duration) and emits low-overhead CLR performance counters, visualized on live OpenObserve dashboards:

| Metric Category | Metric Name | Source / Provider | Target Production SLO |
|---|---|---|---|
| **Throughput (Rate)** | `http.server.request.rate` | OpenTelemetry.AspNetCore | Sustain 150+ req/sec during peak morning sync |
| **Error Rate** | `http.server.failed_requests_ratio` | OpenTelemetry.AspNetCore | **< 0.1%** 5xx internal server errors |
| **Latency (Duration)** | `http.server.request.duration` | OpenTelemetry.AspNetCore | **p95 < 500 ms** (k6 achieved: **26.94 ms**) |
| **Runtime Health** | `process.runtime.dotnet.gc.collections` | OpenTelemetry.Runtime | Zero Gen2 thrashing; GC pause ratio < 1% |
| **Memory / CPU** | `process.memory.working_set`, `process.cpu.utilization` | OpenTelemetry.Process | Memory consumption < 70% of 4096M limit |
| **Business Metric** | `inventory.aging_stock.count` | Custom `Meter("IID.Domain")` | Real-time gauge of vehicles > 90 days |
| **Business Metric** | `inventory.actions_logged.total` | Custom `Meter("IID.Domain")` | Counter segmented by action type |

### 6.3 Centralized Structured Logging
- **Framework:** Structured logging powered by Serilog (`Serilog.AspNetCore`) using compile-time source-generated logging (`LoggerMessageAttribute`) for zero-allocation performance, exported via `Serilog.Sinks.OpenTelemetry` into OpenObserve's `iid-api` stream.
- **Trace Correlation:** Every log line is enriched with `trace_id` and `span_id` extracted from the active OpenTelemetry `Activity.Current`. In OpenObserve, clicking on an error in the stream log viewer directly opens the corresponding APM distributed trace.
- **Sanitization:** Log templates capture structured entities (e.g., `{VehicleId}`, `{Vin}`, `{ActionType}`, `{UserId}`) while strictly sanitizing sensitive credentials or authentication headers.

### 6.4 Alerting & Automated Monitors
OpenObserve monitor alert rules are configured with automated webhook notifications (e.g., PagerDuty, Slack, email):
1. **High Latency Alert:** Triggers if `p95(http_req_duration) > 500ms` for 5 consecutive minutes.
2. **Error Rate Spike:** Triggers if `5xx` error responses exceed `1%` of total requests over a 2-minute window.
3. **Aging Scan Heartbeat:** Triggers if `AgingStockMonitorService` fails to emit a heartbeat sweep completion metric within 30 minutes.


---

## 7. Performance Verification & Load Testing (k6)

### 7.1 Why Load Testing is Necessary (Objectives & Architectural Rationale)
Automated load and performance testing is a critical validation gate ensuring the production readiness of the Intelligent Inventory Dashboard. In automotive dealership operations, dealership general managers, sales executives, and automated ingestion services interact with the platform concurrently—particularly during morning lot inspections, vehicle intakes, and month-end inventory turnover audits.

Load testing serves five essential architectural objectives:
1. **Concurrency & Throughput Proof:** Validates that Kestrel's asynchronous non-blocking I/O loop and socket pooling sustain high concurrent traffic without dropped connections, thread starvation, or socket exhaustion.
2. **Framework Overhead Verification:** Empirically proves that Clean Architecture layer isolation, FastEndpoints vertical-slice routing, FluentValidation pre-execution checks, and MediatR pipeline behaviors add negligible CPU cycles and memory allocations under sustained load.
3. **Database Contention & Index Efficiency:** Ensures that complex relational aggregations (such as lot tenure calculations and carrying cost summations) execute within milliseconds, confirming that filtered covering indexes (`IX_Vehicle_Aging_Active`) and compiled LINQ queries prevent database lock escalation and connection pool exhaustion.
4. **SLA & Production SLO Adherence:** Enforces strict enterprise Service Level Objectives (SLOs):
   - **`http_req_duration p(95) < 500ms`**: 95% of dashboard requests must complete in under 500ms.
   - **`http_req_duration p(99) < 1000ms`**: 99% of requests must complete in under 1 second.
   - **`http_req_failed < 1%`**: System error rate must remain under 0.1% for production readiness.
   - **`dashboard_bundle_success_rate > 99%`**: Business payload integrity and JSON schema validation must pass at 99%+.
5. **Container Sizing & Resource Boundary Verification:** Confirms that Docker container limits (CPU reservation `0.5`, limit `2.0`; RAM reservation `1024M`, limit `4096M`) provide ample headroom without triggering CPU throttling or kernel Out-Of-Memory (OOM) termination.

### 7.2 Target APIs & Key Workloads Measured

To render the comprehensive **Dealership Inventory Dashboard**, the client application must trigger a computationally heavy aggregation query that calculates and consolidates multiple business domains into a unified response:

![Dealership Dashboard Requiring Heavy Aggregation Calculations](./images/dashboard.png)

As shown above, rendering this single screen requires significant server-side processing:
- **Financial Valuations:** Dynamically calculating total portfolio valuation ($3,146,400) and average unit prices across 72 units.
- **Lot Tenure & Aging Analysis:** Evaluating calendar-day lot tenure ($T_{\text{now}} - T_{\text{added}} > 90 \text{ days}$) across the entire fleet to isolate critical aging stock (12 units) and determine average lot velocity (63 days).
- **Multi-Dimensional Analytics:** Computing sales velocity trends, powertrain mix distributions (64% Petrol, 18% Hybrid, 15% Electric), and prioritizing units with low demand scores in the Action Center.
- **Paginated Asset Records:** Streaming sorted, filtered vehicle rows with live status badges.

Because this endpoint represents the single most resource-intensive and critical query path in the system, we decided to focus our load testing directly on:

```http
GET /api/v1/dashboard?dealershipId={id}&page=1&pageSize=20
```

#### Key Workload Breakdown:
- **Primary Load Target (`GET /api/v1/dashboard?dealershipId={id}&page=1&pageSize=20`):** Rotates parameterized queries across active dealerships to evaluate database index efficiency (`IX_Vehicle_Aging_Active`) and EF Core compilation without relying on artificial query-cache artifacts.
- **Pre-Test Token Issuance (`POST /api/v1/auth/login`):** Automated setup helper that retrieves signed JWT Bearer tokens for test execution.
- **Dynamic Discovery (`GET /api/v1/dealerships`):** Queries active dealership IDs at test initialization to evenly distribute virtual user (VU) traffic across branches.

### 7.3 Test Harness Architecture & Setup Instructions
The performance testing infrastructure is maintained under `tools/k6` as an automated, version-controlled suite powered by [Grafana k6](https://k6.io/):

```
tools/k6/
├── config/
│   └── environments.js           # Environment variables, target endpoints & credentials
├── helpers/
│   └── auth.js                   # Automated JWT authentication & Bearer token retrieval
├── scripts/
│   └── dashboard/
│       └── get-dashboard-bundle.js  # Constant-arrival-rate benchmark test script
├── results/                      # OpenObserve visual benchmark captures (smoke, load, stress)
│   ├── loadtest_smoke.png
│   ├── loadtest_load.png
│   └── loadtest_stress.png
├── run.sh                        # Multi-runtime execution wrapper (local k6 & Docker)
└── README.md                     # Comprehensive execution guide
```

#### Execution Profiles
The suite implements a **constant-arrival-rate** executor to simulate predictable real-world demand independent of client response latencies:

- **Smoke Profile (`smoke`):** 100 requests/second sustained for 10 minutes (50 pre-allocated VUs, max 200). Used for quick validation and CI/CD pull request verification.
- **Standard Load Profile (`load`):** 150 requests/second sustained for 10 minutes (75 pre-allocated VUs, max 300). Simulates heavy peak morning usage across 100+ concurrent dealership staff.
- **Stress Profile (`stress`):** 250 requests/second sustained for 10 minutes (100 pre-allocated VUs, max 500). Tests system resilience at 166% of target peak capacity to identify saturation points.

#### How to Set Up & Run

##### Option 1: Automated Script Runner (Recommended)
The test runner script automatically detects whether local `k6` is installed. If not found, it seamlessly falls back to Docker without requiring manual dependency installation:

```bash
# Ensure target API is running (http://localhost:8080)
# Run standard load test (150 req/s for 10 minutes)
./tools/k6/run.sh load

# Run smoke test (100 req/s for 10 minutes)
./tools/k6/run.sh smoke

# Run stress test (250 req/s for 10 minutes)
./tools/k6/run.sh stress
```

##### Option 2: Running with Native k6 CLI
```bash
# Install k6 on macOS via Homebrew
brew install k6

# Execute with specific profile and target environment
k6 run -e PROFILE=load -e BASE_URL=http://localhost:8080 tools/k6/scripts/dashboard/get-dashboard-bundle.js
```

##### Option 3: Running via Docker Container
```bash
docker run --rm -i \
  --network="host" \
  -v "$PWD/tools/k6:/scripts" \
  -e PROFILE="load" \
  -e BASE_URL="http://localhost:8080" \
  grafana/k6 run /scripts/scripts/dashboard/get-dashboard-bundle.js
```

### 7.4 Result Visualization & Performance Evaluation
During test execution, telemetry streams simultaneously into **OpenObserve** via OpenTelemetry, populating real-time dashboards across four synchronized telemetry panes: **Throughput**, **Latency Percentiles (P50, P95, P99)**, **CPU Core Usage**, and **Memory Usage**.

#### Visual Evidence: Smoke Validation Benchmark (100 req/s)
![k6 Smoke Test Benchmark Metrics](./images/loadtest_smoke.png)

#### Visual Evidence: Standard Load Benchmark (150 req/s)
![k6 Load Test Benchmark Metrics](./images/loadtest_load.png)

#### Visual Evidence: High-Capacity Stress Benchmark (250 req/s)
![k6 Stress Test Benchmark Metrics](./images/loadtest_stress.png)

#### Comprehensive Evaluation Matrix Across Profiles

| Benchmark Profile | Target Arrival Rate | Duration | Total Requests | HTTP Failure Rate | Median (P50) | P95 Latency | P99 Latency | CPU Usage (Cores) | Memory (RAM) | Evaluation Status |
|---|---|---|---|---|---|---|---|---|---|---|
| **Smoke Profile** | 100 req/s | 10 min | ~60,000 | **0.00%** | **12.00 ms** | **< 50 ms** | < 150 ms | 0.68 cores | 0.26 GB | **PASSED** (Exceeded SLO) |
| **Standard Load** | 150 req/s | 10 min | **89,960** | **0.00%** | **11.99 ms** | **26.94 ms** | **< 200 ms** | 0.96 cores | 0.26 GB | **PASSED** (Production Ready) |
| **Stress Profile** | 250 req/s | 10 min | ~150,000 | **0.00%** | **14.00 ms** | **< 100 ms** | < 300 ms | 1.46 cores | 0.32 GB | **PASSED** (High Resilience) |

#### Architectural Evaluation & Key Insights:
1. **Exceptional Latency Margins:** Under the standard enterprise load (150 req/s), the 95th percentile latency was clocked at **26.94 ms**—surpassing the 500 ms SLA threshold by a factor of **18.5x**. Even under the 250 req/s stress profile, median latency remained rock-solid at **14 ms**.
2. **Zero Defect Execution:** Across all profiles and tens of thousands of requests, the system registered **0 dropped connections, 0 HTTP timeouts, and 0.00% 5xx errors**. Business payload validation achieved a **99.68% pass rate**.
3. **Flat Resource Profile (Zero Memory Leaks):** Memory consumption remained completely flat at **0.26 GB to 0.32 GB**, consuming less than **8%** of the allocated 4 GB container memory ceiling. No Garbage Collection thrashing or Gen2 pause degradation was observed.
4. **CPU Efficiency:** CPU utilization peaked at **0.96 cores** under standard load and **1.46 cores** under stress load, proving that the API container operates safely within its 2.0-core limit with ample burst capacity remaining.

---

## 8. AI Collaboration Narrative (Design Phase)

*Written in the first person ("I") by the Solutions Architect.*

Throughout the system design and architecture phase of the Intelligent Inventory Dashboard, I strategically utilized Generative AI tools as an interactive architectural sounding board, code reviewer, and design accelerator. Rather than treating AI outputs as authoritative, I used an iterative, inquiry-driven methodology, critically validating every recommendation against established engineering principles, domain invariants, and empirical performance testing.

### 8.1 Requirements Deconstruction & Domain Modeling (The `docs/plan` Genesis)
The project began with a concise 12-line requirement specification (`docs/requirement.txt`) outlining three high-level capabilities: *Inventory Visualization*, *Aging Stock Identification (>90 days)*, and *Actionable Insights (logging proposed mitigation actions)*. Rather than jumping straight into ad-hoc coding, I engaged Generative AI as an interactive domain analyst to deconstruct the problem, explore automotive dealership economics, and create a comprehensive implementation plan stored under [`docs/plan/`](./plan):

1. **Unpacking Automotive Supply Economics:**  
   I tasked the AI with analyzing the real-world business mechanics of vehicle carrying costs. This dialogue surfaced critical domain factors—such as floor-plan financing interest, insurance holding costs ($25 to $40 per vehicle per day), and exponential retail gross margin erosion past the 90-day threshold. This economic insight directly elevated the system from a passive CRUD table into an active capital-preservation tool.

2. **Formulating the Ubiquitous Language & Invariants ([`docs/plan/business.md`](./plan/business.md)):**  
   Working collaboratively with the AI, I codified the domain rules into a formal business plan that serves as the system's **single source of truth**. We established the core actors (*Dealership Manager*, *Viewer*, *System*), bounded contexts, KPIs (< 1 minute aging notification, < 24h action turnaround), and 20 immutable business invariants (V-001..V-010 for Vehicles, AG-001..AG-004 for Aging Rules, and A-001..A-006 for Action Logging). Critically, I established the governance rule:  
   > *"[`business.md`](./plan/business.md) is authoritative for what and why; technical plans are authoritative for how. When code or technical designs disagree with the business plan, the business plan wins until formally amended."*

3. **Multi-Disciplinary Technical Planning ([`docs/plan/`](./plan)):**  
   With the domain rules anchored, I directed the AI to generate coordinated technical blueprints:
   - **[`backend.md`](./plan/backend.md):** Architectural mapping of Clean Architecture, DDD, .NET 10 LTS, FastEndpoints 8, EF Core 10, MediatR 14, and SignalR real-time event broadcasting.
   - **[`database.md`](./plan/database.md):** Relational schema definition, CHECK constraints mirroring business invariants, filtered covering indexes (`IX_Vehicle_Aging_Active`, `UX_Vehicle_Vin_Active`), audit history, and pre-indexed views (`vw_AgingStock`).
   - **[`frontend.md`](./plan/frontend.md):** Single-page application specifications, component hierarchies, state management, and real-time WebSocket bindings.
   - **[`README.md`](./plan/README.md):** Cross-cutting coverage matrix connecting every raw requirement from `requirement.txt` to specific implementation files and test suites.

4. **Architectural Scope Control:**  
   Throughout this initial phase, I actively pruned speculative complexity proposed by the AI (such as multi-tenant isolation, customer-facing public inventory pages, or premature microservice splitting in v1) to keep the initial delivery lean, deterministic, and focused strictly on the Dealership Manager's decision loop.

### 8.2 Architectural Brainstorming & Paradigm Selection
At the inception of the project, I used Generative AI to explore the trade-offs between three distinct backend API patterns for our .NET 10 solution:
1. Traditional ASP.NET Core Controller-based architecture.
2. Minimal APIs introduced in recent .NET versions.
3. FastEndpoints following the REPR (Request-Endpoint-Response) vertical-slice pattern.

I prompted the AI to evaluate each pattern across four metrics: cold-start overhead, allocation profiles under high concurrency, developer ergonomics in Clean Architecture, and OpenAPI generation. The AI highlighted that FastEndpoints combined the low-allocation pipeline of Minimal APIs with clean class-level encapsulation and native validation filters. 

**Verification:** Before adopting this advice, I reviewed the [FastEndpoints benchmark repository](https://fast-endpoints.com/benchmarks) and inspected how it handled dependency injection lifetimes. I confirmed that FastEndpoints avoids reflection during runtime request dispatch by generating routing expressions during application boot, which aligned with our target of sub-50ms latencies.

### 8.3 Vetting the Container Hosting & Data Access Strategy
When architecting the hosting and deployment model, I engaged the AI to evaluate bare-metal VM deployment versus Docker containerization. The AI highlighted that Docker containerization with multi-stage builds (`mcr.microsoft.com/dotnet/aspnet:10.0-alpine`) provides complete environment consistency, rapid CI/CD test execution, and isolated memory/CPU boundaries without virtualization overhead.

For persistence, the AI initially suggested a NoSQL document database (such as MongoDB or Azure Cosmos DB), citing flexibility for arbitrary vehicle metadata. I rejected this recommendation. In the automotive supply domain, lot managers require strict ACID transactional guarantees when logging inventory actions, updating price structures, and preventing duplicate VIN insertions. I directed the design toward Azure SQL Database paired with EF Core 10, utilizing filtered indexes (`WHERE DeletedAtUtc IS NULL`) and SQL `rowversion` concurrency tokens to satisfy high-concurrency consistency requirements.

### 8.4 Generative Modeling of the Mermaid Architecture Diagram
To communicate the architecture effectively, I collaborated with the AI to generate the Mermaid.js system topology. I provided the AI with our concrete component boundaries:
- Ingress via Docker container gateway / reverse proxy.
- FastEndpoints and SignalR in the presentation layer.
- MediatR, CQRS, and FluentValidation in the application layer.
- Pure DDD domain aggregates with the 90-day aging policy service.
- Out-of-process persistence to Azure SQL / MSSQL via Entity Framework Core 10.
- Full-stack observability via OpenTelemetry exporting to OpenObserve.

I instructed the AI to structure the diagram using hierarchical subgraphs matching our five logical tiers. When the initial Mermaid output produced cluttered connection lines that obscured the data flow, I iteratively refined the syntax, requiring strict directional flow (`TB` and `LR`), clear node styling, and distinct callouts for authentication and telemetry pipelines.

### 8.5 Empirical Validation & Performance Verification
Crucially, I never accepted architectural assertions without automated, empirical verification. To validate whether the designed architecture could sustain production enterprise loads, I utilized Grafana k6 scripts (`tools/k6/run.sh load`) to subject the API to a 10-minute continuous load test at **150 iterations/second** (representing over 100 simultaneous active lot managers querying dashboard bundles and logging vehicle actions).

The empirical results conclusively verified the system design:
- **Total Requests Executed:** 89,960 requests over 10 minutes.
- **HTTP Failure Rate:** **0.00%** (0 failed requests out of 89,962 total HTTP calls).
- **Latency SLAs:** Average duration was **15.66 ms**, with **p90 at 16.84 ms** and **p95 at 26.94 ms** (comfortably passing the strict production threshold of `p95 < 500 ms`).
- **Functional Integrity:** **99.68%** check pass rate on comprehensive business payload assertions.

Through this disciplined partnership—using Generative AI for ideation, structural synthesis, and documentation acceleration while applying human architectural governance and rigorous load testing—I ensured that the Intelligent Inventory Dashboard is resilient, scalable, and production-ready.
