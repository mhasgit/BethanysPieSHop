# Logging and Monitoring

This document describes how logging is used in the codebase and how to set up and operate it in the cloud. It is intended for the DevOps engineer taking over the project.

---

## What’s in the codebase

### Web app

- **Registration:** In **`BethanysPieShop/Program.cs`**, the app calls **`builder.Services.AddApplicationInsightsTelemetry()`**. That registers the Application Insights telemetry provider and wires it to the standard logging pipeline.
- **Usage:** The app uses **`ILogger<T>`** throughout:
  - **`OrderRepository`** – Logs when an order is created and when status is updated (Information).
  - **`OrderController`** – Logs empty-cart checkout (Warning) and order creation (Information).
  - **`OrdersApiController`** – Logs order not found for status update (Warning), enqueue failures (Error), and successful status updates (Information).
  - **`OrderStatusQueueService`** – Logs missing queue config (Error), successful enqueue (Information), and enqueue failures (Error).
- **Levels:** Information for normal operations, Warning for expected edge cases (e.g. not found), Error for exceptions or config failures.
- **Config:** **`appsettings.json`** and **`appsettings.Development.json`** contain a **`Logging`** section with **`LogLevel`** (e.g. `Default: Information`, `Microsoft.AspNetCore: Warning`). In Azure, Application Insights is enabled by the **`APPLICATIONINSIGHTS_CONNECTION_STRING`** app setting (set by Bicep); no extra code is required for the web app to send telemetry.

### Azure Function

- **Registration:** In **`BethanysPieShop.OrderStatusFunction/Program.cs`**, the host is configured with:
  - **`AddApplicationInsightsTelemetryWorkerService()`** – Application Insights for worker (non-HTTP) apps.
  - **`ConfigureFunctionsApplicationInsights()`** – Ensures Functions runtime and triggers are correlated with Application Insights.
- **Usage:** **`OrderStatusEmailFunction`** uses **`ILogger<OrderStatusEmailFunction>`** to log:
  - Trigger received and raw message (Information).
  - Deserialization errors (Error).
  - Invalid message / missing email (Warning).
  - Missing SendGrid key (Error).
  - Email sent (Information) or SendGrid failure (Error).
- In Azure, the Function App gets **`APPLICATIONINSIGHTS_CONNECTION_STRING`** from the Bicep template (same Application Insights as the web app), so both apps report to one place.

### Summary

- **ILogger** – Standard .NET logging interface; logs go to the configured providers (console, Application Insights, etc.).
- **Application Insights** – Captures traces, metrics, exceptions, and dependencies. When the connection string is set, ILogger output is also sent there, and the two apps are correlated (same App Insights resource).

---

## Cloud setup for DevOps

### What the Bicep already does

- Creates one **Application Insights** resource.
- Sets **`APPLICATIONINSIGHTS_CONNECTION_STRING`** on:
  - The **Web App** (app settings).
  - The **Function App** (app settings).

No extra resources are required for basic logging and tracing. After deployment, open the Application Insights resource in the Azure Portal to see:

- **Live Metrics** – Near real-time requests and failures.
- **Transaction search / Logs** – Traces, exceptions, and custom log messages from both the web app and the function.
- **Performance / Failures** – Request duration, failure rate, and dependency calls.

### Optional improvements

1. **Diagnostic settings** – Send App Service (or Function App) logs (e.g. HTTP logs, detailed errors) to a Log Analytics workspace or storage. You can add a Log Analytics workspace and diagnostic settings to the Bicep if you want everything in one IaC.
2. **Retention** – In Application Insights, configure data retention (default 90 days; can be reduced to control cost).
3. **Alerts** – Create alerts on:
   - Failure rate or 5xx responses.
   - Exception count.
   - Queue depth or function execution failures (if you use custom metrics or logs).
4. **Sampling** – For high traffic, enable sampling in Application Insights to reduce volume and cost while keeping a representative sample.

### For interviews

- **ILogger vs Application Insights:** ILogger is the abstraction (logs go to all registered providers); Application Insights is one provider that stores and correlates telemetry in Azure.
- **Correlation:** Using the same Application Insights resource for the web app and the function allows you to follow a request or “order status update” across both (e.g. API call → queue message → function execution).
- **Cost:** Application Insights is charged by data volume and retention. Sampling and retention policies help control cost; explain how you’d tune them for production.

---

## Quick reference

| Component   | How logging is enabled | Where config lives in Azure |
|------------|------------------------|-----------------------------|
| Web app    | `AddApplicationInsightsTelemetry()` + ILogger | App Service → Configuration → Application settings: `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| Function   | `AddApplicationInsightsTelemetryWorkerService()` + `ConfigureFunctionsApplicationInsights()` + ILogger | Function App → Configuration → Application settings: `APPLICATIONINSIGHTS_CONNECTION_STRING` |

Both point to the same Application Insights resource created by the Bicep template.
