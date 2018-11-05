using System;
using System.Windows.Forms;
using SD226856.RdlcHelper;

namespace SD226856.RdlcAnalyzer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void OpenMenuItemClick(object sender, EventArgs e)
        {
            const string filter = "Report template files (*.rdlc)|*.rdlc";
            const string directory = @"C:\Program Files\Autodesk\Vault Professional 2019\Explorer\Report Templates";

            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Multiselect = false,
                CheckFileExists = true,
                InitialDirectory = directory
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var rdlc = dialog.FileName;

                using (var reportManager = new ReportManager(rdlc))
                {
                    var parameters = reportManager.LocalReport.GetParameters();
                    viewParams.DataSource = parameters;

                    var fields = reportManager.Fields;
                    viewFields.DataSource = fields;
                }
            }
        }

        private void ExitMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }
    }
}