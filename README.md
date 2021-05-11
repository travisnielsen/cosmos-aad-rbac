# Cosmos DB SQL API: Data Plane RBAC for Azure Active Directory Credentials

This is a sample application for testing Role Based Access Contro (RBAC) for data stored in Cosmos DB (SQL API). This demonstration assumes the following:

1. Administrative control over a Cosmos DB account
2. PowerShell is installed
3. [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) version 2.23.0 or higher installed

## Configure Application Settings

Download the [latest build of the sample application](https://github.com/travisnielsen/cosmos-aad-rbac/releases).

In the same directory hosting `CosmosRbac.exe`, create a new file called `appsettings.json` and populate it with the following data:

```json
{
    "endPointUrl": "https://trniel.documents.azure.com",
    "database": "test",
    "container": "demo",
    "AZURE_CLIENT_ID": "",
    "AZURE_TENANT_ID": "",
    "AZURE_CLIENT_SECRET": ""
}
```

Update the values to match your environment. If you plan to use a Service Principal in your testing, update the values of `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_CLIENT_SECRET`. Otherwise, leave these settings blank. The application will then use your browser for interactive authentication.

## Configure AAD RBAC

Copy the two .json role definition files located in the `roles` folder of this repo to a folder on your workstation. Open a PowerShell terminal and navigate to that directory. Run the following commands to create role definitions for your Cosmos DB account. The resulting IDs are stored in PowerShell variables.

```powershell
$resourceGroupName='<yourResourceGroup>'
$accountName='<yourCosmosAccount>'

az login

# Make sure Azure CLI is pointing to the subscription you want to work in
az account set --subscription "<your-subscription-name>"

az cosmosdb sql role definition create --account-name $accountName --resource-group $resourceGroupName --body @role-definition-ro.json
$roleDefIdReadOnly = az cosmosdb sql role definition list --account-name $accountName --resource-group $resourceGroupName --query "[?roleName=='MyReadOnlyRole'].name" --out tsv

az cosmosdb sql role definition create --account-name $accountName --resource-group $resourceGroupName --body @role-definition-rw.json
$roleDefIdReadWrite = az cosmosdb sql role definition list --account-name $accountName --resource-group $resourceGroupName --query "[?roleName=='MyReadWriteRole'].name" --out tsv
```

Next, use *either* of the following two options to get the object ID of the account you will be granting permissions to:

```powershell
# Interactive User account
$principalId = az ad user show --id <account_name@domain.com> --query objectId --out tsv

# Service Principal
$spDisplayName = "<yourServicePrincipalName>"
$principalId = az ad sp list --display-name $spDisplayName --query "[?appDisplayName=='$spDisplayName'].objectId" -o tsv
```

Finally, assign the `$principalId` to the `MyReadWriteRole` via the following command:

```powershell
az cosmosdb sql role assignment create --account-name $accountName --resource-group $resourceGroupName --scope "/" --principal-id $principalId --role-definition-id $roleDefIdReadWrite
$assignmentRW = az cosmosdb sql role assignment list --account-name $accountName --resource-group $resourceGroupName --query "[?principalId=='$principalId'].name" --out tsv
```

## Run Tests

Make sure that `CosmosRbac.exe` and your `appsettings.json` file are in the same directory and execute the following command to insert a record. If you assigned the `$principalId` to a user account, you will be prompted to authenticate via your browser.

### Insert Record (read/write role assigned)

```powershell
.\CosmosRbac.exe create '"{ \"firstName\": \"Joe\", \"lastName\": \"Smith\" }"'
```

You should see a JSON response similar to this:

```bash
{"id":"4c8854db-ab50-4f1f-a7f4-6879a6c9ccd1","firstName":"Joe","lastName":"Smith"}
```

If not, make sure your `appsettings.json` file is configured correctly. If your `$principalId` is assigned to an AAD Service Principal, make sure the values are correctly set in the file.

### Insert Record (no role)

Remove the role assignment and re-try creating a record.

```powershell
az cosmosdb sql role assignment delete --account-name $accountName --resource-group $resourceGroupName --role-assignment-id $assignmentRW --yes

.\CosmosRbac.exe create '"{ \"firstName\": \"Joe\", \"lastName\": \"Smith\" }"'
```

You should see a response similar to the following:

```bash
Response status code does not indicate success: Forbidden (403); Substatus: 5301; ActivityId: eb482ff1-adbc-47f5-9d34-6d18e71bd9e0; Reason: (Request blocked by Auth trniel : Request is blocked because principal [a2461c6f-d982-481a-ca03-1ad4798a90ff] does not have required RBAC permissions to perform action [Microsoft.DocumentDB/databaseAccounts/readMetadata] on resource [/].
```

### Insert Record (read role)

Now assign your `$principalId` the read role and attempt to insert a record:

```powershell
az cosmosdb sql role assignment create --account-name $accountName --resource-group $resourceGroupName --scope "/" --principal-id $principalId --role-definition-id $roleDefIdReadOnly

.\CosmosRbac.exe create '"{ \"firstName\": \"Joe\", \"lastName\": \"Smith\" }"'
```

You should see the same error as in the previous test.

### Read Records (read role)

Finally, read the records previous inserted into Cosmos DB. This should work 

```powershell
.\CosmosRbac.exe create
```

You should see at least one record returned. This is becuase your `$principalId` was assigned the read role.

```bash
{"id":"4c8854db-ab50-4f1f-a7f4-6879a6c9ccd1","firstName":"Joe","lastName":"Smith"}
```
