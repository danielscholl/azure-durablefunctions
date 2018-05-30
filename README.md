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

### Test the Solution

> _Note: To Access Streaming Logs you have to enable Diagnostics Application File System Logging_

Set the WebHost
`$WEBHOST = "my5qmjppnvdlaqg-func.azurewebsites.net"`

Ping Test the API



```powershell
curl https://$WEBHOST/api/ping

#Result
StatusCode        : 200
StatusDescription : OK
Content           : pong
RawContent        : HTTP/1.1 200 OK
                    Content-Length: 4
                    Content-Type: text/plain; charset=utf-8
                    Date: Wed, 30 May 2018 19:51:58 GMT
                    Server: Kestrel
                    X-Powered-By: ASP.NET

                    pong
Forms             : {}
Headers           : {[Content-Length, 4], [Content-Type, text/plain; charset=utf-8], [Date, Wed, 30 May 2018 19:51:58
                    GMT], [Server, Kestrel]...}
Images            : {}
InputFields       : {}
Links             : {}
ParsedHtml        : mshtml.HTMLDocumentClass
RawContentLength  : 4
```

```

## Trigger the Workflow
$Result = curl http://$WEBHOST/api/Workflow/Start?eventId=20 | Select-Object -Expand Content | ConvertFrom-Json

## Approve or Reject the Activity
curl http://$WEBHOST/api/Approval/{GUID}?result=APPROVED
curl http://$WEBHOST/api/Approval/{GUID}?result=REJECT

## Get the status
curl $Result.statusQueryGetUri |Select-Object -Expand Content | ConvertFrom-Json | ConvertTo-Json

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
$Result = curl http://localhost:7071/api/Workflow/Start?eventId=20 | Select-Object -Expand Content | ConvertFrom-Json

## Monitor the output logs for the Approve/Reject URLs

## Approve or Reject the Activity
curl http://localhost:7071/api/Approval/{GUID}?result=APPROVED
curl http://localhost:7071/api/Approval/{GUID}?result=REJECT

## Get the status
curl $Result.statusQueryGetUri |Select-Object -Expand Content | ConvertFrom-Json | ConvertTo-Json
```