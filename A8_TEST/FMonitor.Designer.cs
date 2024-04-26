namespace A8_TEST
{
    partial class FMonitor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FMonitor));
            this.uiPanel1 = new Sunny.UI.UIPanel();
            this.stopPrewviewBtn = new Sunny.UI.UISymbolButton();
            this.startPrewviewBtn = new Sunny.UI.UISymbolButton();
            this.uiNavMenu1 = new Sunny.UI.UINavMenu();
            this.uiPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiPanel1
            // 
            this.uiPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.uiPanel1.Controls.Add(this.stopPrewviewBtn);
            this.uiPanel1.Controls.Add(this.startPrewviewBtn);
            this.uiPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.uiPanel1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(36)))));
            this.uiPanel1.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(36)))));
            this.uiPanel1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(36)))));
            this.uiPanel1.ForeDisableColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(36)))));
            this.uiPanel1.Location = new System.Drawing.Point(0, 0);
            this.uiPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel1.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel1.Name = "uiPanel1";
            this.uiPanel1.RadiusSides = Sunny.UI.UICornerRadiusSides.None;
            this.uiPanel1.RectColor = System.Drawing.Color.Transparent;
            this.uiPanel1.RectSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.None;
            this.uiPanel1.Size = new System.Drawing.Size(892, 49);
            this.uiPanel1.Style = Sunny.UI.UIStyle.Custom;
            this.uiPanel1.StyleCustomMode = true;
            this.uiPanel1.TabIndex = 3;
            this.uiPanel1.Text = "uiPanel1";
            this.uiPanel1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // stopPrewviewBtn
            // 
            this.stopPrewviewBtn.BackColor = System.Drawing.Color.Transparent;
            this.stopPrewviewBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.stopPrewviewBtn.FillColor = System.Drawing.Color.Transparent;
            this.stopPrewviewBtn.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.stopPrewviewBtn.ForeColor = System.Drawing.Color.Transparent;
            this.stopPrewviewBtn.Image = ((System.Drawing.Image)(resources.GetObject("stopPrewviewBtn.Image")));
            this.stopPrewviewBtn.LightColor = System.Drawing.Color.Transparent;
            this.stopPrewviewBtn.Location = new System.Drawing.Point(55, 8);
            this.stopPrewviewBtn.Margin = new System.Windows.Forms.Padding(10, 3, 3, 3);
            this.stopPrewviewBtn.MinimumSize = new System.Drawing.Size(1, 1);
            this.stopPrewviewBtn.Name = "stopPrewviewBtn";
            this.stopPrewviewBtn.RectColor = System.Drawing.Color.Transparent;
            this.stopPrewviewBtn.RectHoverColor = System.Drawing.Color.Transparent;
            this.stopPrewviewBtn.RectPressColor = System.Drawing.Color.Transparent;
            this.stopPrewviewBtn.RectSelectedColor = System.Drawing.Color.Transparent;
            this.stopPrewviewBtn.Size = new System.Drawing.Size(30, 35);
            this.stopPrewviewBtn.SymbolColor = System.Drawing.Color.Transparent;
            this.stopPrewviewBtn.TabIndex = 1;
            this.stopPrewviewBtn.TipsColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.stopPrewviewBtn.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.stopPrewviewBtn.TipsText = "停止采集";
            this.stopPrewviewBtn.Click += new System.EventHandler(this.StopPrewviewBtn_Click);
            // 
            // startPrewviewBtn
            // 
            this.startPrewviewBtn.BackColor = System.Drawing.Color.Transparent;
            this.startPrewviewBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.startPrewviewBtn.FillColor = System.Drawing.Color.Transparent;
            this.startPrewviewBtn.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.startPrewviewBtn.ForeColor = System.Drawing.Color.Transparent;
            this.startPrewviewBtn.Image = ((System.Drawing.Image)(resources.GetObject("startPrewviewBtn.Image")));
            this.startPrewviewBtn.LightColor = System.Drawing.Color.Transparent;
            this.startPrewviewBtn.Location = new System.Drawing.Point(12, 8);
            this.startPrewviewBtn.MinimumSize = new System.Drawing.Size(1, 1);
            this.startPrewviewBtn.Name = "startPrewviewBtn";
            this.startPrewviewBtn.RectColor = System.Drawing.Color.Transparent;
            this.startPrewviewBtn.RectHoverColor = System.Drawing.Color.Transparent;
            this.startPrewviewBtn.RectPressColor = System.Drawing.Color.Transparent;
            this.startPrewviewBtn.RectSelectedColor = System.Drawing.Color.Transparent;
            this.startPrewviewBtn.Size = new System.Drawing.Size(30, 35);
            this.startPrewviewBtn.SymbolColor = System.Drawing.Color.Transparent;
            this.startPrewviewBtn.TabIndex = 0;
            this.startPrewviewBtn.TipsColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.startPrewviewBtn.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.startPrewviewBtn.TipsForeColor = System.Drawing.Color.Maroon;
            this.startPrewviewBtn.TipsText = "开始采集";
            // 
            // uiNavMenu1
            // 
            this.uiNavMenu1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(36)))));
            this.uiNavMenu1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.uiNavMenu1.Dock = System.Windows.Forms.DockStyle.Left;
            this.uiNavMenu1.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawAll;
            this.uiNavMenu1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(36)))));
            this.uiNavMenu1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiNavMenu1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.uiNavMenu1.FullRowSelect = true;
            this.uiNavMenu1.ItemHeight = 50;
            this.uiNavMenu1.Location = new System.Drawing.Point(0, 49);
            this.uiNavMenu1.MenuStyle = Sunny.UI.UIMenuStyle.Custom;
            this.uiNavMenu1.Name = "uiNavMenu1";
            this.uiNavMenu1.ShowLines = false;
            this.uiNavMenu1.Size = new System.Drawing.Size(250, 495);
            this.uiNavMenu1.TabIndex = 4;
            this.uiNavMenu1.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiNavMenu1.Visible = false;
            // 
            // FMonitor
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(36)))));
            this.ClientSize = new System.Drawing.Size(892, 544);
            this.Controls.Add(this.uiNavMenu1);
            this.Controls.Add(this.uiPanel1);
            this.Name = "FMonitor";
            this.Style = Sunny.UI.UIStyle.Custom;
            this.Text = "FormMonitor";
            this.uiPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        protected Sunny.UI.UIPanel uiPanel1;
        private Sunny.UI.UINavMenu uiNavMenu1;
        private Sunny.UI.UISymbolButton startPrewviewBtn;
        private Sunny.UI.UISymbolButton stopPrewviewBtn;
    }
}