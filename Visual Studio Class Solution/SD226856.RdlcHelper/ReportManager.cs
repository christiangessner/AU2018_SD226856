using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.Reporting.WinForms;
using Warning = Microsoft.Reporting.WinForms.Warning;

namespace SD226856.RdlcHelper
{
    public class ReportManager : IDisposable
    {
        private static readonly string DATASET_NAME = "AutodeskVault_ReportDataSource";
        public string DatasetName => DATASET_NAME;

        private readonly List<Field> _fields = new List<Field>();
        public IEnumerable<Field> Fields => _fields;

        public LocalReport LocalReport { get; }

        #region Constructor
        public ReportManager(string rdlcFullFileName)
        {
            LocalReport = new LocalReport();
            LoadReport(rdlcFullFileName);
        }

        public ReportManager(LocalReport localReport, string rdlcFullFileName)
        {
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            LocalReport = localReport;
            LoadReport(rdlcFullFileName);
        }

        private void LoadReport(string rdlcFullFileName)
        {
            _fields.Clear();
            LocalReport.DataSources.Clear();

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(rdlcFullFileName);

            using (var stringReader = new StringReader(xmlDocument.OuterXml))
            {
                LocalReport.LoadReportDefinition(stringReader);
                stringReader.Close();
            }

            var manager = new XmlNamespaceManager(xmlDocument.NameTable);
            manager.AddNamespace("x", "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition");
            manager.AddNamespace("rd", "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner");

            var fields = xmlDocument.SelectNodes(
                $"/x:Report/x:DataSets/x:DataSet[@Name = '{DATASET_NAME}']/x:Fields/x:Field", manager);
            if (fields == null)
                return;

            foreach (XmlNode field in fields)
            {
                string name = null;
                if (field.Attributes != null)
                    name = field.Attributes["Name"]?.Value;

                var dataFieldNode = field.SelectSingleNode("x:DataField", manager);
                var typeNameNode = field.SelectSingleNode("rd:TypeName", manager);

                if (name != null && dataFieldNode != null && typeNameNode != null)
                {
                    _fields.Add(new Field
                    {
                        Name = name,
                        DataField = dataFieldNode.InnerText,
                        TypeName = typeNameNode.InnerText
                    });
                }
            }
        }
        #endregion

        #region Export
        public void Export(ExportFormatEnum exportFormat, string outPath)
        {
            byte[] bytes = null;

            string mimeType, encoding, extension;
            string[] streamids;
            Warning[] warnings;

            switch (exportFormat)
            {
                case ExportFormatEnum.Pdf:
                {
                    bytes = LocalReport.Render("PDF");
                    break;
                }
                case ExportFormatEnum.Doc:
                {
                    bytes = LocalReport.Render("WORD", 
                        null, 
                        out mimeType, out encoding, out extension, out streamids, out warnings);
                    break;
                }
                case ExportFormatEnum.Docx:
                {
                    bytes = LocalReport.Render("WORDOPENXML", 
                        null, 
                        out mimeType, out encoding, out extension, out streamids, out warnings);
                    break;
                }
                case ExportFormatEnum.Xls:
                {
                    bytes = LocalReport.Render("EXCEL", 
                        null, 
                        out mimeType, out encoding, out extension, out streamids, out warnings);
                    break;
                }
                case ExportFormatEnum.Xlsx:
                {
                    bytes = LocalReport.Render("EXCELOPENXML", 
                        null, 
                        out mimeType, out encoding, out extension, out streamids, out warnings);
                    break;
                }
                case ExportFormatEnum.Image:
                {
                    bytes = LocalReport.Render("IMAGE", 
                        null, 
                        out mimeType, out encoding, out extension, out streamids, out warnings);
                    break;
                }
            }

            if (File.Exists(outPath))
                File.Delete(outPath);

            if (bytes != null)
                File.WriteAllBytes(outPath, bytes);
        }
        #endregion

        #region Print
        private IList<Stream> _streams;
        private int _pageIndex;

        public void Print(string printerName = null)
        {
            _streams = new List<Stream>();

            Warning[] warnings;
            LocalReport.Render("Image",
                CreateEmfDeviceInfo(),
                CreateStream, out warnings);

            foreach (var stream in _streams)
                stream.Position = 0;

            if (_streams == null || _streams.Count == 0)
                return;

            var printDoc = new PrintDocument();

            if (!string.IsNullOrEmpty(printerName))
                printDoc.PrinterSettings.PrinterName = printerName;

            printDoc.PrintPage += PrintPage;
            _pageIndex = 0;
            printDoc.Print();
        }

        private string CreateEmfDeviceInfo()
        {
            var reportPageSettings = LocalReport.GetDefaultPageSettings();
            var pageSettings = new PageSettings
            {
                PaperSize = reportPageSettings.PaperSize,
                Margins = reportPageSettings.Margins,
                Landscape = reportPageSettings.IsLandscape
            };

            var paperSize = pageSettings.PaperSize;
            var margins = pageSettings.Margins;

            var top = margins.Top;
            var left = margins.Left;
            var right = margins.Right;
            var bottom = margins.Bottom;
            var height = paperSize.Height;
            var width = paperSize.Width;

            if (pageSettings.Landscape)
            {
                top = margins.Left;
                left = margins.Bottom;
                right = margins.Top;
                bottom = margins.Right;
                height = paperSize.Width;
                width = paperSize.Height;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                @"
                    <DeviceInfo>
                        <OutputFormat>emf</OutputFormat>
                        <StartPage>0</StartPage>
                        <EndPage>0</EndPage>
                        <MarginTop>{0}</MarginTop>
                        <MarginLeft>{1}</MarginLeft>
                        <MarginRight>{2}</MarginRight>
                        <MarginBottom>{3}</MarginBottom>
                        <PageHeight>{4}</PageHeight>
                        <PageWidth>{5}</PageWidth>
                    </DeviceInfo>
                ",
                ToInches(top),
                ToInches(left),
                ToInches(right),
                ToInches(bottom),
                ToInches(height),
                ToInches(width)
            );
        }

        private static string ToInches(int hundrethsOfInch)
        {
            double inches = hundrethsOfInch / 100.0;
            return inches.ToString(CultureInfo.InvariantCulture) + "in";
        }

        private Stream CreateStream(string name, string fileNameExtension, Encoding encoding, string mimeType, bool willSeek)
        {
            Stream stream = new MemoryStream();
            _streams.Add(stream);
            return stream;
        }

        private void PrintPage(object sender, PrintPageEventArgs ev)
        {
            var pageImage = new Metafile(_streams[_pageIndex]);
            var adjustedRect = new Rectangle(
                ev.PageBounds.Left - (int)ev.PageSettings.HardMarginX,
                ev.PageBounds.Top - (int)ev.PageSettings.HardMarginY,
                ev.PageBounds.Width,
                ev.PageBounds.Height);

            ev.Graphics.FillRectangle(Brushes.White, adjustedRect);
            ev.Graphics.DrawImage(pageImage, adjustedRect);

            _pageIndex++;
            ev.HasMorePages = (_pageIndex < _streams.Count);
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            if (_streams == null) return;
            foreach (var stream in _streams)
            {
                stream.Close();
                stream.Dispose();
            }
            _streams = null;
        }
        #endregion
    }
}