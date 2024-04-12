using a8sdk;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;

namespace A8_TEST
{
    public partial class FormMain : UIForm
    {
        const uint DISPLAYWND_GAP = 1;//监控画面的间隙
        const uint DISPLAYWND_MARGIN_LEFT = 1;//监控画面距离左边控件的距离
        const uint DISPLAYWND_MARGIN_TOP = 1; //监控画面距离上边的距离
        const int PAGE_INDEX = 1000;

        Color PIC_CLICKED_COLOR = Color.FromArgb(128, 128, 255);
        Color PIC_UNCLICKED_COLOR = Color.FromArgb(45, 45, 53);
        private PictureBox[] pics;//显示图像控件
        private UIPage fmonitor;//监控界面

        tstRtmp rtmp = new tstRtmp();//利用ffmpeg获取视频数据
        Rtsplz rtsplz = new Rtsplz();
        Thread thPlayer;//播放红外图像线程
        Thread thPlayerlz;
        SynchronizationContext m_SyncContext = null; //获取上下文

        List<string> ipLists = new List<string>(); //设备ip集合
        List<A8SDK> a8Lists = new List<A8SDK>();
        private bool saveVideoFlag = false;
     
        // private UIPanel pixUIPanel;//容纳PictureBox的Panel
        //public static TransparentLabel[] labels;//图像上面标注控件,背景透明

        A8SDK.globa_temp globa_Temp;

        List<List<Bitmap>> imageList = new List<List<Bitmap>>();//存储图像的集合
        List<Bitmap> irImageList = new List<Bitmap>();
        Thread threadPrewview;


        [Obsolete]
        public FormMain()
        {

            InitializeComponent();
            m_SyncContext = SynchronizationContext.Current;
            Control.CheckForIllegalCrossThreadCalls = false;


            //查找红外设备ip地址
            ipLists = FindDeviceIpAddress();

            if (ipLists.Count > 0)
            {
                for (int i = 0; i < ipLists.Count; i++)
                {
                    //初始化红外设备对象，并添加到集合
                    A8SDK a8 = new A8SDK(ipLists[i]);
                    a8Lists.Add(a8);
                }
            }

            int pageIndex = PAGE_INDEX;

            //设置关联
            uiNavBar1.TabControl = uiTabControl1;

            //uiNavBar1设置节点，也可以在Nodes属性里配置
            uiNavBar1.Nodes.Add("红外监控");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[0], 61501);//设置图标

            //添加实时监控界面
            fmonitor = new FMonitor();
            AddPage(fmonitor, pageIndex);
            uiNavBar1.SetNodePageIndex(uiNavBar1.Nodes[0], pageIndex);//设置显示的初始界面为实时监控界面

            uiNavBar1.Nodes.Add("图像浏览");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[1], 61502);

            uiNavBar1.Nodes.Add("系统设置");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[2], 61459);

            //设置默认显示界面
            uiNavBar1.SelectedIndex = 0;

            initDatas();

            SetFmonitorDisplayWnds(1, 2);

            thPlayer = new Thread(DeCoding);
            thPlayer.IsBackground = true;
            thPlayer.Start();


            threadPrewview = new Thread(Prewview);
            threadPrewview.IsBackground = true;
            threadPrewview.Start();


        }

        private void Prewview()
        {
            int i = 0;
            while (true)
            {
                try
                {
                    if (irImageList.Count > 0)
                    {
                        Bitmap bitmap = irImageList[0];
                        //Bitmap bitmap = imageList[0][0];
                        pics[0].Image = bitmap;

                        if (bitmap != null)
                        {
                            using (Graphics gfx = Graphics.FromImage(bitmap))
                            {

                                Font font = new Font("Arial", 12);
                                Brush brush = Brushes.Red;
                                Pen pen = new Pen(Color.Red, 2);

                                string maxTemp;
                                PointF point;
                                float pt = 2.0f; //显示水平缩放比例温度数据是384 * 288，视频图像768 * 576      

                                // a8.Get_globa_temp(out globa_Temp);//获取全局温度信息

                                maxTemp = ((float)globa_Temp.max_temp / 10).ToString("F1");//全局最高温度

                                float maxTempX = globa_Temp.max_temp_x * pt;
                                float maxTempY = globa_Temp.max_temp_y * pt;

                                SizeF maxTempStringSize = gfx.MeasureString(maxTemp, font);

                                if (maxTempX + maxTempStringSize.Width > bitmap.Width)
                                {
                                    maxTempX = maxTempX - maxTempStringSize.Width;
                                }

                                if (maxTempY + maxTempStringSize.Height > bitmap.Height)
                                {
                                    maxTempY = maxTempY - maxTempStringSize.Height;
                                }
                                point = new PointF(maxTempX, maxTempY);

                                gfx.DrawString(maxTemp, font, brush, point);

                                DrawCrossLine(gfx, globa_Temp.max_temp_x * pt, globa_Temp.max_temp_y * pt, pen, 10);

                                bitmap = null;

                            }
                        }
                    }

                    irImageList.RemoveAt(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                Thread.Sleep(40);
            }

        }

        private void initDatas()
        {
          
            pics = new PictureBox[2 * 2];//定义显示图像控件，每个设备显示可见光和红外图像。

        }


        /// <summary>
        /// 设置实时监控界面相关控件大小及显示位置
        /// </summary>
        /// <param name="rowNum">行数</param>
        /// <param name="colNum">列数</param>

        private void SetFmonitorDisplayWnds(uint rowNum, uint colNum)
        {
            //uint w = (uint)(Screen.PrimaryScreen.Bounds.Width - fmonitor.GetControl("uiPanel1").Width);
            uint w = (uint)(Screen.PrimaryScreen.Bounds.Width - fmonitor.GetControl("uiNavMenu1").Width);
            uint h = (uint)(Screen.PrimaryScreen.Bounds.Height - uiNavBar1.Height - fmonitor.GetControl("uiPanel1").Height);


            //先计算显示窗口的位置和大小，依据为：在不超过主窗口大小的情况下尽可能大，同时严格保持4:3的比例显示
            uint real_width = w;
            uint real_height = h;

            uint display_width = (real_width - DISPLAYWND_MARGIN_LEFT * 2 - (colNum - 1) * DISPLAYWND_GAP) / colNum;//单个相机显示区域的宽度(还未考虑比例)
            uint display_height = (real_height - DISPLAYWND_MARGIN_TOP * 2 - (rowNum - 1) * DISPLAYWND_GAP) / rowNum;//单个相机显示区域的高度(还未考虑比例)

            if (display_width * 3 >= display_height * 4)//考虑比例
            {
                uint ret = display_height % 3;
                if (ret != 0)
                {
                    display_height -= ret;
                }
                display_width = display_height * 4 / 3;
            }
            else
            {
                uint ret = display_width % 4;
                if (ret != 0)
                {
                    display_width -= ret;
                }
                display_height = display_width * 3 / 4;
            }



            for (uint i = 0; i < rowNum; i++)
            {
                uint y = (uint)fmonitor.GetControl("uiPanel1").Height + (real_height - rowNum * display_height - DISPLAYWND_GAP * (rowNum - 1)) / 2 + (display_height + DISPLAYWND_GAP) * i;
                for (uint j = 0; j < colNum; j++)
                {
                    uint x = (uint)fmonitor.GetControl("uiNavMenu1").Width + (real_width - colNum * display_width - DISPLAYWND_GAP * (colNum - 1)) / 2 + (display_width + DISPLAYWND_GAP) * j;

                    pics[i * 2 + j] = new PictureBox();
                    pics[i * 2 + j].Left = (int)x;
                    pics[i * 2 + j].Top = (int)y;
                    pics[i * 2 + j].Width = (int)display_width;
                    pics[i * 2 + j].Height = (int)display_height;
                    pics[i * 2 + j].Show();
                    pics[i * 2 + j].BackColor = Color.FromArgb(45, 45, 53);
                    //pics[i * 2 + j].Image = Image.FromFile(@"D:\C#\IRAY_Test\IR_Tmp_Measurement\bin\Debug\AlarmImage\20230330105027_Visual.jpg");
                    //pics[i * 2 + j].Name = "pic" + (i * 2 + j).ToString();
                    pics[i * 2 + j].SizeMode = PictureBoxSizeMode.StretchImage;
                   

                    fmonitor.Controls.Add(pics[i * 2 + j]);

                    //labels[i * 2 + j] = new TransparentLabel();
                    //labels[i * 2 + j].Left = (int)x;
                    //labels[i * 2 + j].Top = (int)y;
                    ////labels[i * 2 + j].Text = "轴" + (i * 2 + j + 1).ToString();
                    //labels[i * 2 + j].ForeColor = Color.WhiteSmoke;
                    //labels[i * 2 + j].BackColor = Color.Transparent;
                    //// label.Show();

                    //fmonitor.Controls.Add(labels[i * 2 + j]);
                    ////pic[i * 2 + j].Controls.Add(label);
                    ////label.Parent = fmonitor;
                    //labels[i * 2 + j].BringToFront();

                    //switch (i * 2 + j)
                    //{
                    //    case 0:
                    //        pics[i * 2 + j].Tag = 0;
                    //        pics[i * 2 + j].Paint += new PaintEventHandler(Pics0_Paint);
                    //        pics[i * 2 + j].Click += new EventHandler(Pics0_Click);
                    //        break;
                    //    case 1:
                    //        pics[i * 2 + j].Tag = 0;
                    //        pics[i * 2 + j].Paint += new PaintEventHandler(Pics1_Paint);
                    //        pics[i * 2 + j].Click += new EventHandler(Pics1_Click);
                    //        break;
                    //    case 2:
                    //        pics[i * 2 + j].Tag = 0;
                    //        pics[i * 2 + j].Paint += new PaintEventHandler(Pics2_Paint);
                    //        pics[i * 2 + j].Click += new EventHandler(Pics2_Click);
                    //        break;
                    //    case 3:
                    //        pics[i * 2 + j].Tag = 0;
                    //        pics[i * 2 + j].Paint += new PaintEventHandler(Pics3_Paint);
                    //        pics[i * 2 + j].Click += new EventHandler(Pics3_Click);
                    //        break;

                    //}

                }

            }

            //foreach (PictureBox p in fmonitor.GetControls<PictureBox>())
            //{
            //    Console.WriteLine(p.Name);
            //}
        }

        /// <summary>
        /// 查找红外设备IP地址
        /// </summary>
        /// <returns></returns>
        private List<string> FindDeviceIpAddress()
        {

            //SDK初始化
            int res = A8SDK.SDK_initialize();

            if (res == 0)//初始化成功
            {
                string device_List;

                //搜索设备IP地址
                res = A8SDK.SDK_serch_device(1024, out device_List);

                if (res == 0)//搜索设备IP地址成功

                {
                    //分割字符串
                    string[] deviceLists = device_List.Split(new char[] { ',' });//[{"UID":"","IP":"192.168.0.221","MAC":"5A0A7C0186B6","TEMPWIDTH":"384","TEMPHEIGHT":"288","IMAGEWIDTH":"768","IMAGEHEIGHT":"576","DEVICENAME":"mrc123","DEVICETYPE":"Apare-ET300"}]

                    //遍历字符串，查找ip地址
                    foreach (string item in deviceLists)
                    {
                        if (item.Contains("IP"))//"IP":"192.168.0.221"
                        {
                            //截取设备IP地址
                            string ip = item.Substring(6, item.Length - 7);
                            ipLists.Add(ip);
                        }
                    }

                }

            }
            return ipLists;
        }

        /// <summary>
        /// bitmap 位图转为mat类型 
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Mat Bitmap2Mat(Bitmap bitmap)
        {
            MemoryStream s2_ms = null;
            Mat source = null;
            try
            {
                using (s2_ms = new MemoryStream())
                {
                    bitmap.Save(s2_ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    source = Mat.FromStream(s2_ms, ImreadModes.AnyColor);
                }
            }
            catch (Exception e)
            {

            }
            finally
            {
                if (s2_ms != null)
                {
                    s2_ms.Close();
                    s2_ms = null;
                }
                GC.Collect();
            }
            return source;
        }


        [Obsolete]
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            if (thPlayer != null)
            {
                rtmp.Stop();

                thPlayer = null;
            }
            else
            {
                thPlayer = new Thread(DeCoding);
                thPlayer.IsBackground = true;
                thPlayer.Start();
                button1.Text = "停止播放";
                button1.Enabled = true;
            }

            //thPreview = new Thread(ShowIRImageThreadProc);
            //thPreview.IsBackground = true;
            //thPreview.Start();
        }

        CancellationTokenSource cts = new CancellationTokenSource();
        private VideoWriter VideoWriter;


        /// <summary>
        /// 播放线程执行方法
        /// </summary>
        [Obsolete]
        private unsafe void DeCoding()
        {
            try
            {
                Console.WriteLine("DeCoding run...");
                Bitmap oldBmp = null;
                //利用opencv 录制视频
                VideoWriter writer = new VideoWriter("output_video.avi", FourCC.XVID, 25, new OpenCvSharp.Size(768, 576), true);

                // 更新图片显示
                tstRtmp.ShowBitmap show = (bmp) =>
                {

                    irImageList.Add(bmp);

                    if (irImageList.Count > 3)
                    {
                        irImageList.RemoveAt(0);
                    }
                    //imageList[0].Add(bmp);
                    //if(imageList[0].Count > 3)
                    //{
                    //    imageList[0].RemoveAt(0);
                    //}

                    //this.Invoke(new MethodInvoker(() =>
                    //{
                    //    if (bmp != null)
                    //    {

                    //        using (Graphics gfx = Graphics.FromImage(bmp))//bmp.Width = 768,bmp.Height = 576
                    //        {
                    //            // 设置文字的格式
                    //            Font font = new Font("Arial", 12);
                    //            Brush brush = Brushes.Red;
                    //            Pen pen = new Pen(Color.Red, 2);


                    //            string maxTemp;
                    //            PointF point;
                    //            float pt = 2.0f; //显示水平缩放比例温度数据是384 * 288，视频图像768 * 576      


                    //            a8Lists[0].Get_globa_temp(out globa_Temp);//获取全局温度信息
                    //            maxTemp = ((float)globa_Temp.max_temp / 10).ToString("F1");//全局最高温度


                    //            float maxTempX = globa_Temp.max_temp_x * pt;
                    //            float maxTempY = globa_Temp.max_temp_y * pt;

                    //            SizeF maxTempStringSize = gfx.MeasureString(maxTemp, font);

                    //            if (maxTempX + maxTempStringSize.Width > bmp.Width)
                    //            {
                    //                maxTempX = maxTempX - maxTempStringSize.Width;
                    //            }

                    //            if (maxTempY + maxTempStringSize.Height > bmp.Height)
                    //            {
                    //                maxTempY = maxTempY - maxTempStringSize.Height;
                    //            }
                    //            point = new PointF(maxTempX, maxTempY);

                    //            gfx.DrawString(maxTemp, font, brush, point);

                    //            DrawCrossLine(gfx, globa_Temp.max_temp_x * pt, globa_Temp.max_temp_y * pt, pen, 10);

                    //        }
                    //    }

                    //    // 保存Bitmap到文件
                    //    //bmp.Save(@"C:\Users\Dell\Desktop\1.png", System.Drawing.Imaging.ImageFormat.Png);

                    //    if (saveVideoFlag)
                    //    {
                    //        Console.WriteLine(writer.IsOpened());
                    //        Mat mat = Bitmap2Mat(bmp);
                    //        writer.Write(mat);
                    //    }
                    //    //pic.Image = bmp;
                    //    pics[0].Image = bmp;
                    //    if (oldBmp != null)
                    //    {
                    //        oldBmp.Dispose();
                    //    }
                    //    oldBmp = bmp;
                    //}));
                };
                rtmp.Start(show, "rtsp://192.168.1.80/webcam");
                //rtmp.StartSave(show, "rtsp://192.168.100.2/webcam", "D://123//1.mp4");
                //rtmp.Start_save(show, "rtsp://192.168.100.2/webcam", "D://123//1.mp4");


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("DeCoding exit");
                this.Invoke(new MethodInvoker(() =>
                {
                    button1.Text = "开始播放";
                    button1.Enabled = true;
                }));
            }
        }

        /// <summary>
        /// 绘制十字交叉线
        /// </summary>
        /// <param name="g"></param>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="pen"></param>
        /// <param name="lineWidth"></param>
        private void DrawCrossLine(Graphics g, float startX, float startY, Pen pen, int lineLength)
        {
            g.DrawLine(pen, startX, startY, startX + lineLength, startY);
            g.DrawLine(pen, startX, startY, startX - lineLength, startY);
            g.DrawLine(pen, startX, startY, startX, startY + lineLength);
            g.DrawLine(pen, startX, startY, startX, startY - lineLength);
        }

        private unsafe void DeCodinglz()
        {
            try
            {
                Console.WriteLine("DeCodinglz run...");
                tstRtmp.ShowBitmap show = (bmp) =>
                {
                    this.Invoke(new MethodInvoker(() =>
                    {

                    }));
                };
                //rtmp.StartSave(show,"rtsp://192.168.100.2/webcam", "D://123//1.mp4");
                rtsplz.StartSave("rtsp://192.168.0.221/webcam", "D://123//1.mp4");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("DeCoding exit");
                this.Invoke(new MethodInvoker(() =>
                {

                }));
            }


        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            if (button2.Text == "开始录制")
            {
                if (thPlayerlz != null)
                {
                    //rtsplz.Stoplz();
                    button2.Text = "开始录制";
                    button2.Enabled = true;
                    thPlayerlz = null;
                }
                else
                {
                    saveVideoFlag = true;

                    //thPlayerlz = new Thread(DeCodinglz);
                    //thPlayerlz = new Thread(SaveVideoThread);
                    //thPlayerlz.IsBackground = true;
                    //thPlayerlz.Start();
                    button2.Text = "停止录制";
                    button2.Enabled = true;
                }
            }
            else
            {
                saveVideoFlag = false;
                if (VideoWriter != null && !VideoWriter.IsDisposed)
                {
                    VideoWriter.Dispose();
                    VideoWriter = null;
                }

                rtsplz.Stoplz();
                button2.Text = "开始录制";
                button2.Enabled = true;

                if (thPlayerlz != null)
                {
                    thPlayerlz = null;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void UiButton1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            if (thPlayer != null)
            {
                rtmp.Stop();

                thPlayer = null;
            }
            else
            {
                thPlayer = new Thread(DeCoding);
                thPlayer.IsBackground = true;
                thPlayer.Start();
                button1.Text = "停止播放";
                button1.Enabled = true;
            }

            //thPreview = new Thread(ShowIRImageThreadProc);
            //thPreview.IsBackground = true;
            //thPreview.Start();
        }

        private void ToolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            a8Lists[0].Get_globa_temp(out globa_Temp);//获取全局温度信息
        }
    }
}
