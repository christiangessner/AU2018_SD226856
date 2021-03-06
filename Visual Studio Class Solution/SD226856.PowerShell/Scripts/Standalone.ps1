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

#region Functions
function GetReportColumnType([string]$typeName) {
	switch ($typeName) {
        "String" { return [System.String] }
        "Numeric" { return [System.Double] }
        "Bool" { return [System.Byte] }
        "DateTime" { return [System.DateTime] }
        "Image" { return [System.String] }
        Default { throw ("Type '$typeName' cannot be assigned to a .NET type") }
    }
}

function ReplaceInvalidColumnNameChars([string]$columnName) {
    $pattern = "[^A-Za-z0-9]"
    return [System.Text.RegularExpressions.Regex]::Replace($columnName, $pattern, "_")
}

function GetReportDataSet([Autodesk.Connectivity.WebServices.File[]]$files, [System.String[]]$sysNames) {
    $table = New-Object System.Data.DataTable -ArgumentList @("AutodeskVault_ReportDataSource")
    $table.BeginInit()
    
    $propDefInfos = $vault.PropertyService.GetPropertyDefinitionInfosByEntityClassId("FILE", @())
    $propDefs = @($propDefInfos | Select-Object -ExpandProperty PropDef)   
    $propDefIds = @()

    $propDefs | ForEach-Object {
		if ($sysNames -icontains $_.SysName) { 
			$propDefIds += $_.Id
	        $type = GetReportColumnType $_.Typ

	        $column = New-Object System.Data.DataColumn -ArgumentList @(($_.SysName), $type)
	        $column.Caption = (ReplaceInvalidColumnNameChars $_.DispName)
	        $column.AllowDBNull = $true
	        $table.Columns.Add($column)

#	        if ($type -eq [System.DateTime]) {
#	            $column1 = New-Object System.Data.DataColumn -ArgumentList @(($_.SysName + "!dateonly"), $type)
#	            $column1.Caption = (ReplaceInvalidColumnNameChars ($_.DispName + " (Date Only)"))
#	            $column1.AllowDBNull = $true
#	            $table.Columns.Add($column1)
#
#	            $column2 = New-Object System.Data.DataColumn -ArgumentList @(($_.SysName + "!timeonly"), $type)
#	            $column2.Caption = (ReplaceInvalidColumnNameChars ($_.DispName + " (Time Only)"))
#	            $column2.AllowDBNull = $true
#	            $table.Columns.Add($column2)
#	        }
		}
    }

    $colEntityType = New-Object System.Data.DataColumn -ArgumentList @("EntityType", [System.String])
    $colEntityType.Caption = "Entity_Type"
    $colEntityType.DefaultValue = "File"
    $table.Columns.Add($colEntityType)
    
	$colEntityTypeId = New-Object System.Data.DataColumn -ArgumentList @("EntityTypeID", [System.String])
    $colEntityTypeId.Caption = "Entity_Type_ID"
    $colEntityTypeId.DefaultValue = "FILE"
	$table.Columns.Add($colEntityTypeId)

    $fileIds = @($files | Select-Object -ExpandProperty Id)
    $propInsts = $vault.PropertyService.GetProperties("FILE", $fileIds, $propDefIds)
    
    $table.EndInit()
	
	$table.BeginLoadData()
    $files | ForEach-Object {
        $file = $_
        $row = $table.NewRow()
        
        $propInsts | Where-Object { $_.EntityId -eq $file.Id } | ForEach-Object {
            if ($_.Val) {
                $propDefId = $_.PropDefId
                $propDef = $propDefs | Where-Object { $_.Id -eq $propDefId }
                if ($propDef) {
                    if ($propDef.Typ -eq "Image") {
                        $val = [System.Convert]::ToBase64String($_.Val)
                    } else {
                        $val = $_.Val
                    }
                    $row."$($propDef.SysName)" = $val
                }
            }
        }
        $table.Rows.Add($row)
    }
	$table.EndLoadData()
	$table.AcceptChanges()
	
    return ,$table
}

function GetReportParameters([System.String[]]$paramNames) {
    $parameterList = New-Object System.Collections.Generic.List[Microsoft.Reporting.WinForms.ReportParameter]
    $parameters = @{
        Vault_FolderName = "Designs"
        Vault_UserName = $vault.WebServiceCredentials.UserName
        Vault_SearchRoot = "$/"
        Vault_SearchConditions = "Auto Search all files"
        ReportTitle = "my report"
		FileDetailReport_Title = "my report"
    }

    foreach($parameter in $parameters.GetEnumerator()) {
		if ($paramNames -contains $parameter.Key) {
	        $param = New-Object Microsoft.Reporting.WinForms.ReportParameter -ArgumentList @($parameter.Key, $parameter.Value)
	        $parameterList.Add($param)
		}
    }
    
    return ,$parameterList
}
#endregion

$reportFileLocation = "C:\Program Files\Autodesk\Vault Professional 2019\Explorer\Report Templates\File Detail.rdlc"

$sysNames = @()
[xml]$reportFileXmlDocument = Get-Content -Path $reportFileLocation
$dataSets = $reportFileXmlDocument.Report.DataSets.ChildNodes | Where-Object {$_.Name -eq "AutodeskVault_ReportDataSource"} 
$dataSets.Fields.ChildNodes | ForEach-Object {
    $sysNames += $_.DataField
}

$folder = $vault.DocumentService.GetFolderByPath("$/Designs")
$files = $vault.DocumentService.GetLatestFilesByFolderId($folder.Id, $false)

#region XAML
[xml]$global:xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:wf="clr-namespace:Microsoft.Reporting.WinForms;assembly=Microsoft.ReportViewer.WinForms"
        Title="Custom Reporting" Height="550" Width="750" WindowStartupLocation="CenterOwner">
        <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Auto">
        <Grid>
			<WindowsFormsHost>
				<wf:ReportViewer x:Name="ReportViewer" />
			</WindowsFormsHost>
        </Grid>
    </ScrollViewer>
</Window>
"@
#endregion

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase, WindowsFormsIntegration

$reader = (New-Object System.Xml.XmlNodeReader $xaml)
$window = [Windows.Markup.XamlReader]::Load($reader)

$reportViewer = New-Object Microsoft.Reporting.WinForms.ReportViewer
$reportViewer = $window.FindName("ReportViewer")
$reportViewer.LocalReport.DataSources.Clear()
$reportViewer.LocalReport.ReportPath = [string]::Empty
$reportViewer.LocalReport.EnableExternalImages = $false
$reportViewer.LocalReport.EnableHyperlinks = $true

$table = GetReportDataSet $files $sysNames
if ($table.Rows.Count -gt 0) {
    $xmlDocument = New-Object System.Xml.XmlDocument
    $xmlDocument.Load($reportFileLocation)
    
    $stringReader = New-Object System.IO.StringReader -ArgumentList @($xmlDocument.OuterXml)
    $reportViewer.LocalReport.LoadReportDefinition($stringReader)
    $stringReader.Close()
    $stringReader.Dispose()

    $paramNames = $reportViewer.LocalReport.GetParameters() | Select-Object { $_.Name }
    $parameterList = GetReportParameters $paramNames
    $reportViewer.LocalReport.SetParameters($parameterList)

    $reportDataSource = New-Object -TypeName Microsoft.Reporting.WinForms.ReportDataSource -ArgumentList @($table.TableName, [System.Data.DataTable]$table)
    $reportViewer.LocalReport.DataSources.Add($reportDataSource)
	
    $reportViewer.RefreshReport()
    $reportViewer.ZoomMode = [Microsoft.Reporting.WinForms.ZoomMode]::PageWidth
}

$window.ShowDialog()