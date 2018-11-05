using System;
using System.Windows.Forms;

namespace SD226856.PowerBIReportsExplorerExtension
{
    public partial class WebBrowserUserControl : UserControl
    {
        public WebBrowserUserControl()
        {
            InitializeComponent();
        }

        public void Navigate(string url)
        {
            WebBrowserControl.Navigate(new Uri(url));
        }
    }
}
