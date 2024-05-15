using System;
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
using a8sdk;
using OpenCvSharp;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace A8_TEST
{
    public partial class Form1 : UIForm
    {

        const uint DISPLAYWND_GAP = 1;//监控画面的间隙
        const uint DISPLAYWND_MARGIN_LEFT = 1;//监控画面距离左边控件的距离
        const uint DISPLAYWND_MARGIN_TOP = 1; //监控画面距离上边的距离
        const int PAGE_INDEX = 1000;
        Color PIC_CLICKED_COLOR = Color.FromArgb(128, 128, 255);
        Color PIC_UNCLICKED_COLOR = Color.FromArgb(45, 45, 53);
        private PictureBox[] pics;//显示图像控件
        private UIPage fmonitor;//监控界面
        private UIPage fbrowse;//浏览界面
                               // private UIPanel pixUIPanel;//容纳PictureBox的Panel
                               //public static TransparentLabel[] labels;//图像上面标注控件,背景透明

        UISymbolButton startPrewviewBtn, stopPrewviewBtn, startRecordBtn, stopRecordBtn, mouseFollowBtn;
        private bool isStartPrewview = false;//开始采集标志
        List<Socket> sockets = new List<Socket>();//连接红外相机socket
        private delegate string ConnectSocketDelegate(IPEndPoint ipep, Socket sock);
        List<Thread> threadsReceiveTmp = new List<Thread>();//接收温度数据线程
        //private int[,] realTemp;
        private List<int[,]> realTemps = new List<int[,]>();//存储温度数据
        //接收数据缓存
        private List<byte[]> dataBuffers = new List<byte[]>();
        //从dataBuffer已经存了多少个字节数据
        private List<int> contentSizes = new List<int>();
        private List<bool> receiveFlags = new List<bool>();
        private List<bool> socketReceiveFlags = new List<bool>();

        private bool saveVideoFlag = false;//录制视频标志
        private bool mouseFollowFlag = false;//鼠标跟随标志

        List<int> picMouseX = new List<int>();//鼠标跟随x坐标
        List<int> picMouseY = new List<int>();//鼠标跟随y坐标
        List<float> tempMouseX = new List<float>();//鼠标跟随对应温度数组x坐标
        List<float> tempMouseY = new List<float>();//鼠标跟随对应温度数组y坐标

        string recordName;//录制视频文件名
        VideoWriter writer;//存储视频对象
        private bool isInPic;//判断鼠标是否在图像内的标志

        #region 红外
        tstRtmp rtmp = new tstRtmp();//利用ffmpeg获取视频数据
        Rtsplz rtsplz = new Rtsplz();
        Thread thPlayer;//解码红外视频线程
        Thread thPlayerlz;
        SynchronizationContext m_SyncContext = null; //获取上下文
        List<string> ipLists = new List<string>(); //设备ip集合
        List<A8SDK> a8Lists = new List<A8SDK>(); //红外设备A8SDK对象集合
        List<A8SDK.globa_temp> globa_Temps = new List<A8SDK.globa_temp>();
        A8SDK.globa_temp globa_Temp;//全局温度结构对象
        //List<Bitmap> irImageList = new List<Bitmap>();//存储红外图像集合
        List<Bitmap>[] irImageLists;//存储红外图像集合
        List<string> ipList = new List<string>();//红外设备ip集合
        List<bool> isShowIRImageFlags = new List<bool>();//显示红外图像标志
        float pt = 2.0f; //显示水平缩放比例温度数据是384 * 288，视频图像768 * 576   

        #endregion

        #region 可见光
        List<int> mUserIDs = new List<int>();
        List<int> mRealHandles = new List<int>();
        List<CHCNetSDK.LOGINRESULTCALLBACK> LoginCallBacks = new List<CHCNetSDK.LOGINRESULTCALLBACK>();
        List<CHCNetSDK.REALDATACALLBACK> RealDatas = new List<CHCNetSDK.REALDATACALLBACK>();
        List<CHCNetSDK.NET_DVR_DEVICEINFO_V40> DeviceInfos = new List<CHCNetSDK.NET_DVR_DEVICEINFO_V40>();

        private bool m_bInitSDK = false;
        private uint iLastErr = 0;

        private string str;
        public CHCNetSDK.NET_DVR_USER_LOGIN_INFO struLogInfo;
        public CHCNetSDK.NET_DVR_DEVICEINFO_V40 DeviceInfo;
        public CHCNetSDK.NET_DVR_TIME m_struTimeCfg;

        #endregion

        private bool saveImageFlag;//保存图像标志
        private bool saveAlarmImageFlag;//保存报警图像标志
        private bool alertFlag;//报警标志

        List<Object> objects = new List<object>();

        A8SDK a8;

        List<Bitmap> imageList = new List<Bitmap>();
        public Form1()
        {
            InitializeComponent();


            a8 = new A8SDK("192.168.100.2");

            this.TopMost = true;
            this.WindowState = FormWindowState.Maximized;

            Control.CheckForIllegalCrossThreadCalls = false;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.
            SetStyle(ControlStyles.DoubleBuffer, true); // 双缓冲

            //读取配置文件
            Globals.ReadInfoXml<SystemParam>(ref Globals.systemParam, Globals.systemXml);

            int pageIndex = PAGE_INDEX;

            //设置关联
            uiNavBar1.TabControl = uiTabControl1;

            //uiNavBar1设置节点，也可以在Nodes属性里配置
            uiNavBar1.Nodes.Add("在线监控");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[0], 61501);//设置图标

            //添加实时监控界面
            fmonitor = new FMonitor();
            AddPage(fmonitor, pageIndex);
            uiNavBar1.SetNodePageIndex(uiNavBar1.Nodes[0], pageIndex);//设置显示的初始界面为实时监控界面

            uiNavBar1.Nodes.Add("图像浏览");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[1], 61502);

            //添加图像浏览界面  PAGE_INDEX + 1
            pageIndex++;
            fbrowse = new FormBrowse();
            AddPage(fbrowse, pageIndex);
            uiNavBar1.SetNodePageIndex(uiNavBar1.Nodes[1], pageIndex);

            uiNavBar1.Nodes.Add("系统设置");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[2], 61459);


            //初始化数据
            initDatas();


            //初始化图像显示控件布局
            SetFmonitorDisplayWnds((uint)Globals.systemParam.deviceCount, 2);

            //登录光学相机
            LoginOpDevice(0, "192.168.100.222", "admin", "hik12345", "8000", cbLoginCallBack);

            //采集预览光学图像
            PreviewOpDevice(0, RealDataCallBack);

            Thread.Sleep(100);


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

            }

            //ParameterizedThreadStart thStart = new ParameterizedThreadStart(ShowIRImageThreadProc);//threadStart委托 
            Thread thread = new Thread(ShowIRImageThreadProc);
            //thread.Priority = ThreadPriority.Highest;
            thread.IsBackground = true; //关闭窗体继续执行
            thread.Start();

        }

        private void ShowIRImageThreadProc()
        {

            int i = 0;
            int j = 0;
            
            while (true)
            {
                try
                {
                    //红外图像集合长度不为0
                    if (imageList.Count > 0)
                    {

                        //this.Invoke(new MethodInvoker(() =>
                        //{
                        Bitmap bitmap = (Bitmap)imageList[0].Clone();

                        //Console.WriteLine("bitmap.Width" + bitmap.Width);

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

                                maxTemp = ((float)globa_Temp.max_temp / 10).ToString("F1");//全局最高温度，保留一位小数
                                float max = float.Parse(maxTemp);

                                //图像最高温度大于设定的高温报警阈值，保存图像
                                if (max >= Globals.systemParam.alarm_1)
                                {
                                    if (i == 0)
                                    {
                                        saveAlarmImageFlag = true;
                                        alertFlag = true;
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
                                //Console.WriteLine("maxTempX = " + globa_Temps[num].max_temp_x + "maxTempY" + globa_Temps[num].max_temp_y);
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

                                //图像上显示全局温度最大值
                                gfx.DrawString(maxTemp, font, brush, point);

                                //图像上绘制十字标记
                                DrawCrossLine(gfx, globa_Temp.max_temp_x * pt, globa_Temp.max_temp_y * pt, pen, 10);

                                if (saveImageFlag)
                                {
                                    string IrImagePath = GetIrImageFilePath(Globals.ImageDirectoryPath, 0);
                                    bitmap.Save(IrImagePath, System.Drawing.Imaging.ImageFormat.Bmp);
                                    saveImageFlag = false;
                                }


                                //保存报警图像
                                if (saveAlarmImageFlag)
                                {
                                    string IrImagePath = GetIrImageFilePath(Globals.AlarmImageDirectoryPath, 0);
                                    bitmap.Save(IrImagePath, System.Drawing.Imaging.ImageFormat.Bmp);

                                    if (j == 0)
                                    {
                                        SaveOpImage(0, Globals.AlarmImageDirectoryPath, mRealHandles[0], 1);
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

                                Bitmap b = (Bitmap)bitmap.Clone();
                                pics[0].Image = bitmap;


                                //鼠标跟随
                                //if (mouseFollowFlag)
                                //{
                                //    //鼠标在图像内
                                //    if (isInPic)
                                //    {
                                //        float tempX = picMouseX[num];
                                //        float tempY = picMouseY[num];
                                //        string temp = (realTemps[0][(int)tempMouseX[num], (int)tempMouseY[num]] * 1.0f / 10).ToString("F1");
                                //        SizeF tempStringSize = gfx.MeasureString(temp, font);

                                //        float locX = tempX * 1.0f / pics[num].Width * b.Width;
                                //        float locY = tempY * 1.0f / pics[num].Height * b.Height;

                                //        ////超出边界，调整显示位置
                                //        if (locX + tempStringSize.Width > b.Width)
                                //        {
                                //            locX = locX - tempStringSize.Width - 10;
                                //        }

                                //        if (locY + tempStringSize.Height > b.Height)
                                //        {
                                //            locY = locY - tempStringSize.Height - 10;
                                //        }

                                //        gfx.DrawString(temp, font, brush, locX + 10, locY + 10);
                                //    }
                                //}

                                //保存红外视频
                                //if (saveVideoFlag)
                                //{
                                //    try
                                //    {
                                //        Console.WriteLine(writer.IsOpened());
                                //        var mat = BitmapConverter.ToMat(b);
                                //        ///Mat mat = Bitmap2Mat(bitmap);
                                //        writer.Write(mat);
                                //    }
                                //    catch (Exception ex)
                                //    {
                                //        MessageBox.Show("录制视频失败" + ex.ToString());
                                //    }
                                //}

                                b = null;
                                bitmap = null;

                            }
                        }
                        //}));
                        //红外图像集合数量大于5，删除多余图像，防止阻塞
                        if (imageList.Count > 3)
                        {
                            imageList.RemoveRange(0, imageList.Count - 3);
                        }

                        //irImageLists[num].RemoveAt(0);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                Thread.Sleep(50);
            }
        }

        private void PreviewOpDevice(int deviceNum, CHCNetSDK.REALDATACALLBACK realCallback)
        {
            if (mUserIDs[deviceNum] < 0)
            {
                MessageBox.Show("请先登录光学相机" + deviceNum);
                return;
            }

            if (mRealHandles[deviceNum] < 0)
            {
                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();
                lpPreviewInfo.hPlayWnd = pics[deviceNum * 2 + 1].Handle;//预览窗口
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


                if (RealDatas[deviceNum] == null)
                {
                    RealDatas[deviceNum] = new CHCNetSDK.REALDATACALLBACK(realCallback);//预览实时流回调函数
                }

                IntPtr pUser = new IntPtr();//用户数据

                //打开预览 Start live view 
                mRealHandles[deviceNum] = CHCNetSDK.NET_DVR_RealPlay_V40(mUserIDs[deviceNum], ref lpPreviewInfo, null/*RealData*/, pUser);
                if (mRealHandles[deviceNum] < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "预览可见光相机" + deviceNum + "失败!" + "错误号：" + iLastErr; //预览失败，输出错误号
                    MessageBox.Show(str);
                    return;
                }
                else
                {

                }
            }
            //else
            //{
            //    //停止预览 Stop live view 
            //    if (!CHCNetSDK.NET_DVR_StopRealPlay(m_lRealHandle))
            //    {
            //        iLastErr = CHCNetSDK.NET_DVR_GetLastError();
            //        str = "NET_DVR_StopRealPlay failed, error code= " + iLastErr;
            //        MessageBox.Show(str);
            //        return;
            //    }
            //    m_lRealHandle = -1;


            //}
            return;

        }

        /// <summary>
        /// 可见光相机实时预览数据回调
        /// </summary>
        /// <param name="lRealHandle"></param>
        /// <param name="dwDataType"></param>
        /// <param name="pBuffer"></param>
        /// <param name="dwBufSize"></param>
        /// <param name="pUser"></param>
        public void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            if (dwBufSize > 0)
            {


                //byte[] sData = new byte[dwBufSize];
                //Marshal.Copy(pBuffer, sData, 0, (Int32)dwBufSize);


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

        /// <summary>
        /// 登录可见光相机
        /// </summary>
        /// <param name="deviceNum">设备号，从0开始</param>
        /// <param name="ipAddress">ip地址</param>
        /// <param name="userName">用户名</param>
        /// <param name="psw">密码</param>
        /// <param name="port">端口号</param>
        /// <param name="loginCallBack">登录回调函数</param>
        private void LoginOpDevice(int deviceNum, string ipAddress, string userName, string psw, string port, CHCNetSDK.LOGINRESULTCALLBACK loginCallBack)
        {
            //初始化光学相机
            m_bInitSDK = CHCNetSDK.NET_DVR_Init();
            if (m_bInitSDK == false)
            {
                MessageBox.Show("光学设备" + deviceNum + "初始化失败");
                return;
            }
            else
            {
                //保存SDK日志 To save the SDK log
                CHCNetSDK.NET_DVR_SetLogToFile(3, "C:\\SdkLog\\", true);
            }

            if (mUserIDs[deviceNum] < 0)
            {

                struLogInfo = new CHCNetSDK.NET_DVR_USER_LOGIN_INFO();

                //设备IP地址或者域名
                byte[] byIP = System.Text.Encoding.Default.GetBytes(ipAddress);
                struLogInfo.sDeviceAddress = new byte[129];
                byIP.CopyTo(struLogInfo.sDeviceAddress, 0);

                //设备用户名
                byte[] byUserName = System.Text.Encoding.Default.GetBytes(userName);
                struLogInfo.sUserName = new byte[64];
                byUserName.CopyTo(struLogInfo.sUserName, 0);

                //设备密码
                byte[] byPassword = System.Text.Encoding.Default.GetBytes(psw);
                struLogInfo.sPassword = new byte[64];
                byPassword.CopyTo(struLogInfo.sPassword, 0);

                struLogInfo.wPort = ushort.Parse(port);//设备服务端口号

                if (LoginCallBacks[deviceNum] == null)
                {
                    LoginCallBacks[deviceNum] = new CHCNetSDK.LOGINRESULTCALLBACK(loginCallBack);//注册回调函数                    
                }
                struLogInfo.cbLoginResult = LoginCallBacks[deviceNum];
                struLogInfo.bUseAsynLogin = false; //是否异步登录：0- 否，1- 是 

                DeviceInfo = DeviceInfos[deviceNum];

                //登录设备 Login the device
                mUserIDs[deviceNum] = CHCNetSDK.NET_DVR_Login_V40(ref struLogInfo, ref DeviceInfo);
                if (mUserIDs[deviceNum] < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "登录可见光相机" + deviceNum + "失败！" + "错误号：" + iLastErr; //登录失败，输出错误号
                    MessageBox.Show(str);
                    return;
                }
                else
                {
                    //登录成功
                    //MessageBox.Show("Login Success!");
                }

            }
            //else
            //{
            //    //注销登录 Logout the device
            //    if (m_lRealHandle >= 0)
            //    {
            //        MessageBox.Show("Please stop live view firstly");
            //        return;
            //    }

            //    if (!CHCNetSDK.NET_DVR_Logout(userId))
            //    {
            //        iLastErr = CHCNetSDK.NET_DVR_GetLastError();
            //        str = "NET_DVR_Logout failed, error code= " + iLastErr;
            //        MessageBox.Show(str);
            //        return;
            //    }
            //    mUserIDs[deviceNum] = -1;

            //}
        }
        /// <summary>
        /// 登录回调函数
        /// </summary>
        /// <param name="lUserID"></param>
        /// <param name="dwResult"></param>
        /// <param name="lpDeviceInfo"></param>
        /// <param name="pUser"></param>
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

        private void Form1_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
 
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            a8.Get_globa_temp(out globa_Temp);//获取全局温度信息
        }

        private void Timer3_Tick(object sender, EventArgs e)
        {
            saveAlarmImageFlag = true;
            SaveOpImage(0, Globals.AlarmImageDirectoryPath, mRealHandles[0], 1);
        }

        private void SaveOpImage(int deviceNum, string rootPath, int /*userID*/handle, int channel)
        {
            string imagePath = rootPath + 0;

            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }
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
                str = "可见光设备" + deviceNum + "失败" + "错误码：" + iLastErr;
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

                //int i = 0;
                //int j = 0;
                // 更新图片显示
                tstRtmp.ShowBitmap show = (bmp) =>
                {
                    imageList.Add((Bitmap)bmp.Clone());
                    //this.Invoke(new MethodInvoker(() =>
                    //{
                    //    using (Graphics gfx = Graphics.FromImage(bmp))
                    //    {

                    //        if (bmp != null)
                    //        {

                    //            // 设置文字的格式
                    //            Font font = new Font("Arial", 12);
                    //            Brush brush = Brushes.Red;
                    //            Pen pen = new Pen(Color.Red, 2);

                    //            string maxTemp;
                    //            PointF point;
                    //            float pt = 2.0f; //显示水平缩放比例温度数据是384 * 288，视频图像768 * 576      

                    //            maxTemp = ((float)globa_Temp.max_temp / 10).ToString("F1");//全局最高温度
                    //            float max = float.Parse(maxTemp);


                    //            //图像最高温度大于设定的高温报警阈值，保存图像
                    //            if (max >= Globals.systemParam.alarm_1)
                    //            {
                    //                if (i == 0)
                    //                {
                    //                    saveAlarmImageFlag = true;
                    //                    alertFlag = true;
                    //                    i = 1;
                    //                }

                    //            }
                    //            else//温度低于报警阈值时，关闭保存图像定时器
                    //            {
                    //                i = 0;
                    //                j = 0;

                    //                this.Invoke((MethodInvoker)delegate
                    //                {
                    //                    //timer3.Enabled = false;
                    //                    //timer3.Stop();
                    //                });

                    //                saveAlarmImageFlag = false;

                    //            }


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

                    //            //保存报警图像
     


                    //        }

                    //    }

                    //    pics[0].Image = bmp;

                    //    //if (saveAlarmImageFlag)
                    //    //{
                    //    //    string IrImagePath = GetIrImageFilePath(Globals.AlarmImageDirectoryPath, 0);
                    //    //    bmp.Save(IrImagePath, System.Drawing.Imaging.ImageFormat.Bmp);

                    //    //    if (j == 0)
                    //    //    {
                    //    //        SaveOpImage(0, Globals.AlarmImageDirectoryPath, mRealHandles[0], 1);
                    //    //        this.Invoke((MethodInvoker)delegate
                    //    //        {
                    //    //            //开启定时器，定时保存图像
                    //    //            timer3.Enabled = true;
                    //    //            timer3.Start();
                    //    //        });

                    //    //        j = 1;
                    //    //    }

                    //    //    saveAlarmImageFlag = false;

                    //    //}
                    //    if (oldBmp != null)
                    //    {
                    //        oldBmp.Dispose();
                    //    }
                    //    oldBmp = bmp;
                    //}));
                };
                rtmp.Start(show, "rtsp://192.168.100.2/webcam");
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
                    //button_play.Text = "停止播放";
                    //button_play.Enabled = true;
                }));
            }
        }

        /// <summary>
        /// 获取红外图像文件路径
        /// </summary>
        /// <param name="rootPath">根路径</param>
        /// <param name="deviceNum">设备号，从0开始</param>
        /// <returns></returns>
        private string GetIrImageFilePath(string rootPath, int deviceNum)
        {
            string imagePath = rootPath + deviceNum;

            //判断文件夹是否存在，如果不存在，新建文件夹
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }

            string strTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            string IrImagePath = imagePath + "\\" + strTime + "_IR.bmp";

            return IrImagePath;
        }




        private void DrawCrossLine(Graphics g, float startX, float startY, Pen pen, int lineLength)
        {
            g.DrawLine(pen, startX, startY, startX + lineLength, startY);
            g.DrawLine(pen, startX, startY, startX - lineLength, startY);
            g.DrawLine(pen, startX, startY, startX, startY + lineLength);
            g.DrawLine(pen, startX, startY, startX, startY - lineLength);
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void initDatas()
        {
            int deviceCount = Globals.systemParam.deviceCount; //通过配置文件获取设备数量

            pics = new PictureBox[deviceCount * 2];//定义显示图像控件，每个设备显示可见光和红外图像。

            irImageLists = new List<Bitmap>[deviceCount];

            //初始化红外设备ip集合
            ipList.Add(Globals.systemParam.ir_ip_1);
            ipList.Add(Globals.systemParam.ir_ip_2);


            for (int i = 0; i < deviceCount; i++)
            {
                //初始化userId为-1
                mUserIDs.Add(-1);

                irImageLists[i] = new List<Bitmap>();
                //初始化红外设备对象，并添加到集合
                A8SDK a8 = new A8SDK(ipList[i]);
                a8Lists.Add(a8);

                DeviceInfos.Add(new CHCNetSDK.NET_DVR_DEVICEINFO_V40());
                mRealHandles.Add(-1);

                objects.Add(new object());

                globa_Temps.Add(new A8SDK.globa_temp());

                LoginCallBacks.Add(null);

                RealDatas.Add(null);

                isShowIRImageFlags.Add(false);

                sockets.Add(null);

                dataBuffers.Add(new byte[1024 * 1024 * 2]);

                contentSizes.Add(0);

                realTemps.Add(null);

                threadsReceiveTmp.Add(null);

                receiveFlags.Add(false);

                socketReceiveFlags.Add(false);

                picMouseX.Add(0);

                picMouseY.Add(0);

                tempMouseX.Add(0.0f);
                tempMouseY.Add(0.0f);

            }

            DirectoryInfo dirInfo = new DirectoryInfo(Globals.AlarmImageDirectoryPath + 0);
            Globals.fileInfos = dirInfo.GetFiles("*.bmp");
            Globals.SortFolderByCreateTime(ref Globals.fileInfos);
            //Console.WriteLine("FormBrowse_Load" + Globals.fileInfos[0].Name);

        }



        /// <summary>
        /// 设置实时监控界面图像显示控件布局
        /// </summary>
        /// <param name="rowNum">行数</param>
        /// <param name="colNum">列数</param>

        private void SetFmonitorDisplayWnds(uint rowNum, uint colNum)
        {
            //uint w = (uint)(Screen.PrimaryScreen.Bounds.Width - fmonitor.GetControl("uiPanel1").Width);
            //uint w = (uint)(Screen.PrimaryScreen.Bounds.Width - fmonitor.GetControl("uiNavMenu1").Width);
            uint w = (uint)(Screen.PrimaryScreen.Bounds.Width);
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
                    //uint x = (uint)fmonitor.GetControl("uiNavMenu1").Width + (real_width - colNum * display_width - DISPLAYWND_GAP * (colNum - 1)) / 2 + (display_width + DISPLAYWND_GAP) * j;
                    uint x = (real_width - colNum * display_width - DISPLAYWND_GAP * (colNum - 1)) / 2 + (display_width + DISPLAYWND_GAP) * j;

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

                    switch (i * 2 + j)
                    {
                        case 0:
                            //pics[i * 2 + j].Tag = 0;
                            //pics[i * 2 + j].Paint += new PaintEventHandler(Pics0_Paint);
                            //pics[i * 2 + j].Click += new EventHandler(Pics0_Click);

                            //pics[i * 2 + j].MouseMove += new MouseEventHandler(Pics0_MouseMove);
                            //pics[i * 2 + j].MouseLeave += new EventHandler(Pics0_MouseLeave);
                            //pics[i * 2 + j].MouseHover += new EventHandler(Pics0_MouseHover);
                            break;
                        case 1:
                            //pics[i * 2 + j].Tag = 0;
                            //pics[i * 2 + j].Paint += new PaintEventHandler(Pics1_Paint);
                            //pics[i * 2 + j].Click += new EventHandler(Pics1_Click);
                            break;
                        case 2:
                            //pics[i * 2 + j].Tag = 0;
                            //pics[i * 2 + j].Paint += new PaintEventHandler(Pics2_Paint);
                            //pics[i * 2 + j].Click += new EventHandler(Pics2_Click);
                            break;
                        case 3:
                            //pics[i * 2 + j].Tag = 0;
                            //pics[i * 2 + j].Paint += new PaintEventHandler(Pics3_Paint);
                            //pics[i * 2 + j].Click += new EventHandler(Pics3_Click);
                            break;

                    }

                }

            }

            //foreach (PictureBox p in fmonitor.GetControls<PictureBox>())
            //{
            //    Console.WriteLine(p.Name);
            //}
        }
    }
}
