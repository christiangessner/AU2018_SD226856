Add-Type -Path ("C:\Program Files (x86)\Autodesk\Autodesk Vault 2019 SDK\bin\x64\Autodesk.Connectivity.WebServices.dll")
$vaultServer = "VM-2019"
$vaultName = "SD226856"
$vaultUser = "Administrator"
$vaultPassword = ""

$identities = New-Object Autodesk.Connectivity.WebServices.ServerIdentities
$identities.DataServer = $vaultServer
$identities.FileServer = $vaultServer
$credentials = New-Object Autodesk.Connectivity.WebServicesTools.UserPasswordCredentials($identities, $vaultName, $vaultUser, $vaultPassword, $false)
$vault = New-Object Autodesk.Connectivity.WebServicesTools.WebServiceManager ($credentials)

$folder = $vault.DocumentService.GetFolderByPath("$/Designs/Jet Engine Model")
$files = $vault.DocumentService.GetLatestFilesByFolderId($folder.Id, $false)
$file = $files | Where-Object { $_.Name -eq "001204.iam" }
$storage = New-Object "System.Collections.Generic.Dictionary[[long],[int]]"

# function doesn't handle component patterns!
function GetPositions([System.Collections.Generic.Dictionary[[long],[int]]]$storage, [Autodesk.Connectivity.WebServices.BOM]$bom, [long]$parId, [int]$multiplicator) {
    
    $bom.InstArray | Where-Object { $_.ParId -eq $parId } | ForEach-Object { 
        $cldId = $_.CldId
        $quant = $_.Quant * $multiplicator
        $comp = $bom.CompArray | Where-Object { $_.Id -eq $cldId }
        $hasChildren = $bom.InstArray | Where-Object { $_.ParId -eq $cldId }
        $childFileId = $comp.XRefId
        if (-not $hasChildren) {
            if ($storage.ContainsKey($childFileId)) {
                $storage[$childFileId] += $quant
            } else {
                $storage.Add($childFileId, $quant)
            }
        }
        GetPositions $storage $bom $cldId $quant
    }
}

$bom = $vault.DocumentService.GetBOMByFileId($file.Id)
GetPositions $storage $bom $bom.SchmArray[0].RootCompId 1

$files = $vault.DocumentService.GetFilesByIds($storage.Keys)

$propDefs = $vault.PropertyService.GetPropertyDefinitionsByEntityClassId('FILE')
$propDef = $propDefs | Where-Object {$_.DispName -eq 'Part Number'}
$propInsts = $vault.PropertyService.GetProperties('FILE', $storage.Keys, @($propDef.Id))

$storage.GetEnumerator() | ForEach-Object {
    $id = $_.Key
    $quant = $_.Value;
    $file = $files | Where-Object { $_.Id -eq  $id }
    $propInst = $propInsts | Where-Object { $_.PropDefId -eq $propDef.Id -and $_.EntityId -eq $file.Id }
    $propInst.Val + ": " + $_.Value # | Out-File "C:\temp\bom1.txt" -Append
}