using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;

namespace A8_TEST
{
    public partial class FMonitor : UIPage
    {
        public FMonitor()
        {
            InitializeComponent();
            InitViews();
        }

        private void InitViews()
        {
            int pageIndex = 1000;
            uiNavMenu1.CreateNode("控件", 61451, 24, pageIndex);
        }

        private void ToolStripButton1_Click(object sender, EventArgs e)
        {

        }
    }
}
