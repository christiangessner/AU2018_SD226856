using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;
using Microsoft.Reporting.WinForms;
using SD226856.RdlcHelper;
using File = Autodesk.Connectivity.WebServices.File;

[assembly: Autodesk.Connectivity.Extensibility.Framework.ExtensionId("fbb7b48d-8ecf-4810-8788-3ce295f3fa96")]
[assembly: Autodesk.Connectivity.Extensibility.Framework.ApiVersion("12.0")]

namespace SD226856.RdlcFileBomExplorerExtension
{
    public class ExplorerExtension : IExplorerExtension
    {
        #region IExplorerExtension Members
        public IEnumerable<CommandSite> CommandSites()
        {
            var cmdBarcodeWindow = new CommandItem(
                "AU.SD226856.RdlcFileBomPartsOnlyTab",
                "Parts Only Report Window...")
            {
                NavigationTypes = new[] { SelectionTypeId.File, SelectionTypeId.FileVersion },
                MultiSelectEnabled = false
            };
            cmdBarcodeWindow.Execute += CreateReportInNewWindow;

            var cmdBarcodeExport = new CommandItem(
                "AU.SD226856.RdlcFileBomPartsOnlySavePdf", 
                "Parts Only Report PDF Export...")
            {
                NavigationTypes = new[] { SelectionTypeId.File, SelectionTypeId.FileVersion },
                MultiSelectEnabled = false
            };
            cmdBarcodeExport.Execute += CreateReportAndExportPdf;

            var fileContextCmdSite = new CommandSite("AU.SD226856.FileContextMenu", "Custom Reporting")
            {
                Location = CommandSiteLocation.FileContextMenu,
                DeployAsPulldownMenu = false
            };
            fileContextCmdSite.AddCommand(cmdBarcodeWindow);
            fileContextCmdSite.AddCommand(cmdBarcodeExport);

            return new List<CommandSite> {fileContextCmdSite};
        }

        public IEnumerable<DetailPaneTab> DetailTabs()
        {
            var detailPaneTab = new DetailPaneTab(
                "AU.SD226856.RdlcFileBomPartsOnlyTab",
                "BOM (Parts Only) Report",
                SelectionTypeId.File,
                typeof(ReportViewerUserControl));
            detailPaneTab.SelectionChanged += SelectionChanged;

            return new List<DetailPaneTab> { detailPaneTab };
        }

        public IEnumerable<CustomEntityHandler> CustomEntityHandlers()
        {
            return null;
        }

        public IEnumerable<string> HiddenCommands()
        {
            return null;
        }

        public void OnStartup(IApplication application)
        {
        }

        public void OnShutdown(IApplication application)
        {
        }

        public void OnLogOn(IApplication application)
        {
        }

        public void OnLogOff(IApplication application)
        {
        }
        #endregion

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Context.SelectedObject == null)
                return;

            var reportViewer = (e.Context.UserControl as ReportViewerUserControl)?.ReportViewer;
            if (reportViewer == null)
                return;

            var connection = e.Context.Application.Connection;

            var wsm = connection.WebServiceManager;
            var file = wsm.DocumentService.GetLatestFileByMasterId(e.Context.SelectedObject.Id);
            var folder = wsm.DocumentService.GetFolderById(file.FolderId);

            var storage = new Dictionary<long, int>();
            var bom = wsm.DocumentService.GetBOMByFileId(file.Id);
            RecursivelyFillBomQuantities(storage, bom, 0, 1);
            var fileIds = storage.Keys.ToArray();
            var quantities = storage.Values.ToArray();

            var extensionPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (extensionPath == null)
                return;

            var rdlcFullFileName = Path.Combine(extensionPath, "FileBomPartsOnly.rdlc");
            using (var reportManager = new ReportManager(reportViewer.LocalReport, rdlcFullFileName))
            {
                var fieldNames = reportManager.Fields.Select(f => f.DataField).ToArray();

                var propDefInfos = wsm.PropertyService.GetPropertyDefinitionInfosByEntityClassId("FILE", null);
                var propDefs = propDefInfos.Select(p => p.PropDef).ToArray();
                var propDefIds = new List<long>();

                #region Create Table and Columns
                var table = new DataTable(reportManager.DatasetName);
                table.BeginInit();

                foreach (var propDef in propDefs)
                {
                    if (fieldNames.Contains(propDef.SysName))
                    {
                        propDefIds.Add(propDef.Id);
                        var column = new DataColumn(propDef.SysName, propDef.Typ.ToDotNetType())
                        {
                            Caption = propDef.DispName.ToDataColumnCaption(),
                            AllowDBNull = true
                        };
                        table.Columns.Add(column);
                    }
                }

                var colEntityType = new DataColumn("EntityType", typeof(string)) { DefaultValue = "File" };
                table.Columns.Add(colEntityType);

                var colEntityTypeId = new DataColumn("EntityTypeID", typeof(string)) { DefaultValue = "FILE" };
                table.Columns.Add(colEntityTypeId);

                var colQuantity = new DataColumn("Quantity", typeof(int));
                table.Columns.Add(colQuantity);

                table.EndInit();
                #endregion

                var propInsts = connection.WebServiceManager.PropertyService.GetProperties(
                    "FILE", fileIds.ToArray(), propDefIds.ToArray());

                #region Create Table Rows
                table.BeginLoadData();
                for (var i = 0; i < fileIds.Length; i++)
                {
                    var row = table.NewRow();
                    row["Quantity"] = quantities[i];

                    var fileId = fileIds[i];
                    foreach (var propInst in propInsts.Where(p => p.EntityId == fileId))
                    {
                        if (propInst.Val != null)
                        {
                            var propDefId = propInst.PropDefId;
                            var propDef = propDefs.SingleOrDefault(p => p.Id == propDefId);
                            if (propDef != null)
                            {
                                var val = propDef.Typ == DataType.Image
                                    ? Convert.ToBase64String((byte[])propInst.Val)
                                    : propInst.Val;
                                row[propDef.SysName] = val;
                            }
                        }
                    }
                    table.Rows.Add(row);
                }
                table.EndLoadData();
                #endregion

                #region Load Data Source
                var reportDataSource = new ReportDataSource(table.TableName, table);
                reportManager.LocalReport.DataSources.Add(reportDataSource);
                #endregion

                #region Create Parameters 
                var reportParameters = new List<ReportParameter>
                {
                    new ReportParameter("Vault_FolderName", folder.Name),
                    new ReportParameter("Vault_UserName", connection.UserName),
                    new ReportParameter("Vault_SearchRoot", folder.FullName),
                    new ReportParameter("Vault_SearchConditions", "Current Selection"),
                    new ReportParameter("ReportTitle", "File BOM - Parts Only")
                };
                reportManager.LocalReport.SetParameters(reportParameters.ToArray());
                #endregion

                reportViewer.ZoomMode = ZoomMode.PageWidth;
                reportViewer.RefreshReport();
            }
        }

        private void CreateReportInNewWindow(object sender, CommandItemEventArgs e)
        {
            var selection = e.Context.CurrentSelectionSet?.FirstOrDefault();
            if (selection == null)
                return;

            var connection = e.Context.Application.Connection;
            var wsm = connection.WebServiceManager;
            File file = null;
            if (selection.TypeId == SelectionTypeId.File)
                file = connection.WebServiceManager.DocumentService.GetLatestFileByMasterId(selection.Id);
            else if (selection.TypeId == SelectionTypeId.FileVersion)
                file = connection.WebServiceManager.DocumentService.GetFileById(selection.Id);
            if (file == null)
                return;
            var folder = wsm.DocumentService.GetFolderById(file.FolderId);

            var storage = new Dictionary<long, int>();
            var bom = wsm.DocumentService.GetBOMByFileId(file.Id);
            RecursivelyFillBomQuantities(storage, bom, 0, 1);
            var fileIds = storage.Keys.ToArray();
            var quantities = storage.Values.ToArray();

            var window = new ReportViewerWindow(connection, folder, fileIds, quantities);
            window.ShowDialog();
        }

        private void CreateReportAndExportPdf(object sender, CommandItemEventArgs e)
        {
            var selection = e.Context.CurrentSelectionSet?.FirstOrDefault();
            if (selection == null)
                return;

            var connection = e.Context.Application.Connection;
            var wsm = connection.WebServiceManager;
            File file = null;
            if (selection.TypeId == SelectionTypeId.File)
                file = connection.WebServiceManager.DocumentService.GetLatestFileByMasterId(selection.Id);
            else if (selection.TypeId == SelectionTypeId.FileVersion)
                file = connection.WebServiceManager.DocumentService.GetFileById(selection.Id);
            if (file == null)
                return;
            var folder = wsm.DocumentService.GetFolderById(file.FolderId);

            var storage = new Dictionary<long, int>();
            var bom = wsm.DocumentService.GetBOMByFileId(file.Id);
            RecursivelyFillBomQuantities(storage, bom, 0, 1);
            var fileIds = storage.Keys.ToArray();
            var quantities = storage.Values.ToArray();

            var extensionPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (extensionPath == null)
                return;

            var rdlcFullFileName = Path.Combine(extensionPath, "FileBomPartsOnlyWithBarcode.rdlc");
            using (var reportManager = new ReportManager(rdlcFullFileName))
            {
                var fieldNames = reportManager.Fields.Select(f => f.DataField).ToArray();

                var propDefInfos = wsm.PropertyService.GetPropertyDefinitionInfosByEntityClassId("FILE", null);
                var propDefs = propDefInfos.Select(p => p.PropDef).ToArray();
                var propDefIds = new List<long>();

                var partNumberPropDef = propDefs.SingleOrDefault(p => p.DispName == "Part Number");

                #region Create Table and Columns
                var table = new DataTable(reportManager.DatasetName);
                table.BeginInit();

                foreach (var propDef in propDefs)
                {
                    if (fieldNames.Contains(propDef.SysName))
                    {
                        propDefIds.Add(propDef.Id);
                        var column = new DataColumn(propDef.SysName, propDef.Typ.ToDotNetType())
                        {
                            Caption = propDef.DispName.ToDataColumnCaption(),
                            AllowDBNull = true
                        };
                        table.Columns.Add(column);
                    }
                }

                var colEntityType = new DataColumn("EntityType", typeof(string)) { DefaultValue = "File" };
                table.Columns.Add(colEntityType);

                var colEntityTypeId = new DataColumn("EntityTypeID", typeof(string)) { DefaultValue = "FILE" };
                table.Columns.Add(colEntityTypeId);

                var colBarcode = new DataColumn("Barcode", typeof(string));
                table.Columns.Add(colBarcode);

                var colQuantity = new DataColumn("Quantity", typeof(int));
                table.Columns.Add(colQuantity);

                table.EndInit();
                #endregion

                var propInsts = connection.WebServiceManager.PropertyService.GetProperties(
                    "FILE", fileIds, propDefIds.ToArray());

                #region Create Table Rows
                table.BeginLoadData();
                for (var i = 0; i < fileIds.Length; i++)
                {
                    var row = table.NewRow();
                    row["Quantity"] = quantities[i];

                    var fileId = fileIds[i];
                    foreach (var propInst in propInsts.Where(p => p.EntityId == fileId))
                    {
                        if (propInst.Val != null)
                        {
                            var propDefId = propInst.PropDefId;
                            var propDef = propDefs.SingleOrDefault(p => p.Id == propDefId);
                            if (propDef != null)
                            {
                                var val = propDef.Typ == DataType.Image
                                    ? Convert.ToBase64String((byte[])propInst.Val)
                                    : propInst.Val;
                                row[propDef.SysName] = val;

                                if (partNumberPropDef != null && partNumberPropDef.Id == propDef.Id)
                                {
                                    var base64String = GetBarcodeAsBase64String(val.ToString());
                                    row["Barcode"] = base64String;
                                }
                            }
                        }
                    }
                    table.Rows.Add(row);
                }
                table.EndLoadData();
                #endregion

                if (table.Rows.Count > 0)
                {
                    #region Load Data Source
                    var reportDataSource = new ReportDataSource(table.TableName, table);
                    reportManager.LocalReport.DataSources.Add(reportDataSource);
                    #endregion

                    #region Create Parameters 
                    var reportParameters = new List<ReportParameter>
                    {
                        new ReportParameter("Vault_FolderName", folder.Name),
                        new ReportParameter("Vault_UserName", connection.UserName),
                        new ReportParameter("Vault_SearchRoot", folder.FullName),
                        new ReportParameter("Vault_SearchConditions", "Current Selection"),
                        new ReportParameter("ReportTitle", "File BOM - Packaging List")
                    };
                    reportManager.LocalReport.SetParameters(reportParameters.ToArray());
                    #endregion

                    var folderBrowser = new FolderBrowserDialog
                    {
                        RootFolder = Environment.SpecialFolder.Desktop,
                        Description = $@"PDF Export location for file '{file.Name}.pdf'",
                        ShowNewFolderButton = true
                    };
                    if (folderBrowser.ShowDialog() != DialogResult.OK)
                        return;

                    var exportDirectory = folderBrowser.SelectedPath;
                    reportManager.Export(ExportFormatEnum.Pdf, Path.Combine(exportDirectory, $"{file.Name}.pdf"));
                }

            }
        }

        #region Helper functions
        private void RecursivelyFillBomQuantities(Dictionary<long, int> storage, BOM bom, long parId, int parentCount)
        {
            if (bom?.InstArray == null)
                return;

            foreach (var inst in bom.InstArray.Where(b => b.ParId == parId))
            {
                var cldId = inst.CldId;
                var quant = inst.Quant * parentCount;
                var comp = bom.CompArray.Single(c => c.Id == inst.CldId);
                var hasChildren = bom.InstArray.Where(i => i.ParId == inst.CldId);
                var childFileId = comp.XRefId;

                if (!hasChildren.Any())
                {
                    if (storage.ContainsKey(childFileId))
                        storage[childFileId] += quant;
                    else
                        storage.Add(childFileId, quant);
                }

                RecursivelyFillBomQuantities(storage, bom, cldId, quant);
            }  
        }

        private string GetBarcodeAsBase64String(string text)
        {
            var qrCode = Zen.Barcode.BarcodeDrawFactory.CodeQr;
            var image = qrCode.Draw(text, 50);

            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Bmp);
                return Convert.ToBase64String(stream.ToArray());
            }
        }
        #endregion
    }
}