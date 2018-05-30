# DurableFunctions

This is a sample for writing durable functions  and enabling docker support.

__Requirements:__

- [.Net Core](https://www.microsoft.com/net/download/windows)  (>= 2.1.104)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (>= 2.0.32)
- [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) (>= 2.0)
- [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) (>= 5.3)

## Installation
### Clone the repo

```bash
git clone https://github.com/danielscholl/docker-swarm-azure.git durable-functions
```

### Create a local.settings.json file

```javascript
{
    "IsEncrypted": false,
    "Values": {
      "AzureWebJobsStorage": "UseDevelopmentStorage=true",
      "AzureWebJobsDashboard": "UseDevelopmentStorage=true",
      "ActivityCodes": "1,2,3,4",
      "Host":  "http://localhost:7071"
    }
}
```

### Run the Function App

Open the Solution and run in Debug Mode

```powershell
$Event=10
$Result = curl http://localhost:7071/api/Workflow/Start?eventId=20 | Select-Object -Expand Content | ConvertFrom-Json

# Monitor the output logs for the Approve/Reject URLs
# Approve
curl http://localhost:7071/api/Approval/{GUID}?result=APPROVED

# Reject
curl http://localhost:7071/api/Approval/{GUID}?result=REJECT

# Get the status
curl $Result.statusQueryGetUri |Select-Object -Expand Content | ConvertFrom-Json | ConvertTo-Json
```