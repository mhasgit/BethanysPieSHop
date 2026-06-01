# Azure Infrastructure as Code Reference

This document explains the files in the `azure/` folder used to deploy the Bethany's Pie Shop infrastructure to Azure. It is intended for the DevOps engineer taking over the project. Every line of the three files is listed and explained below.

## Purpose of the `azure/` folder

The folder contains **Infrastructure as Code (IaC)** for the full stack:

- **`main.bicep`** – Source of truth. Declares parameters, variables, and all Azure resources in Bicep syntax.
- **`main.json`** – Compiled ARM (Azure Resource Manager) template. Generated from the Bicep file (e.g. with `bicep build azure/main.bicep` or `az bicep build --file azure/main.bicep`). You can deploy either the Bicep file or this JSON; prefer Bicep for edits.
- **`main.parameters.json`** – Parameter values for the deployment. **Security:** Never commit real passwords or API keys here; use CI/CD secret parameters or Azure Key Vault references.

---

## main.bicep – Every line explained

### Line 1

```bicep
@description('Describes plan\'s pricing tier and instance size. Check details at https://azure.microsoft.com/en-us/pricing/details/app-service/')
```

**Explanation:** Bicep decorator that adds a description to the next parameter. This text is shown in the portal and in template documentation. The escaped single quote `\'` is used because the string is wrapped in single quotes.

---

### Lines 2–15

```bicep
@allowed([
  'F1'
  'D1'
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
  'P1'
  'P2'
  'P3'
  'P4'
])
```

**Explanation:** The `@allowed` decorator restricts the next parameter to one of these values. They are valid App Service Plan SKU names: F1 (Free), D1 (Shared), B1/B2/B3 (Basic), S1/S2/S3 (Standard), P1/P2/P3/P4 (Premium). Deployment fails if a different value is passed.

---

### Line 16

```bicep
param skuName string = 'F1'
```

**Explanation:** Declares a string parameter named `skuName` with default value `'F1'`. It defines the pricing tier (and instance size) of the App Service Plan that hosts the web app. F1 is the free tier.

---

### Line 17

```bicep

```

**Explanation:** Blank line (no code).

---

### Line 18

```bicep
@description('Describes plan\'s instance count')
```

**Explanation:** Decorator that describes the next parameter: it is the number of instances (scale-out count) for the App Service Plan.

---

### Lines 19–20

```bicep
@minValue(1)
@maxValue(3)
```

**Explanation:** Decorators that constrain the next parameter to an integer between 1 and 3 (inclusive). Values outside this range cause deployment validation to fail.

---

### Line 21

```bicep
param skuCapacity int = 1
```

**Explanation:** Integer parameter for the App Service Plan instance count. Default is 1. Used later in the `appServicePlan` resource as `capacity: skuCapacity`.

---

### Line 22

```bicep

```

**Explanation:** Blank line.

---

### Line 23

```bicep
@description('The admin user of the SQL Server')
```

**Explanation:** Decorator describing the next parameter as the SQL Server administrator login name.

---

### Line 24

```bicep
param sqlAdministratorLogin string
```

**Explanation:** String parameter for the Azure SQL Server administrator username (e.g. `pieshopdbadm`). No default; must be provided in the parameters file or at deploy time. Used when building the connection string and for SQL authentication.

---

### Line 25

```bicep

```

**Explanation:** Blank line.

---

### Line 26

```bicep
@description('The password of the admin user of the SQL Server')
```

**Explanation:** Decorator describing the next parameter as the SQL Server administrator password.

---

### Line 27

```bicep
@secure()
```

**Explanation:** Marks the next parameter as secure. Secure parameters are not logged or shown in deployment history in plain text. Use this for passwords and secrets.

---

### Line 28

```bicep
param sqlAdministratorLoginPassword string
```

**Explanation:** The SQL Server administrator password. Must be provided at deploy time (e.g. via parameters file or pipeline secret). Never commit a real value to source control.

---

### Line 29

```bicep

```

**Explanation:** Blank line.

---

### Line 30

```bicep
@description('Location for all resources.')
```

**Explanation:** Decorator describing the next parameter as the Azure region for all resources.

---

### Line 31

```bicep
param location string = resourceGroup().location
```

**Explanation:** String parameter for Azure region. Default is `resourceGroup().location`, so all resources are created in the same region as the resource group unless you override this parameter.

---

### Line 32

```bicep

```

**Explanation:** Blank line.

---

### Lines 33–34

```bicep
@description('SendGrid API key for order status emails. Leave empty to set in portal or Key Vault.')
@secure()
```

**Explanation:** First line describes the next parameter: the SendGrid API key used by the Function App to send order status emails; it can be left empty and set later in the portal or Key Vault. Second line marks the parameter as secure so it is not logged.

---

### Line 35

```bicep
param sendGridApiKey string = ''
```

**Explanation:** String parameter for the SendGrid API key. Default is empty string. The Function App reads this as an app setting; if empty at deploy time, you must set it later in the Function App configuration or via Key Vault reference.

---

### Line 36

```bicep

```

**Explanation:** Blank line.

---

### Line 37

```bicep
var appServicePlanName = 'pieshopasp001'
```

**Explanation:** Variable holding the name of the App Service Plan that will host the web app. Must be unique within the subscription in some contexts; this fixed name is used in the `appServicePlan` resource.

---

### Line 38

```bicep
var websiteName = 'pieshopweb001'
```

**Explanation:** Variable for the Web App (site) name. This is the hostname prefix (e.g. pieshopweb001.azurewebsites.net). Must be globally unique across all of Azure.

---

### Line 39

```bicep
var sqlserverName = 'pieshopsqlserver001'
```

**Explanation:** Variable for the logical Azure SQL Server name. The server’s full hostname will be `pieshopsqlserver001.database.windows.net`.

---

### Line 40

```bicep
var databaseName = 'pieshopdb'
```

**Explanation:** Variable for the SQL database name. The actual database created under the SQL Server is named `pieshopdb`.

---

### Line 41

```bicep
var storageAccountName = 'pieshopstor${uniqueString(resourceGroup().id)}'
```

**Explanation:** Variable for the Storage Account name. Storage account names must be globally unique and follow length and character rules. `uniqueString(resourceGroup().id)` generates a deterministic hash from the resource group ID so the name is unique per resource group and reproducible across deployments.

---

### Line 42

```bicep
var functionAppName = 'pieshoporderstatus001'
```

**Explanation:** Variable for the Azure Function App name. This is the name of the Function App that runs the order status email function (e.g. pieshoporderstatus001.azurewebsites.net). Must be globally unique.

---

### Line 43

```bicep
var functionAppPlanName = 'pieshopfuncasp001'
```

**Explanation:** Variable for the App Service Plan that hosts the Function App. Consumption plan (Y1) still uses a logical plan with this name.

---

### Line 44

```bicep
var appInsightsName = 'pieshopinsights${uniqueString(resourceGroup().id)}'
```

**Explanation:** Variable for the Application Insights resource name. The `uniqueString(resourceGroup().id)` suffix ensures a unique name per resource group and avoids collisions if you deploy to multiple resource groups.

---

### Line 45

```bicep

```

**Explanation:** Blank line.

---

### Line 46

```bicep
resource sqlServer 'Microsoft.Sql/servers@2021-02-01-preview' = {
```

**Explanation:** Declares a resource of type Azure SQL Server using API version 2021-02-01-preview. The symbolic name `sqlServer` is used elsewhere in the file to reference this resource (e.g. `parent: sqlServer`, `sqlServer.properties.fullyQualifiedDomainName`).

---

### Line 47

```bicep
  name: sqlserverName
```

**Explanation:** Sets the SQL Server’s name to the value of the variable `sqlserverName` (e.g. pieshopsqlserver001).

---

### Line 48

```bicep
  location: location
```

**Explanation:** Sets the Azure region for the SQL Server to the value of the `location` parameter.

---

### Lines 49–51

```bicep
  tags: {
    displayName: 'SQL Server'
  }
```

**Explanation:** Tags applied to the SQL Server resource. `displayName` is a friendly label often shown in the portal or in cost reports.

---

### Lines 52–56

```bicep
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
    version: '12.0'
  }
```

**Explanation:** `properties` block for the SQL Server. `administratorLogin` and `administratorLoginPassword` come from the parameters and define the server-level admin account. `version: '12.0'` is the SQL Server version (Azure SQL Database compatibility level).

---

### Line 57

```bicep
}
```

**Explanation:** Closes the `sqlServer` resource block.

---

### Line 58

```bicep

```

**Explanation:** Blank line.

---

### Line 59

```bicep
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-02-01-preview' = {
```

**Explanation:** Declares a child resource: a database under the SQL Server. The type is `Microsoft.Sql/servers/databases`; the parent is set on the next line.

---

### Line 60

```bicep
  parent: sqlServer
```

**Explanation:** Makes this database a child of the `sqlServer` resource. The database is created on that server.

---

### Line 61

```bicep
  name: databaseName
```

**Explanation:** The name of the database within the server (e.g. pieshopdb).

---

### Line 62

```bicep
  location: location
```

**Explanation:** Region for the database (same as the `location` parameter).

---

### Lines 63–65

```bicep
  tags: {
    displayName: 'Pie Shop Database'
  }
```

**Explanation:** Tags for the database resource; `displayName` is a friendly label.

---

### Lines 66–68

```bicep
  sku: {
    name: 'Basic'
  }
```

**Explanation:** SKU of the database. `Basic` is the lowest paid tier (e.g. 2 GB max, limited DTU). Use Standard/Premium for higher performance.

---

### Lines 69–72

```bicep
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 1073741824
  }
```

**Explanation:** `collation` sets the database collation (e.g. for string sort and comparison). `maxSizeBytes: 1073741824` is 1 GB maximum size for the Basic tier.

---

### Line 73

```bicep
}
```

**Explanation:** Closes the `sqlDatabase` resource block.

---

### Line 74

```bicep

```

**Explanation:** Blank line.

---

### Line 75

```bicep
resource allowAllWindowsAzureIps 'Microsoft.Sql/servers/firewallRules@2021-02-01-preview' = {
```

**Explanation:** Declares a firewall rule resource for the SQL Server. The symbolic name is `allowAllWindowsAzureIps`. This rule allows Azure services (e.g. App Service) to connect to the database.

---

### Line 76

```bicep
  parent: sqlServer
```

**Explanation:** This firewall rule belongs to the `sqlServer` resource.

---

### Line 77

```bicep
  name: 'AllowAllWindowsAzureIps'
```

**Explanation:** The rule name. This is a well-known name that the portal often uses for “Allow Azure services and resources within Azure to access this server.”

---

### Lines 78–81

```bicep
  properties: {
    endIpAddress: '255.255.255.255'
    startIpAddress: '0.0.0.0'
  }
```

**Explanation:** Defines the IP range that is allowed. 0.0.0.0 to 255.255.255.255 effectively allows all IPv4 addresses; in practice Azure treats this as “allow Azure datacenter IPs” for Azure services. For production, consider restricting to your App Service outbound IPs or using private endpoints.

---

### Line 82

```bicep
}
```

**Explanation:** Closes the firewall rule resource block.

---

### Line 83

```bicep

```

**Explanation:** Blank line.

---

### Line 84

```bicep
resource appServicePlan 'Microsoft.Web/serverfarms@2020-12-01' = {
```

**Explanation:** Declares an App Service Plan (server farm) that hosts the web app. Symbolic name `appServicePlan` is used when creating the Web App.

---

### Line 85

```bicep
  name: appServicePlanName
```

**Explanation:** Name of the plan (e.g. pieshopasp001).

---

### Line 86

```bicep
  location: location
```

**Explanation:** Region for the App Service Plan.

---

### Lines 87–89

```bicep
  tags: {
    displayName: 'HostingPlan'
  }
```

**Explanation:** Tags for the plan.

---

### Lines 90–93

```bicep
  sku: {
    name: skuName
    capacity: skuCapacity
  }
```

**Explanation:** `name` is the SKU from the `skuName` parameter (e.g. F1, B1). `capacity` is the instance count from `skuCapacity` (1–3). Together they define pricing and scale-out.

---

### Line 94

```bicep
}
```

**Explanation:** Closes the `appServicePlan` resource block.

---

### Line 95

```bicep

```

**Explanation:** Blank line.

---

### Line 96

```bicep
resource website 'Microsoft.Web/sites@2020-12-01' = {
```

**Explanation:** Declares the Web App (Azure App Service site) that will run the ASP.NET Core application.

---

### Line 97

```bicep
  name: websiteName
```

**Explanation:** Name of the Web App (e.g. pieshopweb001). This becomes the site name and part of the default hostname.

---

### Line 98

```bicep
  location: location
```

**Explanation:** Region for the Web App.

---

### Lines 99–102

```bicep
  tags: {
    'hidden-related:${appServicePlan.id}': 'empty'
    displayName: 'Pie Shop Website'
  }
```

**Explanation:** The `hidden-related` tag links the Web App to the App Service Plan in the portal (e.g. for “related resources”). `displayName` is a friendly label.

---

### Lines 103–105

```bicep
  properties: {
    serverFarmId: appServicePlan.id
  }
```

**Explanation:** Associates the Web App with the App Service Plan. `serverFarmId` must be the resource ID of the plan so the site runs on that plan.

---

### Line 106

```bicep
}
```

**Explanation:** Closes the `website` resource block.

---

### Line 107

```bicep

```

**Explanation:** Blank line.

---

### Line 108

```bicep
resource webSiteConnectionStrings 'Microsoft.Web/sites/config@2020-12-01' = {
```

**Explanation:** Declares a *config* resource for the Web App. This is the `config` sub-resource that holds connection strings (and app settings in a separate resource below). The resource name in ARM is `sites/config`, and the instance name will be `connectionstrings` (see next line).

---

### Line 109

```bicep
  parent: website
```

**Explanation:** This config belongs to the `website` resource (the Web App).

---

### Line 110

```bicep
  name: 'connectionstrings'
```

**Explanation:** For `Microsoft.Web/sites/config`, the `name` is the config section name. `connectionstrings` is the section where .NET reads `ConnectionStrings:*` (e.g. `BethanysPieShopDbContextConnection`).

---

### Lines 111–116

```bicep
  properties: {
    BethanysPieShopDbContextConnection: {
      value: 'Data Source=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};User Id=${sqlAdministratorLogin}@${sqlServer.properties.fullyQualifiedDomainName};Password=${sqlAdministratorLoginPassword};'
      type: 'SQLAzure'
    }
  }
}
```

**Explanation:** Defines one connection string. **BethanysPieShopDbContextConnection** is the key the app expects (e.g. `ConnectionStrings:BethanysPieShopDbContextConnection`). The **value** is an ADO.NET-style connection string: `Data Source=tcp:<server>,1433` (port 1433), `Initial Catalog=<db>`, `User Id=<login>@<server>` (Azure SQL requires the `@<server>` suffix), and `Password=<password>`. **type: 'SQLAzure'** tells the portal this is an Azure SQL connection string (for UI and optional encryption handling). The closing `}` ends the `properties` block and the resource block.

---

### Line 117

```bicep

```

**Explanation:** Blank line.

---

### Line 118

```bicep
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
```

**Explanation:** Declares an Application Insights component for application telemetry (logs, metrics, requests, dependencies).

---

### Line 119

```bicep
  name: appInsightsName
```

**Explanation:** Name of the Application Insights resource (e.g. pieshopinsights&lt;uniqueSuffix&gt;).

---

### Line 120

```bicep
  location: location
```

**Explanation:** Region for Application Insights.

---

### Lines 121–123

```bicep
  tags: {
    displayName: 'Application Insights'
  }
```

**Explanation:** Tags for the Application Insights resource.

---

### Line 124

```bicep
  kind: 'web'
```

**Explanation:** Component kind. `web` is used for web applications and is the typical choice for ASP.NET Core and Azure Functions.

---

### Lines 125–128

```bicep
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
```

**Explanation:** **Application_Type** aligns with the kind. **Request_Source: 'rest'** indicates the resource was created via REST/ARM (as opposed to the classic portal). These are standard values for ARM-created Application Insights.

---

### Line 129

```bicep
}
```

**Explanation:** Closes the `appInsights` resource block.

---

### Line 130

```bicep

```

**Explanation:** Blank line.

---

### Line 131

```bicep
var appInsightsConnectionString = appInsights.properties.ConnectionString
```

**Explanation:** Variable that gets the connection string of the Application Insights resource after deployment. This is used later to set `APPLICATIONINSIGHTS_CONNECTION_STRING` on both the Web App and the Function App so they send telemetry to this instance.

---

### Line 132

```bicep

```

**Explanation:** Blank line.

---

### Line 133

```bicep
resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
```

**Explanation:** Declares an Azure Storage Account. This one account is used for the Functions runtime (AzureWebJobsStorage) and for the application queue `order-status-changed`. No blob container is created in this template; pie images are referenced by URLs elsewhere.

---

### Line 134

```bicep
  name: storageAccountName
```

**Explanation:** Name of the storage account (e.g. pieshopstor&lt;uniqueString&gt;). Must be globally unique.

---

### Line 135

```bicep
  location: location
```

**Explanation:** Region for the storage account.

---

### Lines 136–138

```bicep
  tags: {
    displayName: 'Storage for Function and Queue'
  }
```

**Explanation:** Tags for the storage account.

---

### Lines 139–141

```bicep
  sku: {
    name: 'Standard_LRS'
  }
```

**Explanation:** Replication and performance tier. `Standard_LRS` is standard performance with locally redundant storage (LRS).

---

### Line 142

```bicep
  kind: 'StorageV2'
```

**Explanation:** Storage account kind. `StorageV2` is the current general-purpose type supporting blobs, queues, files, and tables.

---

### Lines 143–146

```bicep
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
```

**Explanation:** **supportsHttpsTrafficOnly** disables HTTP; only HTTPS is allowed. **minimumTlsVersion** enforces TLS 1.2 or higher for connections. Both improve security.

---

### Line 147

```bicep
}
```

**Explanation:** Closes the `storageAccount` resource block.

---

### Line 148

```bicep

```

**Explanation:** Blank line.

---

### Line 149

```bicep
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
```

**Explanation:** Builds the storage connection string from the deployed account. It uses the account name, the first key from `listKeys()`, and the public Azure suffix. This string is used for `AzureWebJobsStorage` (Function App) and `OrderStatusQueueConnection` (Web App).

---

### Line 150

```bicep

```

**Explanation:** Blank line.

---

### Line 151

```bicep
resource queueServices 'Microsoft.Storage/storageAccounts/queueServices@2023-01-01' = {
```

**Explanation:** Declares the default queue service on the storage account. Queues are created under this service.

---

### Line 152

```bicep
  parent: storageAccount
```

**Explanation:** This queue service is a child of the `storageAccount` resource.

---

### Line 153

```bicep
  name: 'default'
```

**Explanation:** The default queue service is always named `default` in Azure Storage.

---

### Line 154

```bicep
  properties: {}
```

**Explanation:** No properties are required for the default queue service; empty object is valid.

---

### Line 155

```bicep
}
```

**Explanation:** Closes the `queueServices` resource block.

---

### Line 156

```bicep

```

**Explanation:** Blank line.

---

### Line 157

```bicep
resource orderStatusQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
```

**Explanation:** Declares a queue resource. This is the queue the web app writes to and the Function App’s queue trigger reads from.

---

### Line 158

```bicep
  parent: queueServices
```

**Explanation:** The queue is a child of the `queueServices` resource (which is under the storage account).

---

### Line 159

```bicep
  name: 'order-status-changed'
```

**Explanation:** The queue name. It must match the name used in the web app’s `OrderStatusQueueService` and in the Function’s `QueueTrigger` attribute. Changing it requires code changes in both places.

---

### Line 160

```bicep
}
```

**Explanation:** Closes the `orderStatusQueue` resource block. No `properties` are required for a basic queue.

---

### Line 161

```bicep

```

**Explanation:** Blank line.

---

### Line 162

```bicep
resource functionAppPlan 'Microsoft.Web/serverfarms@2020-12-01' = {
```

**Explanation:** Declares the App Service Plan that will host the Azure Function App. For consumption plan (Y1), this is still a server farm resource with specific SKU settings.

---

### Line 163

```bicep
  name: functionAppPlanName
```

**Explanation:** Name of the plan (e.g. pieshopfuncasp001).

---

### Line 164

```bicep
  location: location
```

**Explanation:** Region for the Function App plan.

---

### Lines 165–167

```bicep
  tags: {
    displayName: 'Function App Plan'
  }
```

**Explanation:** Tags for the plan.

---

### Lines 168–171

```bicep
  sku: {
    name: 'Y1'
    capacity: 0
  }
```

**Explanation:** **Y1** is the consumption (serverless) plan for Azure Functions; you pay per execution. **capacity: 0** is required for consumption; scaling is managed by the platform.

---

### Line 172

```bicep
  kind: 'functionapp'
```

**Explanation:** Indicates this server farm is used for Function Apps (not a regular web app plan).

---

### Lines 173–175

```bicep
  properties: {
    reserved: false
  }
```

**Explanation:** **reserved: false** means the plan is not for Linux App Service in a reserved capacity; it is the normal consumption plan behavior.

---

### Line 176

```bicep
}
```

**Explanation:** Closes the `functionAppPlan` resource block.

---

### Line 177

```bicep

```

**Explanation:** Blank line.

---

### Line 178

```bicep
resource functionApp 'Microsoft.Web/sites@2020-12-01' = {
```

**Explanation:** Declares the Azure Function App (the actual site that runs your functions). The type is the same as a Web App (`Microsoft.Web/sites`); the `kind` and app settings distinguish it as a Function App.

---

### Line 179

```bicep
  name: functionAppName
```

**Explanation:** Name of the Function App (e.g. pieshoporderstatus001).

---

### Line 180

```bicep
  location: location
```

**Explanation:** Region for the Function App.

---

### Lines 181–184

```bicep
  tags: {
    'hidden-related:${functionAppPlan.id}': 'empty'
    displayName: 'Order Status Email Function'
  }
```

**Explanation:** `hidden-related` links the Function App to its plan in the portal. `displayName` is a friendly label.

---

### Line 185

```bicep
  kind: 'functionapp'
```

**Explanation:** Marks this site as a Function App so Azure uses the Functions runtime and host.

---

### Lines 186–188

```bicep
  properties: {
    serverFarmId: functionAppPlan.id
    httpsOnly: true
```

**Explanation:** **serverFarmId** attaches the Function App to the consumption plan. **httpsOnly** disables HTTP; only HTTPS is allowed.

---

### Line 189

```bicep
    siteConfig: {
```

**Explanation:** Starts the site configuration block (framework version, app settings, etc.).

---

### Line 190

```bicep
      netFrameworkVersion: 'v8.0'
```

**Explanation:** Targets .NET 8 runtime for the Function App. Must match the project’s target framework.

---

### Line 191

```bicep
      appSettings: [
```

**Explanation:** Begins the array of app settings (key-value pairs) for the Function App. These become environment variables and configuration for the app.

---

### Lines 192–195

```bicep
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
```

**Explanation:** **AzureWebJobsStorage** is required by the Azure Functions runtime. It points to the storage account used for internal queues and blobs (e.g. trigger state) and is also the default connection for bindings like the queue trigger. Here it uses the same account where `order-status-changed` lives.

---

### Lines 196–199

```bicep
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
```

**Explanation:** **FUNCTIONS_EXTENSION_VERSION** sets the Functions host version. `~4` means the latest 4.x. Required for .NET isolated worker and v4 features.

---

### Lines 200–203

```bicep
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
```

**Explanation:** **FUNCTIONS_WORKER_RUNTIME** tells the host to use the .NET isolated worker process (out-of-process). Required for .NET 5+ isolated model; use `dotnet` for in-process .NET Core 3.1.

---

### Lines 204–207

```bicep
        {
          name: 'SendGridApiKey'
          value: sendGridApiKey
        }
```

**Explanation:** **SendGridApiKey** is read by the function code to authenticate with SendGrid. The value comes from the `sendGridApiKey` parameter; if empty at deploy time, set it later in the portal or via Key Vault reference.

---

### Lines 208–211

```bicep
        {
          name: 'SendGridFromEmail'
          value: 'noreply@example.com'
        }
```

**Explanation:** **SendGridFromEmail** is the “from” email address for order status emails. Replace with a verified sender in SendGrid for production.

---

### Lines 212–215

```bicep
        {
          name: 'SendGridFromName'
          value: 'Bethany\'s Pie Shop'
        }
```

**Explanation:** **SendGridFromName** is the display name for the sender. The backslash escapes the single quote inside the string.

---

### Lines 216–219

```bicep
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
```

**Explanation:** **APPLICATIONINSIGHTS_CONNECTION_STRING** connects the Function App to the same Application Insights resource as the web app so all telemetry is in one place.

---

### Line 220

```bicep
      ]
```

**Explanation:** Closes the `appSettings` array.

---

### Line 221

```bicep
    }
```

**Explanation:** Closes the `siteConfig` block.

---

### Line 222

```bicep
  }
```

**Explanation:** Closes the `properties` block of the Function App.

---

### Line 223

```bicep
}
```

**Explanation:** Closes the `functionApp` resource block.

---

### Line 224

```bicep

```

**Explanation:** Blank line.

---

### Line 225

```bicep
resource webSiteAppSettings 'Microsoft.Web/sites/config@2020-12-01' = {
```

**Explanation:** Declares the *app settings* config for the Web App (not connection strings; those were in `webSiteConnectionStrings`). This is the `appsettings` section the app reads via `IConfiguration`.

---

### Line 226

```bicep
  parent: website
```

**Explanation:** This config belongs to the `website` (Web App) resource.

---

### Line 227

```bicep
  name: 'appsettings'
```

**Explanation:** For `sites/config`, the name `appsettings` is the section that holds application settings (environment variables in the app). Connection strings are in a separate config resource named `connectionstrings`.

---

### Lines 228–232

```bicep
  properties: {
    OrderStatusQueueConnection: storageConnectionString
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
  }
}
```

**Explanation:** **OrderStatusQueueConnection** is the storage connection string the web app uses to send messages to the `order-status-changed` queue (read by `OrderStatusQueueService`). **APPLICATIONINSIGHTS_CONNECTION_STRING** sends the web app’s telemetry to Application Insights. Both use the variables defined earlier. The final `}` closes the resource block.

---

## main.parameters.json – Every line explained

The file content is a single JSON object. Each logical part is explained.

### Line 1 (opening and schema)

```json
{"$schema":"https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#","contentVersion":"1.0.0.0","parameters":{
```

**Explanation:** **$schema** points to the JSON schema for Azure deployment parameters files (used for validation and editor support). **contentVersion** is a version for the parameters file (e.g. 1.0.0.0). **parameters** starts the object that holds each parameter’s value.

---

### sqlAdministratorLogin

```json
"sqlAdministratorLogin":{"value":"pieshopdbadm"}
```

**Explanation:** Provides the value for the Bicep parameter `sqlAdministratorLogin`. This is the SQL Server administrator username (e.g. pieshopdbadm). **Warning:** Do not commit real credentials; use pipeline variables or Key Vault references in production.

---

### sqlAdministratorLoginPassword

```json
,"sqlAdministratorLoginPassword":{"value":"YD*Upp!R4BzNKua$f@Y"}
```

**Explanation:** Provides the value for the secure parameter `sqlAdministratorLoginPassword`. This is the SQL Server admin password. **Security:** Never commit real passwords. Replace with a Key Vault reference (e.g. `"reference": { "keyVault": { "id": "..." }, "secretName": "..." }`) or pass via pipeline secret and omit from the file.

---

### sendGridApiKey

```json
,"sendGridApiKey":{"value":""}}
```

**Explanation:** Provides the value for `sendGridApiKey`. Empty string means the Function App will not have a SendGrid key at deploy time; you must set it later in the Function App configuration or Key Vault. The closing `}}` ends the `parameters` object and the root object.

---

**Note:** The other Bicep parameters (`skuName`, `skuCapacity`, `location`) have defaults in the template, so they do not need to appear in the parameters file unless you want to override them.

---

## main.json (ARM template)

**main.json** is the ARM (JSON) equivalent of **main.bicep**. It is generated by the Bicep compiler and contains:

- **parameters** – Same parameters as Bicep, with types, defaults, and metadata (e.g. descriptions, allowed values).
- **variables** – Same variable names and expressions, written in ARM template language (e.g. `[format('pieshopstor{0}', uniqueString(resourceGroup().id))]`).
- **resources** – Same resources with `type`, `apiVersion`, `name`, `location`, `properties`, and **dependsOn** where the compiler inferred dependencies (e.g. database depends on server, config depends on site).

You do not need to edit **main.json** by hand. After changing **main.bicep**, regenerate it with:

```bash
bicep build azure/main.bicep
```

or:

```bash
az bicep build --file azure/main.bicep
```

That overwrites **azure/main.json**. Deploy with either:

- `az deployment group create --template-file azure/main.bicep --parameters azure/main.parameters.json`
- or `az deployment group create --template-file azure/main.json --parameters azure/main.parameters.json`

---

## Summary: What the IaC creates

- One **SQL Server** + one **database** + one **firewall rule** (Allow Azure services).
- One **App Service Plan** + one **Web App** (ASP.NET Core), with connection string **BethanysPieShopDbContextConnection** and app settings **OrderStatusQueueConnection** and **APPLICATIONINSIGHTS_CONNECTION_STRING**.
- One **Application Insights** instance.
- One **Storage Account** (no blob container): used for the Functions runtime and the **order-status-changed** queue.
- One **Function App** (consumption plan) with **AzureWebJobsStorage**, SendGrid settings, and **APPLICATIONINSIGHTS_CONNECTION_STRING**.

Pie images are not stored in this storage account; they are URL-based (see [azure-storage-and-queue-devops.md](azure-storage-and-queue-devops.md)).
