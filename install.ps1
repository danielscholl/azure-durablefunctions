<#
.SYNOPSIS
  Install the Infrastructure As Code
.DESCRIPTION
  This Script will install all the infrastructure needed for the solution.

  1. Resource Group


.EXAMPLE
  .\install.ps1 -Prefix {unique}
  Version History
  v1.0   - Initial Release
#>
#Requires -Version 5.1
#Requires -Module @{ModuleName='AzureRM.Resources'; ModuleVersion='5.0'}

Param(
  [string]$Subscription = $env:AZURE_SUBSCRIPTION,
  [string]$Prefix,
  [string]$Location = "southcentralus"
)

if ( !$Subscription) { throw "Subscription Required" }
if ( !$Prefix) { throw "Prefix Required" } 
else { $ResourceGroupName = $Prefix + "-durablefunctions" }

function Write-Color([String[]]$Text, [ConsoleColor[]]$Color = "White", [int]$StartTab = 0, [int] $LinesBefore = 0, [int] $LinesAfter = 0, [string] $LogFile = "", $TimeFormat = "yyyy-MM-dd HH:mm:ss") {
  # version 0.2
  # - added logging to file
  # version 0.1
  # - first draft
  #
  # Notes:
  # - TimeFormat https://msdn.microsoft.com/en-us/library/8kb3ddd4.aspx

  $DefaultColor = $Color[0]
  if ($LinesBefore -ne 0) {  for ($i = 0; $i -lt $LinesBefore; $i++) { Write-Host "`n" -NoNewline } } # Add empty line before
  if ($StartTab -ne 0) {  for ($i = 0; $i -lt $StartTab; $i++) { Write-Host "`t" -NoNewLine } }  # Add TABS before text
  if ($Color.Count -ge $Text.Count) {
    for ($i = 0; $i -lt $Text.Length; $i++) { Write-Host $Text[$i] -ForegroundColor $Color[$i] -NoNewLine }
  }
  else {
    for ($i = 0; $i -lt $Color.Length ; $i++) { Write-Host $Text[$i] -ForegroundColor $Color[$i] -NoNewLine }
    for ($i = $Color.Length; $i -lt $Text.Length; $i++) { Write-Host $Text[$i] -ForegroundColor $DefaultColor -NoNewLine }
  }
  Write-Host
  if ($LinesAfter -ne 0) {  for ($i = 0; $i -lt $LinesAfter; $i++) { Write-Host "`n" } }  # Add empty line after
  if ($LogFile -ne "") {
    $TextToFile = ""
    for ($i = 0; $i -lt $Text.Length; $i++) {
      $TextToFile += $Text[$i]
    }
    Write-Output "[$([datetime]::Now.ToString($TimeFormat))]$TextToFile" | Out-File $LogFile -Encoding unicode -Append
  }
}
function Get-ScriptDirectory {
  $Invocation = (Get-Variable MyInvocation -Scope 1).Value
  Split-Path $Invocation.MyCommand.Path
}
function LoginAzure() {
  Write-Color -Text "Logging in and setting subscription..." -Color Green
  if ([string]::IsNullOrEmpty($(Get-AzureRmContext).Account)) {
    if ($env:AZURE_TENANT) {
      Login-AzureRmAccount -TenantId $env:AZURE_TENANT
    }
    else {
      Login-AzureRmAccount
    }
  }
  Set-AzureRmContext -SubscriptionId ${Subscription} | Out-null

}
function CreateResourceGroup([string]$ResourceGroupName, [string]$Location) {
  # Required Argument $1 = RESOURCE_GROUP
  # Required Argument $2 = LOCATION

  Get-AzureRmResourceGroup -Name $ResourceGroupName -ev notPresent -ea 0 | Out-null

  if ($notPresent) {

    Write-Host "Creating Resource Group $ResourceGroupName..." -ForegroundColor Yellow
    New-AzureRmResourceGroup -Name $ResourceGroupName -Location $Location
  }
  else {
    Write-Color -Text "Resource Group ", "$ResourceGroupName ", "already exists." -Color Green, Red, Green
  }
}



###############################

## Azure Intialize           ##

###############################

$BASE_DIR = Get-ScriptDirectory
$DEPLOYMENT = Split-Path $BASE_DIR -Leaf
LoginAzure
CreateResourceGroup $ResourceGroupName $Location


Write-Color -Text "Registering Provider..." -Color Yellow
Register-AzureRmResourceProvider -ProviderNamespace Microsoft.Insights
Register-AzureRmResourceProvider -ProviderNamespace Microsoft.Storage

##############################
## Deploy Template          ##
##############################

Write-Color -Text "`r`n---------------------------------------------------- "-Color Yellow
Write-Color -Text "Deploying ", "$DEPLOYMENT ", "template..." -Color Green, Red, Green
Write-Color -Text "---------------------------------------------------- "-Color Yellow

New-AzureRmResourceGroupDeployment -Name $DEPLOYMENT `
  -TemplateFile $BASE_DIR\azuredeploy.json `
  -TemplateParameterFile $BASE_DIR\azuredeploy.parameters.json `
  -Prefix $Prefix `
  -ResourceGroupName $ResourceGroupName

exit

# check we're logged in with the right account first!
az account show --query name -o tsv

$resourceGroup = "dfvp-test2"
$location = "southcentralus"
$appName = "dfvptest2"

# Create resource group
az group create -n $resourceGroup -l $location

# Deploy the template
# creates: app service plan (consumption), function app, storage account, app insights
az group deployment create -g $resourceGroup `
        --template-file deploy.json `
        --parameters "appName=$appName"

# --parameters @MySite.parameters.json \


#az functionapp config appsettings set IntroLocation=$introLocation


# to build (n.b. don't know why RunCodeAnalysis has got turned on - can't work out how to disable)
. "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" /p:Configuration=Release /p:RunCodeAnalysis=False

# create a zip
$publishFolder = "$(pwd)\DurableFunctionVideoProcessor\bin\Release\net461"
$destination = "$(pwd)\publish.zip"
If (Test-Path $destination){ Remove-Item $destination }
Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($publishFolder, $destination)

az functionapp deployment source config-zip `
    -n $appName -g $resourceGroup --src $destination