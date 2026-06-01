# Setup and Run (Local Development)

This guide explains how to set up the repository and run the web app and Azure Function locally. It is intended for the DevOps engineer (and developers) taking over the project.

## Prerequisites

- **.NET 8 SDK** – [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** – Local SQL Server, LocalDB (e.g. with Visual Studio), or SQL Server in a container
- **Optional for queue/function locally:** Azure Storage Emulator or **Azurite** – so the web app can send messages and the Function can read from a queue
- **Optional for emails:** **SendGrid** account and API key – required for the Azure Function to send order status emails (can be left empty locally if you only want to test queue flow)

## Repository layout

- **BethanysPieShop** – ASP.NET Core 8 web application (MVC + Razor Pages, EF Core, Identity)
- **BethanysPieShop.OrderStatusFunction** – Azure Function (isolated worker, .NET 8) that reads from the queue and sends emails via SendGrid
- **BethanysPieShopTests** – Unit tests
- **azure/** – Bicep/ARM templates for deploying infrastructure

## Web app

### Connection string

The web app expects a SQL connection string:

- **Key:** `ConnectionStrings:BethanysPieShopDbContextConnection`
- **Local example** (in `appsettings.Development.json`):

  ```json
  "ConnectionStrings": {
    "BethanysPieShopDbContextConnection": "Server=.;Initial Catalog=testdb001;MultipleActiveResultSets=False;Integrated Security=True;TrustServerCertificate=True;Connection Timeout=30;"
  }
  ```

Adjust `Server` and `Initial Catalog` to your local instance and database name. The app creates the database and applies migrations on first run if it does not exist (via `DbInitializer.Seed`).

### Queue connection (optional for local)

To test order status → queue → function locally, the web app needs the storage queue connection:

- **Key:** `OrderStatusQueueConnection` (or `ConnectionStrings:OrderStatusQueueConnection`)
- **With Azurite:** e.g. `UseDevelopmentStorage=true` (default Azurite endpoints), or the explicit connection string to your Azurite queue endpoint.

If you omit it, the web app will throw at startup when the queue service is resolved (it validates the setting). For local-only UI without queue, you could point it to a real Azure Storage connection string or run Azurite.

### Run

From the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project BethanysPieShop
```

Or open the solution in Visual Studio and set **BethanysPieShop** as the startup project, then F5.

- First run: the app seeds categories and pies (with image URLs) if the database is empty.
- Default URL is typically `https://localhost:7xxx` or `http://localhost:5xxx` (check the console).

## Azure Function (order status email)

### Configuration

Settings are in **`BethanysPieShop.OrderStatusFunction/local.settings.json`** (not deployed; for local only):

| Key | Purpose | Example (local) |
|-----|---------|------------------|
| **AzureWebJobsStorage** | Storage account for Functions runtime and queue trigger | `UseDevelopmentStorage=true` (Azurite) |
| **FUNCTIONS_WORKER_RUNTIME** | Runtime | `dotnet-isolated` |
| **SendGridApiKey** | SendGrid API key for sending email | Leave `""` or set a key |
| **SendGridFromEmail** | Sender email | `noreply@example.com` |
| **SendGridFromName** | Sender display name | `Bethany's Pie Shop` |

The function is triggered by the queue **`order-status-changed`**. The queue name is hardcoded in the function and in the web app’s queue service; do not change it without updating both.

### Run

1. **Start Azurite** (if using emulated storage), e.g.:

   ```bash
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

   Or use the Azurite extension in VS Code.

2. **Run the function** from the repo root:

   ```bash
   cd BethanysPieShop.OrderStatusFunction
   func start
   ```

   Or set **BethanysPieShop.OrderStatusFunction** as the startup project in Visual Studio and run (with Azure Functions Core Tools installed).

The function will listen for messages on the `order-status-changed` queue. If `SendGridApiKey` is empty, the function will fail when it tries to send email (you’ll see the error in logs). You can still verify that the trigger runs and deserializes the message.

## Order flow for testing (queue + function)

1. **Start Azurite** (so both web app and function use the same emulated queue).
2. **Start the web app** – place an order through the UI so an order exists.
3. **Start the Azure Function** – so it is listening for queue messages.
4. **Trigger a status update** – call the API to change order status so the web app enqueues a message:
   - **PATCH** `https://localhost:<port>/api/orders/{id}/status`
   - **Body (JSON):** `{ "status": "Shipped" }`
   - Use an order ID that exists (e.g. 1).
5. **Web app** updates the order in the DB and sends a JSON message to the queue `order-status-changed`.
6. **Function** is triggered, deserializes the message, and sends an email via SendGrid (if the key is set).

If SendGrid is not configured, the function will log and throw when sending; you can still confirm trigger and deserialization from the function logs.

## Summary

- **Web:** Set `ConnectionStrings:BethanysPieShopDbContextConnection` (and optionally `OrderStatusQueueConnection` for Azurite), then `dotnet run --project BethanysPieShop`.
- **Function:** Set `AzureWebJobsStorage` (e.g. Azurite), optionally `SendGridApiKey`, then run from `BethanysPieShop.OrderStatusFunction` with `func start`.
- **Queue name** must match in both: `order-status-changed`.

For deployment to Azure, see [deployment-guide.md](deployment-guide.md).
