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
using System.Net.Sockets;
using System.Net;

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

        List<Object> objects = new List<object>();

        [Obsolete]
        public FormMain()
        {

            InitializeComponent();

            this.TopMost = true;
            this.WindowState = FormWindowState.Maximized;

            m_SyncContext = SynchronizationContext.Current;
            Control.CheckForIllegalCrossThreadCalls = false;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.
            SetStyle(ControlStyles.DoubleBuffer, true); // 双缓冲

            //读取配置文件
            Globals.ReadInfoXml<SystemParam>(ref Globals.systemParam, Globals.systemXml);


            ////查找红外设备ip地址
            //ipLists = FindDeviceIpAddress();

            //if (ipLists.Count > 0)
            //{
            //    for (int i = 0; i < ipLists.Count; i++)
            //    {
            //        //初始化红外设备对象，并添加到集合
            //        A8SDK a8 = new A8SDK(ipLists[i]);
            //        a8Lists.Add(a8);
            //    }
            //}


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

            //设置默认显示界面
            // uiNavBar1.SelectedIndex = 0;


            //初始化数据
            initDatas();

            //初始化图像显示控件布局
            SetFmonitorDisplayWnds((uint)Globals.systemParam.deviceCount, 2);

            //LoginOpDevice(0, Globals.systemParam.op_ip_1, Globals.systemParam.op_username_1, Globals.systemParam.op_psw_1, Globals.systemParam.op_port_1, cbLoginCallBack);
            //PreviewOpDevice_1(0, RealDataCallBack);
            ////开启解码红外视频线程
            //DecodingIRImage(0);
            //////开启红外图像预览线程
            //PreviewIRImage(0);

            //获取Fmonitor界面开始采集按钮，并添加相关事件
            startPrewviewBtn = (UISymbolButton)fmonitor.GetControl("startPrewviewBtn");
            startPrewviewBtn.Click += new EventHandler(StartPrewviewBtn_Click);
            startPrewviewBtn.MouseHover += new EventHandler(StartPrewviewBtn_MouseHover);
            startPrewviewBtn.MouseLeave += new EventHandler(StartPrewviewBtn_MouseLeave);

            //获取Fmonitor界面停止按钮，并添加相关事件
            stopPrewviewBtn = (UISymbolButton)fmonitor.GetControl("stopPrewviewBtn");
            stopPrewviewBtn.Click += new EventHandler(StopPrewviewBtn_Click);
            stopPrewviewBtn.MouseHover += new EventHandler(StopPrewviewBtn_MouseHover);
            stopPrewviewBtn.MouseLeave += new EventHandler(StopPrewviewBtn_MouseLeave);

            //获取Fmonitor界面开始录制视频按钮，并添加相关事件
            startRecordBtn = (UISymbolButton)fmonitor.GetControl("startRecordBtn");
            startRecordBtn.Click += new EventHandler(StartRecordBtn_Click);
            startRecordBtn.MouseHover += new EventHandler(StartRecordBtn_MouseHover);
            startRecordBtn.MouseLeave += new EventHandler(StartRecordBtn_MouseLeave);

            //获取Fmoitor界面停止录制视频按钮，并添加相关事件
            stopRecordBtn = (UISymbolButton)fmonitor.GetControl("stopRecordBtn");
            stopRecordBtn.Click += new EventHandler(StopRecordBtn_Click);
            stopRecordBtn.MouseHover += new EventHandler(stopRecordBtn_MouseHover);
            stopRecordBtn.MouseLeave += new EventHandler(stopRecordBtn_MouseLeave);

            //获取Fmoitor界面鼠标跟随按钮，并添加相关事件
            mouseFollowBtn = (UISymbolButton)fmonitor.GetControl("mouseFollowBtn");
            mouseFollowBtn.Click += new EventHandler(mouseFollowBtn_Click);
            mouseFollowBtn.MouseHover += new EventHandler(mouseFollowBtn_MouseHover);
            mouseFollowBtn.MouseLeave += new EventHandler(mouseFollowBtn_MouseLeave);

            //为按钮添加提示信息
            uiToolTip1.SetToolTip(startPrewviewBtn, "开始采集");
            uiToolTip1.SetToolTip(stopPrewviewBtn, "停止采集");
            uiToolTip1.SetToolTip(startRecordBtn, "开始录制");
            uiToolTip1.SetToolTip(stopRecordBtn, "停止录制");
            uiToolTip1.SetToolTip(mouseFollowBtn, "鼠标跟随");

        }

        /// <summary>
        /// 设置按钮图片
        /// </summary>
        /// <param name="btn"></param>
        /// <param name="imageName"></param>
        private void SetButtonImg(UISymbolButton btn, string imageName)
        {
            btn.Image = Image.FromFile(Globals.startPathInfo.Parent.Parent.FullName + "\\Resources\\" + imageName);
        }

        /// <summary>
        ///  鼠标跟随按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseFollowBtn_MouseLeave(object sender, EventArgs e)
        {

            if (!mouseFollowFlag)
            {
                SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            }
        }

        /// <summary>
        /// 鼠标跟随按钮鼠标在控件内事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseFollowBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(mouseFollowBtn, "鼠标跟随1.png");
        }

        /// <summary>
        /// 鼠标跟随按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseFollowBtn_Click(object sender, EventArgs e)
        {
            //没有开始采集，返回
            if (!isStartPrewview)
            {
                return;
            }

            if (!mouseFollowFlag)
            {
                mouseFollowFlag = true;
                SetButtonImg(mouseFollowBtn, "鼠标跟随1.png");
            }
            else
            {
                mouseFollowFlag = false;
                SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            }
        }

        /// <summary>
        /// 停止录制视频按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stopRecordBtn_MouseLeave(object sender, EventArgs e)
        {
            SetButtonImg(stopRecordBtn, "停止录制.png");
        }

        /// <summary>
        ///  鼠标跟随按钮停止录制视频按钮鼠标在控件内事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stopRecordBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(stopRecordBtn, "停止录制1.png");
        }

        /// <summary>
        ///  开始录制视频按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartRecordBtn_MouseLeave(object sender, EventArgs e)
        {
            if (!saveVideoFlag)
            {
                SetButtonImg(startRecordBtn, "开始录制-line.png");
            }
        }

        /// <summary>
        ///  开始录制视频鼠标在控件内事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartRecordBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(startRecordBtn, "开始录制-line(1).png");
        }

        /// <summary>
        /// 停止录制视频按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopRecordBtn_Click(object sender, EventArgs e)
        {
            if (saveVideoFlag)
            {
                saveVideoFlag = false;
                SetButtonImg(startRecordBtn, "开始录制-line.png");
            }
        }

        /// <summary>
        /// 开始录制视频按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartRecordBtn_Click(object sender, EventArgs e)
        {
            if (!isStartPrewview)
            {
                return;
            }
            if (!saveVideoFlag)
            {

                string filePath = saveFilePath();
                if (filePath == "")
                {
                    return;
                }
                recordName = filePath + ".avi";
                //利用VideoWriter对象 录制视频  红外视频帧频25HZ，分辨率768*576      
                writer = new VideoWriter(recordName, FourCC.MJPG, 20, new OpenCvSharp.Size(768, 576), true);
                saveVideoFlag = true;
            }
            SetButtonImg(startRecordBtn, "开始录制-line(1).png");
        }

        /// <summary>
        /// 停止采集按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopPrewviewBtn_MouseLeave(object sender, EventArgs e)
        {
            SetButtonImg(stopPrewviewBtn, "stop.png");            
        }

        /// <summary>
        /// 停止采集按钮鼠标在控件内事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopPrewviewBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(stopPrewviewBtn, "stopPressed.png");           
        }

        /// <summary>
        /// 开始采集按钮鼠标在控件内事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartPrewviewBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(startPrewviewBtn, "开始(1).png");            
        }

        /// <summary>
        /// 开始采集按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartPrewviewBtn_MouseLeave(object sender, EventArgs e)
        {
            if (!isStartPrewview)
            {
                SetButtonImg(startPrewviewBtn, "开始 .png");
            }
        }

        /// <summary>
        /// 开始采集预览图像
        /// </summary>
        private void StartPrewview()
        {
            //已经开始采集，返回
            if (isStartPrewview)
            {
                return;
            }

            ConnectSocketToReceiveTemp(0);

            isStartPrewview = true;//设置开始采集标志为true

            //登录光学相机
            LoginOpDevice(0, Globals.systemParam.op_ip_1, Globals.systemParam.op_username_1, Globals.systemParam.op_psw_1, Globals.systemParam.op_port_1, cbLoginCallBack);

            //采集预览光学图像
            PreviewOpDevice(0, RealDataCallBack);

            //开启解码红外视频线程
            DecodingIRImage(0);

            ////开启红外图像预览线程
            PreviewIRImage(0);

            SetButtonImg(startPrewviewBtn, "开始(1).png");          

            //设备校时
            Timing();
        }

        /// <summary>
        /// 停止采集预览
        /// </summary>
        private void StopPrewview()
        {
            isStartPrewview = false;//设置停止采集标志位false   
            saveVideoFlag = false;

            for (int i = 0; i < Globals.systemParam.deviceCount; i++)
            {
                isShowIRImageFlags[i] = false;//设置显示红外图像标志
                socketReceiveFlags[i] = false;

                //如果已经登录光学设备，登出，同时设置userId为-1
                if (mUserIDs[i] >= 0)
                {
                    CHCNetSDK.NET_DVR_Logout(mUserIDs[i]);
                    mUserIDs[i] = -1;

                }

                //如果正在预览光学图像，停止预览，并设置mRealHandles为-1
                if (mRealHandles[i] >= 0)
                {
                    CHCNetSDK.NET_DVR_StopRealPlay(mRealHandles[i]);
                    mRealHandles[i] = -1;
                }

                //图像预览控件清空，不显示图像
                pics[i * 2].Image = null;
                pics[i * 2 + 1].Image = null;
            }

            //停止保存报警图像定时器
            timer3.Enabled = false;

            //设置监控界面功能按钮的图像
            SetMonitorFucBtnImg("开始 .png", "stopPressed.png");
            SetButtonImg(startRecordBtn, "开始录制-line.png");        
        }

        /// <summary>
        /// 开始采集按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartPrewviewBtn_Click(object sender, EventArgs e)
        {
            StartPrewview();
        }

        /// <summary>
        /// 停止采集按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopPrewviewBtn_Click(object sender, EventArgs e)
        {
            if (isStartPrewview)
            {
                StopPrewview();
            }
        }

        /// <summary>
        /// 开启红外视频解码线程
        /// </summary>
        /// <param name="num">设备号，从0开始</param>
        private void DecodingIRImage(int num)
        {
            ParameterizedThreadStart thStart = new ParameterizedThreadStart(DeCoding);//threadStart委托 
            Thread thread = new Thread(thStart);
            //thread.Priority = ThreadPriority.Highest;
            thread.IsBackground = true; //关闭窗体继续执行
            thread.Start(num);
        }

        /// <summary>
        /// 开启预览红外图像线程
        /// </summary>
        /// <param name="num">设备号，从0开始</param>
        private void PreviewIRImage(int num)
        {
            ParameterizedThreadStart thStart = new ParameterizedThreadStart(ShowIRImageThreadProc);//threadStart委托 
            Thread thread = new Thread(thStart);
            //thread.Priority = ThreadPriority.Highest;
            thread.IsBackground = true; //关闭窗体继续执行
            thread.Start(num);
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


        /// <summary>
        /// 红外图像预览线程执行方法
        /// </summary>
        private void ShowIRImageThreadProc(object deviceNum)
        {

            int i = 0;
            int j = 0;
            int num = (int)deviceNum;
            while (isShowIRImageFlags[num])
            {
                try
                {
                    //红外图像集合长度不为0
                    if (irImageLists[num].Count > 0)
                    {
                        //this.Invoke(new MethodInvoker(() =>
                        //{
                        Bitmap bitmap = (Bitmap)irImageLists[num][0].Clone();
                        Console.WriteLine("bitmap.Width" + bitmap.Width);

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

                                maxTemp = ((float)globa_Temps[num].max_temp / 10).ToString("F1");//全局最高温度，保留一位小数
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
                                //Console.WriteLine("maxTempX = " + globa_Temps[num].max_temp_x + "maxTempY" + globa_Temps[num].max_temp_y);
                                float maxTempX = globa_Temps[num].max_temp_x * pt;
                                float maxTempY = globa_Temps[num].max_temp_y * pt;

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
                                DrawCrossLine(gfx, globa_Temps[num].max_temp_x * pt, globa_Temps[num].max_temp_y * pt, pen, 10);

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
                                        SaveOpImage(num, Globals.AlarmImageDirectoryPath, mRealHandles[num], 1);
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
                                pics[num].Image = bitmap;


                                //鼠标跟随
                                if (mouseFollowFlag)
                                {
                                    //鼠标在图像内
                                    if (isInPic)
                                    {
                                        float tempX = picMouseX[num];
                                        float tempY = picMouseY[num];
                                        string temp = (realTemps[0][(int)tempMouseX[num], (int)tempMouseY[num]] * 1.0f / 10).ToString("F1");
                                        SizeF tempStringSize = gfx.MeasureString(temp, font);

                                        float locX = tempX * 1.0f / pics[num].Width * b.Width;
                                        float locY = tempY * 1.0f / pics[num].Height * b.Height;

                                        ////超出边界，调整显示位置
                                        if (locX + tempStringSize.Width > b.Width)
                                        {
                                            locX = locX - tempStringSize.Width - 10;
                                        }

                                        if (locY + tempStringSize.Height > b.Height)
                                        {
                                            locY = locY - tempStringSize.Height - 10;
                                        }

                                        gfx.DrawString(temp, font, brush, locX + 10, locY + 10);
                                    }
                                }

                                //保存红外视频
                                if (saveVideoFlag)
                                {
                                    try
                                    {
                                        Console.WriteLine(writer.IsOpened());
                                        var mat = BitmapConverter.ToMat(b);
                                        ///Mat mat = Bitmap2Mat(bitmap);
                                        writer.Write(mat);
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show("录制视频失败" + ex.ToString());
                                    }
                                }

                                b = null;
                                bitmap = null;

                            }
                        }
                        //}));
                        //红外图像集合数量大于5，删除多余图像，防止阻塞
                        if (irImageLists[num].Count > 5)
                        {
                            irImageLists[num].RemoveRange(0, irImageLists[num].Count - 5);
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

                            pics[i * 2 + j].MouseMove += new MouseEventHandler(Pics0_MouseMove);
                            pics[i * 2 + j].MouseLeave += new EventHandler(Pics0_MouseLeave);
                            pics[i * 2 + j].MouseHover += new EventHandler(Pics0_MouseHover);
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

        private void Pics0_MouseLeave(object sender, EventArgs e)
        {
            //mouseFollowFlag = false;
            isInPic = false;
        }

        private void Pics0_MouseHover(object sender, EventArgs e)
        {
            //this.uiToolTip1.SetToolTip(sender as PictureBox, "23.5");
        }


        private void Pics0_MouseMove(object sender, MouseEventArgs e)
        {

            if (!isStartPrewview)
            {
                return;
            }

            isInPic = true;
            //Console.WriteLine("Pics0_MouseMove");

            PictureBox pic = sender as PictureBox;
            Console.WriteLine("pic.Width" + pic.Width);

            //// 获取鼠标在PictureBox内的位置
            ///

            picMouseX[0] = e.X;
            picMouseY[0] = e.Y;

            tempMouseX[0] = picMouseX[0] * 1.0f / (pics[0].Width) * 384;
            tempMouseY[0] = picMouseY[0] * 1.0f / (pics[0].Height) * 288;

            //// PointF pointF = new PointF(x,y);

            //string temp = (realTemps[0][(int)a, (int)b] * 1.0f / 10).ToString("F1");
            ////mousePoints.Add(pointF);

            ////Graphics g = pic.CreateGraphics();

            ////g.DrawEllipse(Pens.Blue, 10, 10, 100, 100); // 绘制蓝色椭圆
            ////g.Dispose();

            //Console.WriteLine("x=" + x);
            //Console.WriteLine("y= " + y);

            //// 显示坐标（可以用ToolTip或其他方式显示）
            //this.uiToolTip1.SetToolTip(pic, temp);



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
        private unsafe void DeCoding(object deviceNum)
        {
            int num = (int)deviceNum;
            isShowIRImageFlags[num] = true;
            try
            {
                //Console.WriteLine("DeCoding run...");
                //Bitmap oldBmp = null;

                // 更新图片显示
                tstRtmp.ShowBitmap show = (bmp) =>
                {
                    lock (objects[num])
                    {
                        irImageLists[num].Add((Bitmap)bmp.Clone());

                        //if (irImageLists[num].Count > 5)
                        //{
                        //    irImageLists[num].RemoveAt(0);
                        //}
                    }
                };
                //Console.WriteLine(ipList[0]);
                rtmp.Start(show, "rtsp://" + ipList[num] + "/webcam");
                //rtmp.Start(show, "rtsp://192.168.1.80/webcam");
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
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
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


        /// <summary>
        /// 定时获取红外图像的全局温度信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer1_Tick(object sender, EventArgs e)
        {
            //a8Lists[0].Get_globa_temp(out globa_Temp);//获取全局温度信息
            //globa_Temps[0] = globa_Temp;
            if (isStartPrewview)
            {
                for (int i = 0; i < Globals.systemParam.deviceCount; i++)
                {
                    a8Lists[i].Get_globa_temp(out globa_Temp);//获取全局温度信息
                    globa_Temps[i] = globa_Temp;
                }
            }
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

            if (!CHCNetSDK.NET_DVR_SetDVRConfig(mUserIDs[0], CHCNetSDK.NET_DVR_SET_TIMECFG, -1, ptrTimeCfg, (UInt32)nSize))
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
            //if (isStartPrewview)
            //{
            //    A8SDK.time_param time_Param = new A8SDK.time_param();

            //    time_Param.year = DateTime.Now.Year;
            //    time_Param.month = (char)DateTime.Now.Month;
            //    time_Param.day = (char)DateTime.Now.Day;

            //    time_Param.hour = (char)DateTime.Now.Hour;
            //    time_Param.minute = (char)DateTime.Now.Minute;
            //    time_Param.second = (char)DateTime.Now.Second;

            //    a8Lists[0].Set_time(time_Param);

            //    m_struTimeCfg.dwYear = DateTime.Now.Year; ;
            //    m_struTimeCfg.dwMonth = DateTime.Now.Month;
            //    m_struTimeCfg.dwDay = DateTime.Now.Day;
            //    m_struTimeCfg.dwHour = DateTime.Now.Hour;
            //    m_struTimeCfg.dwMinute = DateTime.Now.Minute;
            //    m_struTimeCfg.dwSecond = DateTime.Now.Second;

            //    Int32 nSize = Marshal.SizeOf(m_struTimeCfg);
            //    IntPtr ptrTimeCfg = Marshal.AllocHGlobal(nSize);
            //    Marshal.StructureToPtr(m_struTimeCfg, ptrTimeCfg, false);

            //    if (!CHCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, CHCNetSDK.NET_DVR_SET_TIMECFG, -1, ptrTimeCfg, (UInt32)nSize))
            //    {
            //        iLastErr = CHCNetSDK.NET_DVR_GetLastError();
            //        str = "NET_DVR_SET_TIMECFG failed, error code= " + iLastErr;
            //        //设置时间失败，输出错误号 Failed to set the time of device and output the error code
            //        MessageBox.Show(str);
            //    }
            //    else
            //    {
            //        //MessageBox.Show("校时成功！");
            //    }

            //    Marshal.FreeHGlobal(ptrTimeCfg);
            //}

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

        //private void UiButton1_Click_1(object sender, EventArgs e)
        //{

        //    saveImageFlag = true;
        //    SaveOpImage(Globals.ImageDirectoryPath, mRealHandles[0], 1);
        //    //saveImageFlag1 = true;
        //    // SaveOpImage(m_lRealHandle, 1);

        //}

        /// <summary>
        /// 保存可见光图像定时器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer3_Tick(object sender, EventArgs e)
        {
            saveAlarmImageFlag = true;
            SaveOpImage(0, Globals.AlarmImageDirectoryPath, mRealHandles[0], 1);
        }

        /// <summary>
        /// 窗体最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        /// <summary>
        /// 关闭窗体按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox2_Click(object sender, EventArgs e)
        {

            try
            {
                for (int i = 0; i < Globals.systemParam.deviceCount; i++)
                {
                    if (sockets[i] != null)
                    {
                        sockets[i].Close();
                    }

                    isShowIRImageFlags[i] = false;
                    socketReceiveFlags[i] = false;

                    if (mUserIDs[i] >= 0)
                    {
                        CHCNetSDK.NET_DVR_Logout(mUserIDs[i]);
                        mUserIDs[i] = -1;

                    }

                    if (mRealHandles[i] >= 0)
                    {
                        CHCNetSDK.NET_DVR_StopRealPlay(mRealHandles[i]);
                        mRealHandles[i] = -1;
                    }

                }
            }
            catch (Exception ex)
            {
                Globals.Log("关闭窗口" + ex.ToString());
            }

            Globals.fileInfos = null;
            this.Close();
            System.Environment.Exit(0);
        }

        private void UiNavBar1_MenuItemClick(string itemText, int menuIndex, int pageIndex)
        {
            Console.WriteLine("UiNavBar1_MenuItemClic" + pageIndex);
            Console.WriteLine("isStartPrewview" + isStartPrewview);
            if (pageIndex == PAGE_INDEX)
            {
                StartPrewview();
                SetMonitorFucBtnImg("开始(1).png", "stop.png");
                //startPrewviewBtn.Image = Image.FromFile(Globals.startPathInfo.Parent.Parent.FullName + "\\Resources\\" + "开始(1).png");
                //stopPrewviewBtn.Image = Image.FromFile(Globals.startPathInfo.Parent.Parent.FullName + "\\Resources\\" + "stop.png");

            }
            else
            {
                if (isStartPrewview)
                {
                    StopPrewview();
                }
            }

        }

        private void SetMonitorFucBtnImg(string startBtnImgName, string stopBtnImgName)
        {
            startPrewviewBtn.Image = Image.FromFile(Globals.startPathInfo.Parent.Parent.FullName + "\\Resources\\" + startBtnImgName);
            stopPrewviewBtn.Image = Image.FromFile(Globals.startPathInfo.Parent.Parent.FullName + "\\Resources\\" + stopBtnImgName);
        }


        private string ConnectSocket(IPEndPoint ipep, Socket sock)
        {
            string exmessage = "";
            try
            {
                sock.Connect(ipep);
            }
            catch (System.Exception ex)
            {
                exmessage = ex.Message;
            }
            finally
            {
            }

            return exmessage;
        }

        /// <summary>
        /// 基于TCP协议的原始温度数据
        /// </summary>
        /// <param name="deviceNum"></param>
        private void ConnectSocketToReceiveTemp(int deviceNum)
        {

            try
            {
                sockets[deviceNum] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Parse(ipList[deviceNum]);
                IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32("8081"));

                ConnectSocketDelegate connect = ConnectSocket;
                IAsyncResult asyncResult = connect.BeginInvoke(point, sockets[deviceNum], null, null);


                bool connectSuccess = asyncResult.AsyncWaitHandle.WaitOne(1000, false);
                if (!connectSuccess)
                {
                    Globals.Log("设备" + deviceNum + "连接超时");
                    return;
                }

                string exmessage = connect.EndInvoke(asyncResult);
                if (!string.IsNullOrEmpty(exmessage))
                {
                    Globals.Log("设备" + deviceNum + "连接失败" + "错误信息：" + exmessage);
                }
                else
                {
                    threadsReceiveTmp[deviceNum] = new Thread(new ParameterizedThreadStart(Receive));
                    threadsReceiveTmp[deviceNum].IsBackground = true;
                    threadsReceiveTmp[deviceNum].Start(deviceNum);
                    socketReceiveFlags[deviceNum] = true;
                }
            }
            catch (Exception e)
            {
                Globals.Log("设备" + deviceNum + "连接失败" + e.ToString());
            }
        }

        private void ByteToInt16(Byte[] arrByte, int nByteCount, ref Int16[] destInt16Arr)
        {
            //按两个字节⼀个整数解析，前⼀字节当做整数⾼位，后⼀字节当做整数低位
            //for (int i = 0; i < nByteCount / 2; i++)
            //{
            //    destInt16Arr[i] = Convert.ToInt16(arrByte[2 * i + 0] << 8 + arrByte[2 * i + 1]);
            //}
            int i = 0;
            try
            {
                //按两个字节一个整数解析，前一字节当做整数低位，后一字节当做整数高位，调用系统函数转化
                for (i = 0; i < nByteCount / 2; i++)
                {
                    Byte[] tmpBytes = new Byte[2] { arrByte[2 * i + 0], arrByte[2 * i + 1] };
                    destInt16Arr[i] = BitConverter.ToInt16(tmpBytes, 0);
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show("Byte to Int16转化错误！i=" + e.Message + i.ToString());
            }

        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
        }

        private void UiButton1_Click_1(object sender, EventArgs e)
        {
            if (saveVideoFlag)
            {
                saveVideoFlag = false;
            }
        }

        private string saveFilePath()
        {
            string filePath = "";
            // 创建保存对话框
            SaveFileDialog saveDataSend = new SaveFileDialog();
            saveDataSend.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);   // 获取文件路径
            if (saveDataSend.ShowDialog() == DialogResult.OK)   // 显示文件框，并且选择文件
            {
                filePath = saveDataSend.FileName.Replace("\\", "/");   // 获取文件名
            }
            return filePath;

        }

        private void Receive(object deviceNum)
        {
            int num = (int)deviceNum;
            int com_temp;
            //double min, max, avg;
            //int maxx, maxy, minx, miny;
            try
            {
                while (socketReceiveFlags[num])
                {
                    // a8Lists[num].Get_comp_temp(out com_temp);
                    //Console.WriteLine("设备温补" + com_temp);


                    byte[] buffer = new byte[1024 * 1024];
                    int r = sockets[num].Receive(buffer);
                    //Console.WriteLine("读取到数据");
                    //Console.WriteLine(r);


                    if (r == 0)
                    {
                        break;
                    }
                    else
                    {
                        Array.Copy(buffer, 0, dataBuffers[num], contentSizes[num], r);

                        if (dataBuffers[num][0] == 43 && dataBuffers[num][1] == 84 && dataBuffers[num][2] == 69 && dataBuffers[num][3] == 77 && dataBuffers[num][4] == 80)
                        {
                            receiveFlags[num] = true;
                            //存了数据个数
                            contentSizes[num] = contentSizes[num] + r;
                            //Console.WriteLine("目前存储数据");
                            //Console.WriteLine(contentSizes[num]);
                        }
                        else
                        {
                            receiveFlags[num] = false;
                            contentSizes[num] = 0;
                            Array.Clear(dataBuffers[num], 0, dataBuffers[num].Length);

                        }
                        //dataBuffer
                        if (receiveFlags[num])
                        {

                            //int min, max, avg;
                            byte[] Lbuffer = new byte[4];
                            Array.Copy(dataBuffers[num], 8, Lbuffer, 0, 4);
                            int bodySize = BitConverter.ToInt32(Lbuffer, 0);//从包头里面分析出包体的长度
                            //Console.WriteLine("包体长度");
                            //Console.WriteLine(bodySize);
                            ////缓存区小于9个字节，表示连表头都无法解析
                            //if (contentSize <= 9) return;
                            ////缓存区中的数据，不够解析一条完整的数据
                            //if (contentSize - 9 < bodySize) return;

                            if (contentSizes[num] <= 24 || contentSizes[num] - 24 < bodySize)
                            {

                            }
                            else
                            {
                                if (dataBuffers[num][0] == 43 && dataBuffers[num][1] == 84 && dataBuffers[num][2] == 69 && dataBuffers[num][3] == 77 && dataBuffers[num][4] == 80)
                                {

                                    byte[] Cbuffer = new byte[bodySize];
                                    Array.Copy(dataBuffers[num], 24, Cbuffer, 0, bodySize);

                                    Int16[] tempdate = new Int16[384 * 288];
                                    ByteToInt16(Cbuffer, Cbuffer.Length, ref tempdate);
                                    //realTemp = new int[256, 192];
                                    realTemps[num] = new int[384, 288];
                                    for (int i = 0; i < 384; i++)
                                    {
                                        for (int j = 0; j < 288; j++)
                                        {
                                            // ShortToUnsignedInt
                                            //realTemp[i][j] = (0xff & tempData[j * infraredImageWidth + i]) | (0xff00 & (tempData[j * infraredImageWidth + i + infraredImageWidth * infraredImageHeight] << 8)) & 0xffff;

                                            realTemps[num][i, j] = tempdate[j * 384 + i];


                                        }
                                    }
                                    double f = realTemps[num][321, 0] / 10.0f;
                                    //SetValue((Convert.ToString(f)));
                                    string s = f.ToString("F1");
                                    //Convert.ToString(f);
                                    //Console.WriteLine("(100,100)温度" + s);
                                    //MessageBox.Show(s);
                                    //把剩余的数据Copy到缓存区头部位置
                                    Array.Copy(dataBuffers[num], 24 + bodySize, dataBuffers[num], 0, contentSizes[num] - 24 - bodySize);
                                    contentSizes[num] = contentSizes[num] - 24 - bodySize;

                                    byte[] Testbuffer = new byte[contentSizes[num]];
                                    Array.Copy(dataBuffers[num], 0, Testbuffer, 0, contentSizes[num]);

                                    //Console.WriteLine("已存数据");
                                    //Console.WriteLine(contentSizes[num]);
                                    //Console.WriteLine(min);
                                    //i = byteto(buffer).Length;
                                    //Console.WriteLine(i);
                                    //MessageBox.Show(min.ToString("0.0"));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.Log("接收消息失败" + ex.ToString());
                MessageBox.Show("接收消息失败" + ex.ToString());
            }
        }
    }
}
