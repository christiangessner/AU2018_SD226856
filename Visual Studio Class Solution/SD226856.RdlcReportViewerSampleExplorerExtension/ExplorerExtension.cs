using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
using Autodesk.Connectivity.Explorer.Extensibility;

[assembly: Autodesk.Connectivity.Extensibility.Framework.ExtensionId("7349b40a-ed83-4cb2-b944-92ba152a0716")]
[assembly: Autodesk.Connectivity.Extensibility.Framework.ApiVersion("12.0")]

namespace SD226856.RdlcReportViewerSampleExplorerExtension
{
    public class ExplorerExtension : IExplorerExtension
    {
        #region IExplorerExtension Members
        public IEnumerable<CommandSite> CommandSites()
        {
            return null;
        }

        public IEnumerable<DetailPaneTab> DetailTabs()
        {
            var detailPaneTab = new DetailPaneTab(
                "AU.SD226856.RdlcReportViewerSampleTab",
                "Sample Report",
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
            var reportViewer = (e.Context.UserControl as ReportViewerUserControl)?.ReportViewer;
            if (reportViewer == null)
                return;

            reportViewer.Clear();

            var extensionPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (extensionPath == null)
                return;

            #region Load Report Template file
            var rdlcFullFileName = Path.Combine(extensionPath, "SampleReportTemplate.rdlc");
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(rdlcFullFileName);
            using (var stringReader = new StringReader(xmlDocument.OuterXml))
            {
                reportViewer.LocalReport.LoadReportDefinition(stringReader);
                stringReader.Close();
            }
            #endregion

            #region Create Table and Columns
            var table = new DataTable("AutodeskVault_ReportDataSource");

            table.BeginInit();
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            table.Columns.Add(new DataColumn("City", typeof(string)));
            table.Columns.Add(new DataColumn("Start", typeof(DateTime)));
            table.Columns.Add(new DataColumn("End", typeof(DateTime)));
            table.EndInit();
            #endregion

            #region Create Table Rows
            table.BeginLoadData();
            table.Rows.Add(CreateDataRow(table, "AU MIDDLE EAST", "Dubai", 
                new DateTime(2018, 5, 7), new DateTime(2018, 5, 8)));
            table.Rows.Add(CreateDataRow(table, "AU UNITED KINGDOM", "London",
                new DateTime(2018, 6, 18), new DateTime(2018, 6, 19)));
            table.Rows.Add(CreateDataRow(table, "AU JAPAN", "Tokyo",
                new DateTime(2018, 8, 31), new DateTime(2018, 8, 31)));
            table.Rows.Add(CreateDataRow(table, "AU INDIA", "Delhi",
                new DateTime(2018, 9, 11), new DateTime(2018, 9, 11)));
            table.Rows.Add(CreateDataRow(table, "AU CHINA", "Hangzhou",
                new DateTime(2018, 9, 20), new DateTime(2018, 9, 20)));
            table.Rows.Add(CreateDataRow(table, "AU RUSSIA", "Moscow",
                new DateTime(2018, 10, 3), new DateTime(2018, 10, 4)));
            table.Rows.Add(CreateDataRow(table, "AU GERMANY", "Darmstadt",
                new DateTime(2018, 10, 17), new DateTime(2018, 10, 18)));
            table.Rows.Add(CreateDataRow(table, "AU AFRICA", "Johannesburg",
                new DateTime(2018, 10, 24), new DateTime(2018, 10, 25)));
            table.Rows.Add(CreateDataRow(table, "AU SOUTH KOREA", "Seoul",
                new DateTime(2018, 10, 30), new DateTime(2018, 10, 30)));
            table.Rows.Add(CreateDataRow(table, "AU U.S.A.", "Las Vegas",
                new DateTime(2018, 11, 13), new DateTime(2018, 11, 15)));
            table.EndLoadData();
            #endregion

            #region Load Data Source
            var reportDataSource = new Microsoft.Reporting.WinForms.ReportDataSource(table.TableName, table);
            reportViewer.LocalReport.DataSources.Add(reportDataSource);
            #endregion

            #region Create Parameters
            var reportParameters = new List<Microsoft.Reporting.WinForms.ReportParameter>
            {
                new Microsoft.Reporting.WinForms.ReportParameter("Title", "Autodesk University Around the World"),
                new Microsoft.Reporting.WinForms.ReportParameter("Class", "SD226856 - Custom Reporting in Vault 2019")
            };
            reportViewer.LocalReport.SetParameters(reportParameters.ToArray());
            #endregion

            reportViewer.ZoomMode = Microsoft.Reporting.WinForms.ZoomMode.PageWidth;
            reportViewer.RefreshReport();
        }

        #region Helper functions
        private DataRow CreateDataRow(DataTable table, string name, string city, DateTime start, DateTime end)
        {
            var row = table.NewRow();
            row["Name"] = name;
            row["City"] = city;
            row["Start"] = start;
            row["End"] = end;
            return row;
        }
        #endregion
    }
}