$accountName="trniel"
$resourceGroupName="cosmos-demo"
$subscrptionId = "cecea5c9-0754-4a7f-b5a9-46ae68dcafd3"

Connect-AzAccount
# Get-AzContext -ListAvailable
Set-AzContext -Subscription $subscrptionId

$assignmentList = New-Object -TypeName "System.Collections.ArrayList"

$roleDefinitions = Get-AzCosmosDBSqlRoleDefinition -AccountName $accountName -ResourceGroupName $resourceGroupName
$assignments = Get-AzCosmosDBSqlRoleAssignment -AccountName $accountName -ResourceGroupName $resourceGroupName


# function to resolve Object ID to friendly names
Function Get-PrincipalName {
    Param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $objectId
    )

    # try user account
    $name = Get-AzADUser -ObjectId $objectId

    if ($name) {
        return $name
    }
    else {
        # Try with service principal - this includes Managed Identities
        $name = (Get-AzADServicePrincipal -ObjectId $objectId).DisplayName
    }

    if ($name) {
        return $name
    }
    else {
        # try with group
        $name = (Get-AzADApplication -ObjectId $objectId).DisplayName
    }

    return $name
}


# get list of all Role Definitions that have assignments
foreach ($i in $assignments) {
    $match = $roleDefinitions | Where-Object {$_.Id -eq $i.RoleDefinitionId }
    $principalName = Get-PrincipalName $i.PrincipalId
    
    $assignmentItem = [PSCustomObject]@{
        RoleDefId = $match.Id
        RoleDefName = $match.RoleName
        PrincipalId = $i.PrincipalId
        PrincipalName = $principalName 
    }

    $assignmentList.Add($assignmentItem)
}

$assignmentList