# Deployment Guide – Azure

This guide describes how to deploy Bethany's Pie Shop to Azure: infrastructure first, then the web app and the Azure Function, including database and migrations. It covers **command-line (Bicep/ARM)** deployment and **manual provisioning in the Azure Portal (UI)**. It is intended for the DevOps engineer taking over the project.

---

## Overview

1. **Deploy infrastructure** – Either:
   - **CLI:** Use the Bicep (or ARM) template in `azure/` to create all resources in one go, or
   - **Portal:** Manually create resource group, SQL Server + database, Application Insights, Storage Account + queue, App Service Plan + Web App, and Function App in the Azure Portal (see [Manual provisioning in Azure Portal (UI)](#manual-provisioning-in-azure-portal-ui)).
2. **Apply database migrations** – Run EF Core migrations against the Azure SQL database (see [Database](#database) below).
3. **Deploy the web app** – Publish the ASP.NET Core project and deploy to the Web App (CLI or portal).
4. **Deploy the Azure Function** – Publish the Function project and deploy to the Function App (CLI or portal).
5. **Configure secrets** – Ensure SendGrid API key (and any overrides) are set in the Function App; never commit real credentials to the repo.

---

## Prerequisites

- **Azure subscription** – Required for both CLI and portal. For CLI: log in with `az login`.
- **For CLI/Bicep deployment:** **Azure CLI** ([Install](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)), **Bicep** (bundled with Azure CLI: `az bicep version`; or [Bicep CLI](https://github.com/Azure/bicep/releases)).
- **For manual portal provisioning:** A browser and access to [Azure Portal](https://portal.azure.com); no CLI or Bicep needed for infrastructure.
- **.NET 8 SDK** – For building and publishing the web app and function (needed for code deployment in both approaches).
- **Azure Functions Core Tools** (optional) – For `func azure functionapp publish`; [install](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#install-the-azure-functions-core-tools).

**Secrets:** Do **not** put real SQL passwords or SendGrid API keys in `azure/main.parameters.json` in source control. Use pipeline secret variables or Azure Key Vault references and override parameters at deploy time. When using the portal, enter secrets only in the resource Configuration blades; they are stored securely in the service.

---

## Infrastructure

### Create resource group

```bash
az group create --name <resource-group-name> --location <region>
# Example:
az group create --name rg-pieshop-prod --location eastus
```

### Deploy Bicep (recommended)

```bash
az deployment group create \
  --resource-group <resource-group-name> \
  --template-file azure/main.bicep \
  --parameters azure/main.parameters.json
```

To override sensitive parameters from the command line (avoid storing them in the parameters file):

```bash
az deployment group create \
  --resource-group <resource-group-name> \
  --template-file azure/main.bicep \
  --parameters azure/main.parameters.json \
  --parameters sqlAdministratorLoginPassword='<secure-password>' \
  --parameters sendGridApiKey='<sendgrid-key>'
```

Or use Key Vault references in a separate parameters file and pass that file instead.

### Deploy ARM template (optional)

If you prefer to deploy the compiled ARM template:

```bash
bicep build azure/main.bicep
# This updates azure/main.json

az deployment group create \
  --resource-group <resource-group-name> \
  --template-file azure/main.json \
  --parameters azure/main.parameters.json
```

After deployment, note:

- **Web App name** – e.g. `pieshopweb001`
- **Function App name** – e.g. `pieshoporderstatus001`
- **SQL Server** – e.g. `pieshopsqlserver001.database.windows.net` (for connection string and firewall)

---

## Manual provisioning in Azure Portal (UI)

If you prefer to create all resources by hand in the Azure Portal instead of using Bicep/ARM, follow this sequence. Use the same names as the Bicep (e.g. `pieshopweb001`, `pieshopdb`) so connection strings and app settings match the rest of this guide. Create everything in the same **region** (e.g. East US).

### 1. Resource group

1. In the portal, search for **Resource groups** → **Create**.
2. **Subscription** and **Resource group**: e.g. `rg-pieshop-prod`.
3. **Region**: e.g. East US (remember it for all other resources).
4. **Review + create** → **Create**.

---

### 2. SQL Server and database

1. Search **SQL databases** → **Create** → choose **Single database**.
2. **Resource group**: Select the group you created.
3. **Database name**: `pieshopdb`.
4. **Server**: Click **Create new**:
   - **Server name**: e.g. `pieshopsqlserver001` (must be globally unique; try a suffix if taken).
   - **Location**: Same as resource group.
   - **Authentication**: **Use SQL authentication**.
   - **Server admin login**: e.g. `pieshopdbadm`.
   - **Password**: Set a strong password and store it securely.
5. Back on the database blade, **Compute + storage**: **Basic** (or Standard if you prefer).
6. **Review + create** → **Create**.

**Firewall (allow Azure services):**

1. Go to the **SQL Server** resource (not the database) → **Networking** (or **Security** → **Networking**).
2. Under **Firewall rules**, ensure **Allow Azure services and resources within Azure to access this server** is **Yes** (or add a rule 0.0.0.0–255.255.255.255 for Azure IPs).
3. To run migrations from your PC, add a rule: **Add your client IPv4 address** (or add a custom rule with your IP).

**Connection string (save for later):**

- SQL Server → **Connection strings** (or build it):  
  `Data Source=tcp:pieshopsqlserver001.database.windows.net,1433;Initial Catalog=pieshopdb;User Id=pieshopdbadm@pieshopsqlserver001;Password=<your-password>;`

---

### 3. Application Insights

1. Search **Application Insights** → **Create**.
2. **Resource group**: Same group.
3. **Name**: e.g. `pieshopinsights001` (or add a unique suffix).
4. **Region**: Same as resource group.
5. **Create**.
6. After creation, open the resource → **Overview** (or **Properties**). Copy the **Connection string** and save it; you will use it for both the Web App and the Function App.

---

### 4. Storage account and queue

1. Search **Storage accounts** → **Create**.
2. **Resource group**: Same group. **Storage account name**: e.g. `pieshopstor001` (globally unique; try a suffix if needed). **Region**: Same. **Performance**: Standard. **Redundancy**: LRS.
3. **Advanced**: Enable **Require secure transfer (HTTPS)**. **Create**.
4. After creation, go to the storage account → **Access keys**. Copy **key1** (or key2) and the **Connection string** (you will need it for the Web App and Function App).

**Create the queue:**

1. In the storage account, under **Data storage**, open **Queues**.
2. **+ Queue**. **Name**: `order-status-changed` (must match the app and function code). **Create**.

---

### 5. App Service Plan and Web App

1. Search **App Service** → **Create** → **Web App**.
2. **Resource group**: Same group. **Name**: e.g. `pieshopweb001` (globally unique). **Publish**: Code. **Runtime stack**: .NET 8 (or .NET Core). **Operating system**: Windows. **Region**: Same.
3. **App Service Plan**: Create new → **Name**: e.g. `pieshopasp001`. **Pricing tier**: Free (F1) or Basic B1. **Create** (plan and app).
4. After the Web App is created, go to the Web App → **Configuration**:
   - **Connection strings** → **+ New connection string**:
     - **Name**: `BethanysPieShopDbContextConnection`
     - **Value**: The SQL connection string from step 2.
     - **Type**: SQLAzure
   - **Application settings** → **+ New application setting**:
     - `OrderStatusQueueConnection` = storage account connection string from step 4.
     - `APPLICATIONINSIGHTS_CONNECTION_STRING` = Application Insights connection string from step 3.
5. **Save** and restart the app if prompted.

---

### 6. Function App (consumption plan)

1. Search **Function App** → **Create**.
2. **Resource group**: Same group. **Function App name**: e.g. `pieshoporderstatus001` (globally unique). **Runtime stack**: .NET. **Version**: 8 (isolated). **Region**: Same.
3. **Hosting**: **Storage account** – select the storage account you created in step 4. **Plan type**: Consumption (Serverless).
4. **Create**.
5. After creation, go to the Function App → **Configuration** → **Application settings**:
   - **AzureWebJobsStorage** – Should already be set to the storage connection string; if not, add it.
   - **SendGridApiKey** – Your SendGrid API key (required for sending emails).
   - **SendGridFromEmail** – e.g. `noreply@example.com` (use a verified sender in production).
   - **SendGridFromName** – e.g. `Bethany's Pie Shop`.
   - **APPLICATIONINSIGHTS_CONNECTION_STRING** – Application Insights connection string from step 3.
6. **Save** and restart if prompted.

---

### 7. Summary of manual provisioning

| Resource | Example name | What to note |
|----------|--------------|--------------|
| Resource group | rg-pieshop-prod | Same region for all resources. |
| SQL Server | pieshopsqlserver001 | Admin login, password; firewall (Azure + your IP). |
| SQL Database | pieshopdb | Basic tier; connection string. |
| Application Insights | pieshopinsights001 | Connection string for Web App and Function. |
| Storage account | pieshopstor001 | Connection string; queue `order-status-changed`. |
| App Service Plan | pieshopasp001 | F1 or B1. |
| Web App | pieshopweb001 | Connection string + OrderStatusQueueConnection + App Insights. |
| Function App | pieshoporderstatus001 | AzureWebJobsStorage + SendGrid + App Insights. |

After this, continue with [Database](#database) (migrations and firewall), then [Web app deployment](#web-app-deployment) and [Function app deployment](#function-app-deployment) to deploy your code. The connection strings and app settings you set in the portal are the same ones the Bicep sets; only the method of provisioning differs.

---

## Database

### Firewall

The Bicep adds a firewall rule **AllowAllWindowsAzureIps** (0.0.0.0–255.255.255.255) so that Azure services (e.g. the Web App) can connect. For production, consider:

- Restricting the rule to your App Service outbound IPs, or
- Using Private Endpoints and disabling public access.

To run migrations or connect from your machine, add a firewall rule for your IP:

```bash
az sql server firewall-rule create \
  --resource-group <resource-group-name> \
  --server <sql-server-name> \
  --name AllowMyIP \
  --start-ip-address <your-ip> \
  --end-ip-address <your-ip>
```

### Connection string

Format (replace placeholders):

```
Data Source=tcp:<sql-server-name>.database.windows.net,1433;Initial Catalog=pieshopdb;User Id=<admin-login>@<sql-server-name>;Password=<password>;
```

You can copy the connection string from the Azure Portal: SQL Server → Settings → Connection strings, or from the Web App → Configuration → Connection strings → `BethanysPieShopDbContextConnection`.

### Applying migrations

The Bicep does **not** run EF Core migrations. You must apply them explicitly.

**Option A – One-off task / build pipeline**

- Run from a build agent or a one-off job that can reach Azure SQL (firewall allows its IP or it runs in Azure):
  ```bash
  cd BethanysPieShop
  dotnet ef database update --connection "<connection-string>"
  ```
- Ensure the project has `Microsoft.EntityFrameworkCore.Design` and the correct connection string. This creates or updates the database schema; seed data is applied by the web app on first run (`DbInitializer.Seed`).

**Option B – From your machine**

- Add a firewall rule for your IP (see above).
- Run the same command with the Azure SQL connection string. Use the same connection string the Web App will use so the schema matches.

**Recommendation:** Do **not** run migrations automatically from the web app startup in production unless that is an explicit design choice (e.g. simple single-instance app). Prefer a controlled migration step in CI/CD or a manual run.

---

## Web app deployment

Connection string and app settings (**OrderStatusQueueConnection**, **APPLICATIONINSIGHTS_CONNECTION_STRING**) are already set by the Bicep. You only need to deploy the application bits.

### CLI – Publish and zip deploy

From the repository root:

```bash
dotnet publish BethanysPieShop/BethanysPieShop.csproj -c Release -o ./publish-web
cd publish-web
zip -r ../web.zip .   # On Windows: Compress-Archive -Path * -DestinationPath ../web.zip
cd ..
az webapp deployment source config-zip \
  --resource-group <resource-group-name> \
  --name pieshopweb001 \
  --src web.zip
```

Alternative: **`az webapp up`** (from the project folder) can build and deploy in one step; see Azure CLI docs for your scenario.

### Manual – Portal

1. Locally: `dotnet publish BethanysPieShop/BethanysPieShop.csproj -c Release -o ./publish-web`. Zip the contents of `publish-web` (all files in the root of the zip).
2. In Azure Portal: open the Web App (**pieshopweb001** or your name) → **Deployment Center** or **Advanced Tools** (Kudu).
3. **Deployment Center:** Configure source (e.g. Local Git, GitHub, or Zip deploy) and upload the zip or drag-and-drop the folder.
4. **Kudu:** **Debug console** → **CMD** → go to `site/wwwroot`, delete existing content, and upload the zip then unzip; or use the **Zip Push Deploy** API.

After deployment, confirm in **Configuration** → **Application settings** and **Connection strings** that **BethanysPieShopDbContextConnection**, **OrderStatusQueueConnection**, and **APPLICATIONINSIGHTS_CONNECTION_STRING** are present (they should be from Bicep).

---

## Function app deployment

The Bicep sets **AzureWebJobsStorage**, **SendGridFromEmail**, **SendGridFromName**, and **APPLICATIONINSIGHTS_CONNECTION_STRING**. You must set **SendGridApiKey** in the Function App (Portal or Key Vault reference) for emails to work.

### CLI – Azure Functions Core Tools

From the repository root:

```bash
dotnet publish BethanysPieShop.OrderStatusFunction/BethanysPieShop.OrderStatusFunction.csproj -c Release
func azure functionapp publish pieshoporderstatus001
```

If the function app name or resource group is different, adjust. Core Tools will use your current Azure login and deploy the published output.

### CLI – Zip deploy

```bash
dotnet publish BethanysPieShop.OrderStatusFunction/BethanysPieShop.OrderStatusFunction.csproj -c Release -o ./publish-func
# Create zip of publish-func contents, then:
az functionapp deployment source config-zip \
  --resource-group <resource-group-name> \
  --name pieshoporderstatus001 \
  --src func.zip
```

### Manual – Portal

1. Publish: `dotnet publish BethanysPieShop.OrderStatusFunction/BethanysPieShop.OrderStatusFunction.csproj -c Release -o ./publish-func`.
2. Zip the contents of `publish-func`.
3. In Azure Portal: Function App (**pieshoporderstatus001**) → **Deployment Center** or **Overview** → deploy via zip upload / drag-and-drop.
4. In **Configuration** → **Application settings**, ensure **SendGridApiKey** is set (or a Key Vault reference). Verify **AzureWebJobsStorage**, **SendGridFromEmail**, **SendGridFromName**, and **APPLICATIONINSIGHTS_CONNECTION_STRING** (Bicep should have set these).

---

## Post-deploy checklist

1. **Web app** – Open the Web App URL. Confirm the site loads and you can browse pies (seed data is applied on first run if the database was empty).
2. **Database** – Place an order to confirm the app can read/write to Azure SQL.
3. **Queue and email** – Call **PATCH** `https://<web-app-url>/api/orders/1/status` with body `{ "status": "Shipped" }` (use an existing order ID). Check that the customer receives the order status email and that the Function App runs without errors.
4. **Application Insights** – In the Application Insights resource, open **Transaction search** or **Logs** and confirm traces from both the web app and the function (e.g. requests, queue trigger executions, and your ILogger messages).

---

## Summary

| Step | CLI / Bicep | Manual (Portal) |
|------|-------------|------------------|
| Resource group | `az group create --name <rg> --location <region>` | Portal → Resource groups → Create |
| Infra | `az deployment group create --resource-group <rg> --template-file azure/main.bicep --parameters azure/main.parameters.json` (override secrets as needed) | Portal: create SQL Server + DB, App Insights, Storage + queue `order-status-changed`, App Service Plan + Web App, Function App; set all connection strings and app settings (see [Manual provisioning in Azure Portal (UI)](#manual-provisioning-in-azure-portal-ui)) |
| DB migrations | `dotnet ef database update --connection "<azure-sql-connection-string>"` from `BethanysPieShop` (after firewall allows your IP or build agent) | Same |
| Web app | Publish → zip → `az webapp deployment source config-zip` or Portal deploy | Publish locally → Portal: Web App → Deployment Center / Kudu → zip deploy |
| Function | `func azure functionapp publish <function-app-name>` or zip deploy; set **SendGridApiKey** in Portal | Publish locally → Portal: Function App → Deployment Center → zip; set **SendGridApiKey** in Configuration |
| Verify | Site loads, place order, PATCH order status, check email and Application Insights | Same |

For infrastructure details, see [azure-iac-reference.md](azure-iac-reference.md). For local setup, see [setup-and-run.md](setup-and-run.md).
