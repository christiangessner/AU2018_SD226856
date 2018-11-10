using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Connectivity.WebServices;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections;
using Microsoft.Reporting.WinForms;
using SD226856.RdlcHelper;

namespace SD226856.RdlcFileBomExplorerExtension
{
    public partial class ReportViewerWindow : Form
    {
        private readonly Connection _connection;
        private readonly Folder _folder;
        private readonly long[] _fileIds;
        private readonly int[] _quantities;

        public ReportViewerWindow(Connection connection, Folder folder, long[] fileIds, int[] quantities)
        {
            InitializeComponent();

            _connection = connection;
            _folder = folder;
            _fileIds = fileIds;
            _quantities = quantities;
        }


        private void ReportViewerWindow_Load(object sender, EventArgs e)
        {
            var wsm = _connection.WebServiceManager;

            var extensionPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (extensionPath == null)
                return;

            var rdlcFullFileName = Path.Combine(extensionPath, "FileBomPartsOnlyWithBarcode.rdlc");
            using (var reportManager = new ReportManager(reportViewer1.LocalReport, rdlcFullFileName))
            {
                var fieldNames = reportManager.Fields.Select(f => f.DataField).ToArray();

                var propDefs = wsm.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
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

                var propInsts = _connection.WebServiceManager.PropertyService.GetProperties(
                    "FILE", _fileIds, propDefIds.ToArray());

                #region Create Table Rows
                table.BeginLoadData();
                for (var i = 0; i < _fileIds.Length; i++)
                {
                    var row = table.NewRow();
                    row["Quantity"] = _quantities[i];

                    var fileId = _fileIds[i];
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

                #region Load Data Source
                var reportDataSource = new ReportDataSource(table.TableName, table);
                reportManager.LocalReport.DataSources.Add(reportDataSource);
                #endregion

                #region Create Parameters 
                var reportParameters = new List<ReportParameter>
                {
                    new ReportParameter("Vault_FolderName", _folder.Name),
                    new ReportParameter("Vault_UserName", _connection.UserName),
                    new ReportParameter("Vault_SearchRoot", _folder.FullName),
                    new ReportParameter("Vault_SearchConditions", "Current Selection"),
                    new ReportParameter("ReportTitle", "File BOM - Packaging List")
                };
                reportManager.LocalReport.SetParameters(reportParameters.ToArray());
                #endregion

                reportViewer1.ZoomMode = ZoomMode.PageWidth;
                reportViewer1.RefreshReport();
            }
        }

        #region Helper functions
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