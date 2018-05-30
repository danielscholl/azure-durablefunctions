# DurableFunctions

This is a sample for writing durable functions  and enabling docker support.

__Requirements:__

- [.Net Core](https://www.microsoft.com/net/download/windows)  (>= 2.1.104)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (>= 2.0.32)
- [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) (>= 2.0)
- [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) (>= 5.3)


## Deploy the Solution

_Note: The solution deployment has github syncing enabled._

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fdanielscholl%2Fazure-durablefunctions%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

### Manually Deploy the Solution
```powershell
$Subscription = "<your_subscription>"
$Prefix = "<unique_prefix>"
.\install.ps1 -Prefix $Prefix -Subscription $Subscription
```


## Develop the Solution
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
### Compile the Code

Open the DurableFunctions.sln and Build the code.


### Locally Test the Code

Open the Solution and run in Debug Mode

```powershell
## Set an Event
$Event=10

## Trigger the Workflow
$Result = curl http://localhost:7071/api/Workflow/Start?eventId=20 | Select-Object -Expand Content | ConvertFrom-Json

## Monitor the output logs for the Approve/Reject URLs

## Approve or Reject the Activity
curl http://localhost:7071/api/Approval/{GUID}?result=APPROVED
curl http://localhost:7071/api/Approval/{GUID}?result=REJECT

## Get the status
curl $Result.statusQueryGetUri |Select-Object -Expand Content | ConvertFrom-Json | ConvertTo-Json
```