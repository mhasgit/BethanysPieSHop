# Bethany's Pie Shop – DevOps Documentation

This folder contains documentation for the DevOps engineer taking over the project. Use it for handover, runbooks, and interview preparation.

| Document | Description |
|----------|-------------|
| [azure-iac-reference.md](azure-iac-reference.md) | Line-by-line explanation of the Azure Bicep/ARM templates and parameters in the `azure/` folder. |
| [setup-and-run.md](setup-and-run.md) | How to set up the repo and run the web app and Azure Function locally (including database and queue). |
| [azure-storage-and-queue-devops.md](azure-storage-and-queue-devops.md) | Blob storage (images) and Storage Queue (order status email): how they are used and what DevOps needs to know. |
| [logging-and-monitoring.md](logging-and-monitoring.md) | Logging in the codebase and Application Insights setup in the cloud. |
| [deployment-guide.md](deployment-guide.md) | Full guide to deploying the solution to Azure (CLI and manual), including database and migrations. |

Start with [setup-and-run.md](setup-and-run.md) for local development, then [azure-iac-reference.md](azure-iac-reference.md) and [deployment-guide.md](deployment-guide.md) for deployment.
