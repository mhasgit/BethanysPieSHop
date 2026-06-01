# Azure Storage and Queue – DevOps Perspective

This document explains how **blob storage (images)** and **Storage Queue (order status email)** are used in Bethany's Pie Shop, and what a DevOps engineer should understand for operations and interviews.

---

## 1. Blob storage (images)

### How it’s used today

- The application **does not upload or manage blobs**. It does not use the `Azure.Storage.Blobs` SDK.
- Pie images are stored as **URLs** in the database. The `Pie` entity has `ImageUrl` and `ImageThumbnailUrl`; these are plain strings (e.g. `https://gillcleerenpluralsight.blob.core.windows.net/files/...`).
- Seed data in **`BethanysPieShop/Models/DbInitializer.cs`** populates these URLs; they point to an external blob host. The app only renders `<img src="...">` with those URLs.
- The **Azure Bicep/ARM in `azure/`** creates one Storage Account for the Function runtime and the queue. It does **not** create any blob container. There is no “pie images” container in this IaC.

### What DevOps should know

1. **If you host your own images:** Create a blob container (in the existing storage account or a separate one), configure public access or SAS/CORS as needed, upload images, and either:
   - Update seed data / database to use your blob URLs, or  
   - Add a future feature that uploads images and saves blob URLs.  
   The app will work with any HTTPS image URL.

2. **No blob SDK in the web app** – Only **Azure.Storage.Queues** is used. Blob is “reference only” via URLs.

3. **For interviews:** Understand blob containers, access tiers, lifecycle policies, and that this app uses blob only as a **URL source** (read-only from the app’s perspective). You may be asked about adding a container via IaC, securing access (private vs public, SAS), and CORS if the front end talks to blob directly.

---

## 2. Storage Queue (order status notification by email)

### End-to-end flow

1. **API:** An order status is updated via **PATCH** `api/orders/{id}/status` (e.g. body `{ "status": "Shipped" }`).
2. **Web app** – **`BethanysPieShop/Controllers/Api/OrdersApiController.cs`**:  
   Updates the order in the database, then calls **`IOrderStatusQueueService.EnqueueOrderStatusChangedAsync(...)`**.
3. **Queue service** – **`BethanysPieShop/Services/OrderStatusQueueService.cs`**:  
   Serializes a message to JSON and sends it to the Azure Storage Queue **`order-status-changed`** (same queue name as in Bicep and in the Function).
4. **Azure Function** – **`BethanysPieShop.OrderStatusFunction/OrderStatusEmailFunction.cs`**:  
   Triggered by **`[QueueTrigger("order-status-changed", Connection = "AzureWebJobsStorage")]`**. It:
   - Deserializes the JSON to `OrderStatusChangedMessage`
   - Sends an email via **SendGrid** (subject/body with order ID and new status)

So: **Web app (producer) → Storage Queue → Function (consumer) → SendGrid (email).**

### Message contract

The same shape is used when sending (web) and receiving (function). JSON properties:

- **OrderId** (int)
- **Email** (string)
- **CustomerName** (string)
- **NewStatus** (string)

Defined in the web app as **`BethanysPieShop/Services/OrderStatusChangedMessage.cs`** and in the function as **`BethanysPieShop.OrderStatusFunction/OrderStatusChangedMessage.cs`**. They must stay in sync (same property names) for JSON serialization.

### Configuration

| Component   | Setting                         | Purpose |
|------------|----------------------------------|---------|
| **Web app** | `OrderStatusQueueConnection`     | Storage account connection string used to send messages to `order-status-changed`. |
| **Function** | `AzureWebJobsStorage`           | Same storage account; used by the Functions runtime and by the QueueTrigger to read from `order-status-changed`. |

In the Bicep deployment, both point to the **same** Storage Account. The queue **`order-status-changed`** is created by the template.

### DevOps takeaways

1. **One Storage Account** is used for:
   - Functions runtime (AzureWebJobsStorage – internal queues/blobs used by the host)
   - Application queue `order-status-changed`  
   So the Function App only needs one connection string; the trigger binding uses the same account and queue name.

2. **Queue creation** – Bicep creates the queue. The web app’s queue service also calls `CreateIfNotExistsAsync` so it works even if the queue was missing (e.g. in a different environment).

3. **At-least-once delivery** – Storage Queue can deliver a message more than once (e.g. on timeout or failure). For production, consider idempotent handling (e.g. “already sent email for this order+status”) or dead-letter handling.

4. **SendGrid** – The function **requires** SendGrid to send email. `SendGridApiKey` must be set in the Function App (portal, Key Vault reference, or deployment parameters). Without it, the function fails when processing a message.

5. **For interviews:** Be ready to explain:
   - **Scaling:** Consumption plan (Y1) scales the function on queue depth.
   - **Monitoring:** Queue depth, function execution count/failures in Application Insights, and alerting on failures or backlog.
   - **Security:** Store connection strings and SendGrid key in App Settings or Key Vault; never in code or public config.
   - **Dead letter / poison messages:** How you’d handle repeated failures (e.g. move to a poison queue or alert and fix the payload/SendGrid config).

---

## Summary

| Topic        | Usage in this project | DevOps focus |
|-------------|------------------------|--------------|
| **Blob**    | Images are URL-only; no blob SDK or container in IaC. | Optional: add container + URLs; know access/CORS/lifecycle. |
| **Queue**   | Single queue `order-status-changed`: web app produces, function consumes and sends email via SendGrid. | Same storage for Functions + queue; config; scaling; monitoring; secrets. |

For infrastructure details, see [azure-iac-reference.md](azure-iac-reference.md). For deployment and app settings, see [deployment-guide.md](deployment-guide.md).
