﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.
            SetStyle(ControlStyles.DoubleBuffer, true); // 双缓冲

        }

        private void InitViews()
        {
            int pageIndex = 1000;
            //uiNavMenu1.CreateNode("控件", 61451, 24, pageIndex);
        }

        private void ToolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void StartPrewviewBtn_MouseHover(object sender, EventArgs e)
        {
            startPrewviewBtn.Image = Image.FromFile(Globals.startPathInfo.Parent.Parent.FullName + "\\Resources\\" + "开始(1).png");
        }



        private void StopPrewviewBtn_Click(object sender, EventArgs e)
        {
            //isStartPrewview = false;
            //DirectoryInfo path = new DirectoryInfo(Application.StartupPath);
            //startPrewviewBtn.Image = Image.FromFile(path.Parent.Parent.FullName + "\\Resources\\" + "开始 .png");
        }

        private void StartPrewviewBtn_Click(object sender, EventArgs e)
        {

        }

        private void StartRecordBtn_Click(object sender, EventArgs e)
        {

        }
    }
}
