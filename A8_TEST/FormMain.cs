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
using System.Runtime.InteropServices;

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
                                // private UIPanel pixUIPanel;//容纳PictureBox的Panel
                                //public static TransparentLabel[] labels;//图像上面标注控件,背景透明

        #region 红外
        tstRtmp rtmp = new tstRtmp();//利用ffmpeg获取视频数据
        Rtsplz rtsplz = new Rtsplz();
        Thread thPlayer;//解码红外视频线程
        Thread thPlayerlz;
        SynchronizationContext m_SyncContext = null; //获取上下文
        List<string> ipLists = new List<string>(); //设备ip集合
        List<A8SDK> a8Lists = new List<A8SDK>(); //红外设备A8SDK对象集合
        A8SDK.globa_temp globa_Temp;//全局温度结构对象
        List<Bitmap> irImageList = new List<Bitmap>();//存储红外图像集合
        Thread threadPrewview;//红外图像预览线程
        #endregion

        #region 可见光
        private Int32 m_lUserID = -1;
        private Int32 m_lRealHandle = -1;
        private bool m_bInitSDK = false;
        private uint iLastErr = 0;

        private string str;
        public CHCNetSDK.NET_DVR_USER_LOGIN_INFO struLogInfo;
        CHCNetSDK.LOGINRESULTCALLBACK LoginCallBack = null;
        public CHCNetSDK.NET_DVR_DEVICEINFO_V40 DeviceInfo;
        CHCNetSDK.REALDATACALLBACK RealData = null;
        public CHCNetSDK.NET_DVR_TIME m_struTimeCfg;

        #endregion


        private bool saveImageFlag;
        private bool saveAlarmImageFlag;
        private bool saveAlarmIrImageFlag;
        Object obj0 = new Object();

        [Obsolete]
        public FormMain()
        {

            InitializeComponent();

            m_SyncContext = SynchronizationContext.Current;
            Control.CheckForIllegalCrossThreadCalls = false;

            //读取配置文件
            Globals.ReadInfoXml<SystemParam>(ref Globals.systemParam, Globals.systemXml);

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


            //初始化数据
            initDatas();
            SetFmonitorDisplayWnds(2, 2);


            LoginOpDevice_1();
            PreviewOpDevice_1();

            Timing();

            //开启解码红外视频线程
            thPlayer = new Thread(DeCoding);
            thPlayer.IsBackground = true;
            thPlayer.Start();

            //开启红外图像预览线程
            threadPrewview = new Thread(Prewview);
            threadPrewview.IsBackground = true;
            threadPrewview.Start();
        }

        private void PreviewOpDevice_1()
        {
            if (m_lUserID < 0)
            {
                MessageBox.Show("Please login the device firstly");
                return;
            }

            if (m_lRealHandle < 0)
            {
                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();
                lpPreviewInfo.hPlayWnd = pics[1].Handle;//预览窗口
                lpPreviewInfo.lChannel = Int16.Parse("1");//预te览的设备通道
                lpPreviewInfo.dwStreamType = 0;//码流类型：0-主码流，1-子码流，2-码流3，3-码流4，以此类推
                lpPreviewInfo.dwLinkMode = 0;//连接方式：0- TCP方式，1- UDP方式，2- 多播方式，3- RTP方式，4-RTP/RTSP，5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = true; //0- 非阻塞取流，1- 阻塞取流
                lpPreviewInfo.dwDisplayBufNum = 1; //播放库播放缓冲区最大缓冲帧数
                lpPreviewInfo.byProtoType = 0;
                lpPreviewInfo.byPreviewMode = 0;

                //if (textBoxID.Text != "")
                //{
                //    lpPreviewInfo.lChannel = -1;
                //    byte[] byStreamID = System.Text.Encoding.Default.GetBytes(textBoxID.Text);
                //    lpPreviewInfo.byStreamID = new byte[32];
                //    byStreamID.CopyTo(lpPreviewInfo.byStreamID, 0);
                //}


                if (RealData == null)
                {
                    RealData = new CHCNetSDK.REALDATACALLBACK(RealDataCallBack);//预览实时流回调函数
                }

                IntPtr pUser = new IntPtr();//用户数据

                //打开预览 Start live view 
                m_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, null/*RealData*/, pUser);
                if (m_lRealHandle < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_RealPlay_V40 failed, error code= " + iLastErr; //预览失败，输出错误号
                    MessageBox.Show(str);
                    return;
                }
                else
                {

                }
            }
            else
            {
                //停止预览 Stop live view 
                if (!CHCNetSDK.NET_DVR_StopRealPlay(m_lRealHandle))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_StopRealPlay failed, error code= " + iLastErr;
                    MessageBox.Show(str);
                    return;
                }
                m_lRealHandle = -1;


            }
            return;

        }

        public void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            if (dwBufSize > 0)
            {


                byte[] sData = new byte[dwBufSize];
                Marshal.Copy(pBuffer, sData, 0, (Int32)dwBufSize);


                //if (saveImageFlag1)
                //{
                //    string strFileName = "test1.jpg";
                //    Cv2.ImWrite(strFileName, mgMatShow);
                //    saveImageFlag1 = false;
                //}


                //string str = "实时流数据.ps";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)dwBufSize;
                //fs.Write(sData, 0, iLen);
                //fs.Close();


            }
        }

        private void LoginOpDevice_1()
        {
            m_bInitSDK = CHCNetSDK.NET_DVR_Init();
            if (m_bInitSDK == false)
            {
                MessageBox.Show("NET_DVR_Init error!");
                return;
            }
            else
            {
                //保存SDK日志 To save the SDK log
                CHCNetSDK.NET_DVR_SetLogToFile(3, "C:\\SdkLog\\", true);
            }

            if (m_lUserID < 0)
            {

                struLogInfo = new CHCNetSDK.NET_DVR_USER_LOGIN_INFO();

                //设备IP地址或者域名
                byte[] byIP = System.Text.Encoding.Default.GetBytes(Globals.systemParam.op_ip_1);
                struLogInfo.sDeviceAddress = new byte[129];
                byIP.CopyTo(struLogInfo.sDeviceAddress, 0);

                //设备用户名
                byte[] byUserName = System.Text.Encoding.Default.GetBytes(Globals.systemParam.op_username_1);
                struLogInfo.sUserName = new byte[64];
                byUserName.CopyTo(struLogInfo.sUserName, 0);

                //设备密码
                byte[] byPassword = System.Text.Encoding.Default.GetBytes(Globals.systemParam.op_psw_1);
                struLogInfo.sPassword = new byte[64];
                byPassword.CopyTo(struLogInfo.sPassword, 0);

                struLogInfo.wPort = ushort.Parse(Globals.systemParam.op_port_1);//设备服务端口号

                if (LoginCallBack == null)
                {
                    LoginCallBack = new CHCNetSDK.LOGINRESULTCALLBACK(cbLoginCallBack);//注册回调函数                    
                }
                struLogInfo.cbLoginResult = LoginCallBack;
                struLogInfo.bUseAsynLogin = false; //是否异步登录：0- 否，1- 是 

                DeviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V40();

                //登录设备 Login the device
                m_lUserID = CHCNetSDK.NET_DVR_Login_V40(ref struLogInfo, ref DeviceInfo);
                if (m_lUserID < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_Login_V40 failed, error code= " + iLastErr; //登录失败，输出错误号
                    MessageBox.Show(str);
                    return;
                }
                else
                {
                    //登录成功
                    MessageBox.Show("Login Success!");
                }

            }
            else
            {
                //注销登录 Logout the device
                if (m_lRealHandle >= 0)
                {
                    MessageBox.Show("Please stop live view firstly");
                    return;
                }

                if (!CHCNetSDK.NET_DVR_Logout(m_lUserID))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_Logout failed, error code= " + iLastErr;
                    MessageBox.Show(str);
                    return;
                }
                m_lUserID = -1;

            }
        }

        public void cbLoginCallBack(int lUserID, int dwResult, IntPtr lpDeviceInfo, IntPtr pUser)
        {
            //string strLoginCallBack = "登录设备，lUserID：" + lUserID + "，dwResult：" + dwResult;

            //if (dwResult == 0)
            //{
            //    uint iErrCode = CHCNetSDK.NET_DVR_GetLastError();
            //    strLoginCallBack = strLoginCallBack + "，错误号:" + iErrCode;
            //}

            ////下面代码注释掉也会崩溃
            //if (InvokeRequired)
            //{
            //    object[] paras = new object[2];
            //    paras[0] = strLoginCallBack;
            //    paras[1] = lpDeviceInfo;
            //    labelLogin.BeginInvoke(new UpdateTextStatusCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(strLoginCallBack, lpDeviceInfo);
            //}

        }

        /// <summary>
        /// 红外图像预览线程执行方法
        /// </summary>
        private void Prewview()
        {
            //利用VideoWriter对象 录制视频  红外视频帧频25HZ，分辨率768*576      
            VideoWriter writer = new VideoWriter("output_video.avi", FourCC.XVID, 25, new OpenCvSharp.Size(768, 576), true);
            int i = 0;
            int j = 0;
            while (true)
            {
                try
                {
                    if (irImageList.Count > 0)
                    {
                        Bitmap bitmap = (Bitmap)irImageList[0].Clone();

                        if (bitmap != null)
                        {
                            using (Graphics gfx = Graphics.FromImage(bitmap))
                            {

                                Font font = new Font("Arial", 14);
                                //SolidBrush solidBrush = new SolidBrush(Color.FromArgb(205,51,51)) ;
                                Brush brush = Brushes.LightGreen;
                                Pen pen = new Pen(Color.Red, 2);

                                string maxTemp;
                                PointF point;
                                float pt = 2.0f; //显示水平缩放比例温度数据是384 * 288，视频图像768 * 576      

                                maxTemp = ((float)globa_Temp.max_temp / 10).ToString("F1");//全局最高温度，保留一位小数
                                float max = float.Parse(maxTemp);

                                //图像最高温度大于设定的高温报警阈值，保存图像
                                if (max >= Globals.systemParam.alarm_1)
                                {
                                    if (i == 0)
                                    {
                                        saveAlarmImageFlag = true;                                       
                                        i = 1;                                      
                                    }

                                }
                                else//温度低于报警阈值时，关闭保存图像定时器
                                {
                                    i = 0;
                                    j = 0;

                                    this.Invoke((MethodInvoker)delegate
                                    {                                      
                                        timer3.Enabled = false;
                                        timer3.Stop();
                                    });

                                    saveAlarmImageFlag = false;

                                }

                                float maxTempX = globa_Temp.max_temp_x * pt;
                                float maxTempY = globa_Temp.max_temp_y * pt;

                                //获取最大值字符串在屏幕上显示的尺寸
                                SizeF maxTempStringSize = gfx.MeasureString(maxTemp, font);

                                //超出边界，调整显示位置
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

                                if (saveImageFlag)
                                {
                                    string IrImagePath = GetIrImageFilePath(Globals.ImageDirectoryPath,0);
                                    bitmap.Save(IrImagePath, System.Drawing.Imaging.ImageFormat.Bmp);
                                    saveImageFlag = false;
                                }

                                //保存报警图像
                                if (saveAlarmImageFlag)
                                {
                                    string IrImagePath = GetIrImageFilePath(Globals.AlarmImageDirectoryPath, 0);
                                    bitmap.Save(IrImagePath, System.Drawing.Imaging.ImageFormat.Bmp);

                                    if(j == 0)
                                    {
                                        SaveOpImage(Globals.AlarmImageDirectoryPath,m_lRealHandle, 1);
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            //开启定时器，定时保存图像
                                            timer3.Enabled = true;
                                            timer3.Start();
                                        });
                                        
                                        j = 1;
                                    }

                                    saveAlarmImageFlag = false;                                

                                }
                                pics[0].Image = bitmap;
                                //    // 保存Bitmap到文件
                                //    //bmp.Save(@"C:\Users\Dell\Desktop\1.png", System.Drawing.Imaging.ImageFormat.Png);

                                //    if (saveVideoFlag)
                                //    {
                                //        Console.WriteLine(writer.IsOpened());
                                //        Mat mat = Bitmap2Mat(bmp);
                                //        writer.Write(mat);
                                //    }

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

        private string GetIrImageFilePath(string rootPath,int deviceNum)
        {
            string imagePath = rootPath + deviceNum;

            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }

            string strTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            string IrImagePath = imagePath + "\\" + strTime + "_IR.bmp";

            return IrImagePath;
        }

        private void initDatas()
        {
            int deviceNum = Globals.systemParam.deviceNum;
            pics = new PictureBox[deviceNum * 2];//定义显示图像控件，每个设备显示可见光和红外图像。

        }


        /// <summary>
        /// 设置实时监控界面图像显示控件布局
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

        public SolidBrush SolidBrush { get; private set; }


        /// <summary>
        /// 解码线程执行方法
        /// </summary>
        [Obsolete]
        private unsafe void DeCoding()
        {
            try
            {
                Console.WriteLine("DeCoding run...");
                Bitmap oldBmp = null;


                // 更新图片显示
                tstRtmp.ShowBitmap show = (bmp) =>
                {
                    lock (obj0)
                    {
                        irImageList.Add(bmp);

                        if (irImageList.Count > 3)
                        {
                            irImageList.RemoveAt(0);
                        }
                    }
                };
                rtmp.Start(show, "rtsp://192.168.1.80/webcam");
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
                    //saveVideoFlag = true;

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
                //saveVideoFlag = false;
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

        /// <summary>
        /// 定时获取红外图像的全局温度信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer1_Tick(object sender, EventArgs e)
        {
            a8Lists[0].Get_globa_temp(out globa_Temp);//获取全局温度信息

        }

        /// <summary>
        /// 设备校时
        /// </summary>
        private void Timing()
        {
            A8SDK.time_param time_Param = new A8SDK.time_param();

            time_Param.year = DateTime.Now.Year;
            time_Param.month = (char)DateTime.Now.Month;
            time_Param.day = (char)DateTime.Now.Day;

            time_Param.hour = (char)DateTime.Now.Hour;
            time_Param.minute = (char)DateTime.Now.Minute;
            time_Param.second = (char)DateTime.Now.Second;

            a8Lists[0].Set_time(time_Param);

            m_struTimeCfg.dwYear = DateTime.Now.Year; ;
            m_struTimeCfg.dwMonth = DateTime.Now.Month;
            m_struTimeCfg.dwDay = DateTime.Now.Day;
            m_struTimeCfg.dwHour = DateTime.Now.Hour;
            m_struTimeCfg.dwMinute = DateTime.Now.Minute;
            m_struTimeCfg.dwSecond = DateTime.Now.Second;

            Int32 nSize = Marshal.SizeOf(m_struTimeCfg);
            IntPtr ptrTimeCfg = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(m_struTimeCfg, ptrTimeCfg, false);

            if (!CHCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, CHCNetSDK.NET_DVR_SET_TIMECFG, -1, ptrTimeCfg, (UInt32)nSize))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "NET_DVR_SET_TIMECFG failed, error code= " + iLastErr;
                //设置时间失败，输出错误号 Failed to set the time of device and output the error code
                MessageBox.Show(str);
            }
            else
            {
                //MessageBox.Show("校时成功！");
            }

            Marshal.FreeHGlobal(ptrTimeCfg);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer2_Tick(object sender, EventArgs e)
        {
            A8SDK.time_param time_Param = new A8SDK.time_param();

            time_Param.year = DateTime.Now.Year;
            time_Param.month = (char)DateTime.Now.Month;
            time_Param.day = (char)DateTime.Now.Day;

            time_Param.hour = (char)DateTime.Now.Hour;
            time_Param.minute = (char)DateTime.Now.Minute;
            time_Param.second = (char)DateTime.Now.Second;

            a8Lists[0].Set_time(time_Param);

            m_struTimeCfg.dwYear = DateTime.Now.Year; ;
            m_struTimeCfg.dwMonth = DateTime.Now.Month;
            m_struTimeCfg.dwDay = DateTime.Now.Day;
            m_struTimeCfg.dwHour = DateTime.Now.Hour;
            m_struTimeCfg.dwMinute = DateTime.Now.Minute;
            m_struTimeCfg.dwSecond = DateTime.Now.Second;

            Int32 nSize = Marshal.SizeOf(m_struTimeCfg);
            IntPtr ptrTimeCfg = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(m_struTimeCfg, ptrTimeCfg, false);

            if (!CHCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, CHCNetSDK.NET_DVR_SET_TIMECFG, -1, ptrTimeCfg, (UInt32)nSize))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "NET_DVR_SET_TIMECFG failed, error code= " + iLastErr;
                //设置时间失败，输出错误号 Failed to set the time of device and output the error code
                MessageBox.Show(str);
            }
            else
            {
                //MessageBox.Show("校时成功！");
            }

            Marshal.FreeHGlobal(ptrTimeCfg);
        }

        private void SaveOpImage(string rootPath,int /*userID*/handle, int channel)
        {
            string imagePath = rootPath + 0;

            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }
            //if (!Directory.Exists(alarmImagePath))
            //{
            //    Directory.CreateDirectory(alarmImagePath);
            //}

            string strTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            string opImagePath = imagePath + "\\" + strTime + "_OP.bmp";


            //CHCNetSDK.NET_DVR_JPEGPARA lpJpegPara = new CHCNetSDK.NET_DVR_JPEGPARA();
            //lpJpegPara.wPicQuality = 0; //图像质量 Image quality
            //lpJpegPara.wPicSize = 0xff; //抓图分辨率 Picture size: 2- 4CIF，0xff- Auto(使用当前码流分辨率)，抓图分辨率需要设备支持，更多取值请参考SDK文档

            //JPEG抓图 Capture a JPEG picture
            if (!CHCNetSDK.NET_DVR_CapturePicture(handle, opImagePath))
            //if (!CHCNetSDK.NET_DVR_CaptureJPEGPicture(userID, channel, ref lpJpegPara, opImagePath))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "NET_DVR_CaptureJPEGPicture failed, error code= " + iLastErr;
                MessageBox.Show(str);
                return;
            }
            else
            {
                //str = "Successful to capture the JPEG file and the saved file is " + opImagePath;
                //MessageBox.Show(str);
            }
            return;
        }

        private void UiButton1_Click_1(object sender, EventArgs e)
        {

            saveImageFlag = true;
            SaveOpImage(Globals.ImageDirectoryPath, m_lRealHandle, 1);
            //saveImageFlag1 = true;
            // SaveOpImage(m_lRealHandle, 1);

        }

        private void Timer3_Tick(object sender, EventArgs e)
        {
            saveAlarmImageFlag = true;
            SaveOpImage(Globals.AlarmImageDirectoryPath,m_lRealHandle,1);            
        }
    }
}
