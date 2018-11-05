﻿using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;

namespace SD226856.RdlcFileBomExplorerExtension
{
    public partial class ReportViewerUserControl : UserControl
    {
        public ReportViewerUserControl()
        {
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            InitializeComponent();
        }

        public ReportViewer ReportViewer => reportViewer1;
    }
}
