# DurableFunctions

This is a sample for writing durable functions  and enabling docker support.

__Requirements:__

- [.Net Core](https://www.microsoft.com/net/download/windows)  (>= 2.1.104)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (>= 2.0.32)
- [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) (>= 2.0)
- [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) (>= 5.3)


## Deploy the Solution

### Automatically Deploy the Solution
> _Github Deployment Sync Enabled._

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fdanielscholl%2Fazure-durablefunctions%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

### Manually Deploy the Solution
```powershell
$Subscription = "<your_subscription>"
$Prefix = "<unique_prefix>"
.\install.ps1 -Prefix $Prefix -Subscription $Subscription
```

### Test the Solution

> _Note: To Access Streaming Logs you have to enable Diagnostics Application File System Logging_

1. Simple Function Ping Test

    ```bash
    # Commands for Bash with HTTPie
    RESOURCE_GROUP=<your_resource_group>
    WEBHOST="https://"$(az functionapp list --resource-group ${RESOURCE_GROUP} --query [].defaultHostName -otsv)
    http get $WEBHOST/api/ping

    # Result
    HTTP/1.1 200 OK
    Content-Encoding: gzip
    Content-Type: text/plain; charset=utf-8
    Date: Thu, 31 May 2018 14:30:49 GMT
    Server: Kestrel
    Transfer-Encoding: chunked
    Vary: Accept-Encoding
    X-Powered-By: ASP.NET

    pong
    ```

    ```powershell
    # Commands for Powershell
    $WEBHOST="https://<your_function_app>.azurewebsites.net/"
    Invoke-RestMethod -Method Get -Uri $WEBHOST/api/ping

    # Result
    pong
    ```

    In Log Analytics query for logs

    ```sql
    // Ping AIQL Logging Query
    traces
    | where operation_Name == "ping" 
    | where severityLevel == 2
    | sort by timestamp asc 
    ```

1.  Durable Function Periodic (Monitoring)
    ```bash
    # Commands for Bash with HTTPie
    RESOURCE_GROUP=<your_resource_group>
    WEBHOST="https://"$(az functionapp list --resource-group ${RESOURCE_GROUP} --query [].defaultHostName -otsv)
    
    WORKFLOW=$(http get $WEBHOST/api/StartPeriodic)
    STATUS=$(echo $WORKFLOW |jq -r '.statusQueryGetUri')
    TERMINATE=$(echo $WORKFLOW |jq -r '.terminatePostUri')

    http get $STATUS
    
    # Result
    HTTP/1.1 202 Accepted
    Content-Length: 149
    Content-Type: application/json; charset=utf-8
    Date: Thu, 31 May 2018 15:06:33 GMT
    Location: https://dse42vlpxvu2u4y-func.azurewebsites.net/runtime/webhooks/DurableTaskExtension/instances/31fd33e419744154a34d715843ecd5ca?taskHub=DurableFunctionsHub&connection=Storage&code=uInXDPrmG7q8jp2FYI3cvzXi8kFghLviHacpSUFDkXWW7vsb2aJSYQ==
    Retry-After: 5
    Server: Kestrel
    X-Powered-By: ASP.NET

    {
        "createdTime": "2018-05-31T15:06:14Z",
        "customStatus": null,
        "input": 0,
        "lastUpdatedTime": "2018-05-31T15:06:16Z",
        "output": null,
        "runtimeStatus": "Running"
    }

    http post $TERMINATE

    # Result
    HTTP/1.1 202 Accepted
    Content-Length: 0
    Date: Thu, 31 May 2018 15:07:42 GMT
    Server: Kestrel
    X-Powered-By: ASP.NET
    ```

    ```powershell
    # Trigger the Workflow
    $PERIODIC_RESULT = curl https://$WEBHOST/api/StartPeriodic |`
    Select-Object -Expand Content | `
    ConvertFrom-Json

    # Check the Status
    curl $PERIODIC_RESULT.statusQueryGetUri |`
        Select-Object -Expand Content | `
        ConvertFrom-Json | `
        ConvertTo-Json

    # Terminate the Workflow
    Invoke-RestMethod -Method Post -Uri $PERIODIC_RESULT.terminatePostUri

    ```

    In Log Analytics query for logs

    ```sql
    // Periodic AIQL Logging Query
    traces
    | where operation_Name == "A_PeriodicActivity" 
    | where severityLevel == 2
    | sort by timestamp asc 
    ```

1.  Durable Function Complex Workflow
    ```bash
    # Commands for Bash with HTTPie
    RESOURCE_GROUP=<your_resource_group>
    WEBHOST="https://"$(az functionapp list --resource-group ${RESOURCE_GROUP} --query [].defaultHostName -otsv)
    
    EVENT_ID=10
    WORKFLOW=$(http get $WEBHOST/api/Workflow/Start?eventId=${EVENT_ID})
    STATUS=$(echo $WORKFLOW |jq -r '.statusQueryGetUri')
    TERMINATE=$(echo $WORKFLOW |jq -r '.terminatePostUri')

    http get $STATUS
    ```


    ```powershell
    # Trigger the Workflow
    $RESULT = curl http://$WEBHOST/api/Workflow/Start?eventId=20 | `
        Select-Object -Expand Content | `
        ConvertFrom-Json

    # Check the Status
    curl $Result.statusQueryGetUri | `
        Select-Object -Expand Content | `
        ConvertFrom-Json | `
        ConvertTo-Json

    # AIQL Logging Query
    traces
    | where operation_Name == "A_SendApproval"
    | where severityLevel == 2
    | sort by timestamp asc 

    # Approve or Reject the Activity  (Automatically REJECT in 120 Seconds)
    curl http://$WEBHOST/api/Approval/{GUID}?result=APPROVED
    curl http://$WEBHOST/api/Approval/{GUID}?result=REJECT

    # Check the Status
    curl $Result.statusQueryGetUri | `
        Select-Object -Expand Content | `
        ConvertFrom-Json | `
        ConvertTo-Json
    ```

## Develop the Solution Locally
### Clone the repo

`git clone https://github.com/danielscholl/docker-swarm-azure.git durable-functions`


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


### Test the Code

Open the Solution and run in Debug Mode

```powershell
## Set an Event
$Event=10

## Trigger the Workflow
$Result = curl http://localhost:7071/api/Workflow/Start?eventId=20 | `
    Select-Object -Expand Content | `
    ConvertFrom-Json

## Monitor the output logs for the Approve/Reject URLs

## Approve or Reject the Activity
curl http://localhost:7071/api/Approval/{GUID}?result=APPROVED
curl http://localhost:7071/api/Approval/{GUID}?result=REJECT

## Get the status
curl $Result.statusQueryGetUri | `
    Select-Object -Expand Content | `
    ConvertFrom-Json | `
    ConvertTo-Json
```