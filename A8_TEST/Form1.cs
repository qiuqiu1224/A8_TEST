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
using a8sdk;
using OpenCvSharp;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using OpenCvSharp.Extensions;
using System.Media;
using System.Drawing.Imaging;

namespace A8_TEST
{
    public partial class Form1 : UIForm
    {

        const uint DISPLAYWND_GAP = 1;//监控画面的间隙
        const uint DISPLAYWND_MARGIN_LEFT = 1;//监控画面距离左边控件的距离
        const uint DISPLAYWND_MARGIN_TOP = 1; //监控画面距离上边的距离
        const int PAGE_INDEX = 1000;
        const Int32 IR_VEDIO_WIDTH = 768;//红外图像视频帧宽度
        const Int32 IR_VEDIO_HEIGHT = 576;//红外图像视频帧高度
        const Int32 IR_TEMP_WIDTH = 388;//红外温度帧宽度
        const Int32 IR_TEMP_HEIGHT = 284;//红外温度帧高度

        Color PIC_CLICKED_COLOR = Color.FromArgb(128, 128, 255);
        Color PIC_UNCLICKED_COLOR = Color.FromArgb(45, 45, 53);
        private PictureBox[] pics;//显示图像控件
        private UIPage fmonitor;//监控界面
        private UIPage fbrowse;//浏览界面
        // private UIPanel pixUIPanel;//容纳PictureBox的Panel
        //public static TransparentLabel[] labels;//图像上面标注控件,背景透明

        UISymbolButton startPrewviewBtn, stopPrewviewBtn, startRecordBtn, stopRecordBtn,
            mouseFollowBtn, takePicBtn, drawRectBtn, drawCircleBtn, deleteAllDrawBtn;
        private bool isStartPrewview = false;//开始采集标志
        List<Socket> sockets = new List<Socket>();//连接红外相机获取温度socket
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

        //private bool mouseFollowFlag = false;//鼠标跟随标志

        List<int> picMouseX = new List<int>();//鼠标跟随x坐标
        List<int> picMouseY = new List<int>();//鼠标跟随y坐标
        List<float> tempMouseX = new List<float>();//鼠标跟随对应温度数组x坐标
        List<float> tempMouseY = new List<float>();//鼠标跟随对应温度数组y坐标

        #region 红外
        tstRtmp rtmp = new tstRtmp();//利用ffmpeg获取视频数据
        Thread thPlayer;//解码红外视频线程      


        //List<string> ipLists = new List<string>(); //设备ip集合
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

        private List<bool> saveVideoFlags = new List<bool>();


        private bool saveVideoFlag = false;//录制视频标志
        private bool saveImageFlag;//保存图像标志
        private bool saveAlarmImageFlag;//保存报警图像标志
        private bool alertFlag;//报警标志

        string recordName;//录制视频文件名
        VideoWriter writer;//存储红外视频对象
        private bool isInPic;//判断鼠标是否在图像内的标志

        string sVideoFileName;

        int selectType = -1;

        List<Object> objects = new List<object>();

        List<Bitmap> imageList = new List<Bitmap>();

        List<TempRuleInfo> tempRuleInfos = new List<TempRuleInfo>();
        int rectModeIndex = 0;
        IRC_NET_POINT mouseDownPoint = new IRC_NET_POINT();
        private List<IRC_NET_POINT> points = new List<IRC_NET_POINT>();
        public int iNowPaint_X_Start = 0;
        public int iNowPaint_Y_Start = 0;
        public int iNowPaint_X_End = 0;
        public int iNowPaint_Y_End = 0;
        public int iTempType = 2;
        bool idraw = false;
        //public float fSx;//在红外显示控件上画选框，转换成红外视频帧x轴方向的缩放比例
        //public float fSy;//在红外显示控件上画选框，转换成红外视频帧y轴方向的缩放比例




        public struct TempRuleInfo
        {
            public int type;
            public int index;
            public int startPointX;
            public int startPointY;
            public int endPointX;
            public int endPointY;
            public int maxTemp;
            public int maxTempLocX;
            public int maxTempLocY;
        }

        /// <summary>
        /// 测温工具模式
        /// </summary>
        public enum DrawMode
        {
            NO_DRAW = -1,
            DRAW_POINT,
            DRAW_LINE,
            DRAW_AREA,
            DRAW_CIRCLE,
            DRAW_POLYGON,
            DRAW_MOUSE//鼠标跟随
        }

        public struct IRC_NET_POINT
        {
            public int x; ///< x坐标
            public int y; ///< y坐标
        }


        [Obsolete]
        public Form1()
        {
            InitializeComponent();

            A8SDK.SDK_initialize();

            //a8 = new A8SDK("192.168.100.2");

            //this.TopMost = true;
            //this.WindowState = FormWindowState.Maximized;

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

            //获取Fmoitor界面拍照按钮，并添加相关事件
            takePicBtn = (UISymbolButton)fmonitor.GetControl("takePicBtn");
            takePicBtn.Click += new EventHandler(takePicBtn_Click);
            takePicBtn.MouseHover += new EventHandler(takePicBtn_MouseHover);
            takePicBtn.MouseLeave += new EventHandler(takePicBtn_MouseLeave);

            //获取Fmoitor界面画矩形按钮，并添加相关事件
            drawRectBtn = (UISymbolButton)fmonitor.GetControl("drawRectBtn");
            drawRectBtn.Click += new EventHandler(drawRectBtn_Click);
            drawRectBtn.MouseHover += new EventHandler(drawRectBtn_MouseHover);
            drawRectBtn.MouseLeave += new EventHandler(drawRectBtn_MouseLeave);

            //获取Fmoitor界面画圆形按钮，并添加相关事件
            drawCircleBtn = (UISymbolButton)fmonitor.GetControl("drawCircleBtn");
            drawCircleBtn.Click += new EventHandler(drawCircleBtn_Click);
            drawCircleBtn.MouseHover += new EventHandler(drawCircleBtn_MouseHover);
            drawCircleBtn.MouseLeave += new EventHandler(drawCircleBtn_MouseLeave);

            //获取Fmoitor界面删除所有选区按钮，并添加相关事件
            deleteAllDrawBtn = (UISymbolButton)fmonitor.GetControl("deleteAllDrawBtn");
            deleteAllDrawBtn.Click += new EventHandler(deleteAllDrawBtn_Click);
            deleteAllDrawBtn.MouseHover += new EventHandler(deleteAllDrawBtn_MouseHover);
            deleteAllDrawBtn.MouseLeave += new EventHandler(deleteAllDrawBtn_MouseLeave);

            //为按钮添加提示信息
            uiToolTip1.SetToolTip(startPrewviewBtn, "开始采集");
            uiToolTip1.SetToolTip(stopPrewviewBtn, "停止采集");
            uiToolTip1.SetToolTip(startRecordBtn, "开始录制");
            uiToolTip1.SetToolTip(stopRecordBtn, "停止录制");
            uiToolTip1.SetToolTip(mouseFollowBtn, "鼠标跟随");
            uiToolTip1.SetToolTip(takePicBtn, "手动抓图");
            uiToolTip1.SetToolTip(drawRectBtn, "矩形测温");
            uiToolTip1.SetToolTip(drawCircleBtn, "圆形测温");
            uiToolTip1.SetToolTip(deleteAllDrawBtn, "删除所有选区");


            uiNavBar1.SelectedIndex = 0;
            //StartPrewview();

        }

        private void deleteAllDrawBtn_MouseLeave(object sender, EventArgs e)
        {
            SetButtonImg(deleteAllDrawBtn, "delete.png");
        }

        private void deleteAllDrawBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(deleteAllDrawBtn, "delete_pressed.png");
        }

        private void deleteAllDrawBtn_Click(object sender, EventArgs e)
        {
            SetButtonImg(deleteAllDrawBtn, "delete.png");
            //SetButtonImg(drawCircleBtn, "circle.png");
            //SetButtonImg(drawRectBtn, "square.png");
            //SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            tempRuleInfos.Clear();
        }

        private void drawCircleBtn_MouseLeave(object sender, EventArgs e)
        {
            if (selectType != (int)DrawMode.DRAW_CIRCLE)
            {
                SetButtonImg(drawCircleBtn, "circle.png");
            }
        }

        private void drawCircleBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(drawCircleBtn, "circlePressed.png");
        }

        private void drawCircleBtn_Click(object sender, EventArgs e)
        {
            if (selectType != (int)DrawMode.DRAW_CIRCLE)
            {
                selectType = (int)DrawMode.DRAW_CIRCLE;
                SetButtonImg(drawCircleBtn, "circlePressed.png");
                SetButtonImg(drawRectBtn, "square.png");
                SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            }
            else
            {
                selectType = -1;
                SetButtonImg(drawCircleBtn, "circle.png");
            }
        }

        private void drawRectBtn_MouseLeave(object sender, EventArgs e)
        {
            if (selectType != (int)DrawMode.DRAW_AREA)
            {
                SetButtonImg(drawRectBtn, "square.png");
            }

        }

        private void drawRectBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(drawRectBtn, "square_pressed.png");

        }

        private void drawRectBtn_Click(object sender, EventArgs e)
        {
            if (selectType != (int)DrawMode.DRAW_AREA)
            {
                selectType = (int)DrawMode.DRAW_AREA;
                SetButtonImg(drawRectBtn, "square_pressed.png");
                SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
                SetButtonImg(drawCircleBtn, "circle.png");
            }
            else
            {
                selectType = -1;
                SetButtonImg(drawRectBtn, "square.png");
            }
        }

        private void ThreadAlert()
        {
            while (true)
            {
                Thread.Sleep(100);

                if (alertFlag)
                {
                    try
                    {
                        SoundPlayer player = new SoundPlayer();
                        player.SoundLocation = Application.StartupPath + "\\Alert.wav";
                        player.Load();
                        player.Play();
                        Thread.Sleep(5000);

                        alertFlag = false;
                    }
                    catch (Exception e)
                    {
                        Globals.Log("ThreadAlert" + e.Message);
                    }
                }
            }
        }


        private void takePicBtn_MouseLeave(object sender, EventArgs e)
        {
            SetButtonImg(takePicBtn, "抓图.png");
        }

        private void takePicBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(takePicBtn, "抓图3.png");
        }


        private void takePicBtn_Click(object sender, EventArgs e)
        {
            saveImageFlag = true;
        }

        protected override void OnResize(EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                //登录光学相机
                //LoginOpDevice(0, Globals.systemParam.op_ip_1, Globals.systemParam.op_username_1, Globals.systemParam.op_psw_1, Globals.systemParam.op_port_1, cbLoginCallBack);

                //Thread.Sleep(100);
                //采集预览光学图像
                //PreviewOpDevice(0, RealDataCallBack);
            }
            else if (WindowState == FormWindowState.Minimized)
            {

                //for (int i = 0; i < Globals.systemParam.deviceCount; i++)
                //{
                //    //isShowIRImageFlags[i] = false;//设置显示红外图像标志
                //    //socketReceiveFlags[i] = false;                

                //    //如果正在预览光学图像，停止预览，并设置mRealHandles为-1
                //    if (mRealHandles[i] >= 0)
                //    {
                //        CHCNetSDK.NET_DVR_StopRealPlay(mRealHandles[i]);
                //        mRealHandles[i] = -1;
                //    }

                //    //CHCNetSDK.NET_DVR_Cleanup();
                //}
                //StopPrewview();
            }
        }


        /// <summary>
        ///  鼠标跟随按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseFollowBtn_MouseLeave(object sender, EventArgs e)
        {

            //if (!mouseFollowFlag)
            if (selectType != 5)
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
            ////录像保存路径和文件名 the path and file name to save
            //string sVideoFileName;
            //sVideoFileName = "Record_test.mp4";

            //if (m_bRecord == false)
            //{
            //    //强制I帧 Make a I frame
            //     //通道号 Channel number
            //    CHCNetSDK.NET_DVR_MakeKeyFrame(mUserIDs[0], 1);

            //    //开始录像 Start recording
            //    if (!CHCNetSDK.NET_DVR_SaveRealData(mRealHandles[0], sVideoFileName))
            //    {
            //        iLastErr = CHCNetSDK.NET_DVR_GetLastError();
            //        str = "NET_DVR_SaveRealData failed, error code= " + iLastErr;
            //        MessageBox.Show(str);
            //        return;
            //    }
            //    else
            //    {

            //        m_bRecord = true;
            //    }
            //}
            //else
            //{
            //    //停止录像 Stop recording
            //    if (!CHCNetSDK.NET_DVR_StopSaveRealData(mRealHandles[0]))
            //    {
            //        iLastErr = CHCNetSDK.NET_DVR_GetLastError();
            //        str = "NET_DVR_StopSaveRealData failed, error code= " + iLastErr;
            //        MessageBox.Show(str);
            //        return;
            //    }
            //    else
            //    {
            //        str = "Successful to stop recording and the saved file is " + sVideoFileName;
            //        MessageBox.Show(str);                   
            //        m_bRecord = false;
            //    }
            //}

            //return;
            //没有开始采集，返回
            if (!isStartPrewview)
            {
                return;
            }

            if (selectType != 5)
            {
                selectType = 5;
                //mouseFollowFlag = true;
                SetButtonImg(mouseFollowBtn, "鼠标跟随1.png");
                SetButtonImg(drawRectBtn, "square.png");
                SetButtonImg(drawCircleBtn, "circle.png");
            }
            else
            {
                selectType = -1;
                //mouseFollowFlag = false;
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
        /// 停止录制视频按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopRecordBtn_Click(object sender, EventArgs e)
        {
            if (saveVideoFlag)
            {
                //停止录像 Stop recording
                if (!CHCNetSDK.NET_DVR_StopSaveRealData(mRealHandles[0]))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_StopSaveRealData failed, error code= " + iLastErr;
                    MessageBox.Show(str);
                    return;
                }
                else
                {
                    str = "Successful to stop recording and the saved file is " + sVideoFileName;
                    MessageBox.Show(str);

                }

                saveVideoFlag = false;
                SetButtonImg(startRecordBtn, "开始录制-line.png");
            }
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


        private void Receive(object deviceNum)
        {
            int num = (int)deviceNum;
            int test = 0;
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
                    test = r;
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

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {

                Globals.Log("接收消息失败" + ex.ToString());
                Globals.Log("目标数组长度：" + dataBuffers[num].Length + "destIndex：" + contentSizes[num] + "r:" + test);
                //MessageBox.Show("接收消息失败" + ex.ToString());
            }
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
                recordName = GetIrImageFilePath(Globals.RecordDirectoryPath, 0, "_IR.avi");
                //string filePath = saveFilePath();
                //if (recordName == "")
                //{
                //    return;
                //}
                // recordName = filePath + ".avi";

                //利用VideoWriter对象 录制视频  红外视频帧频25HZ，分辨率768*576      
                writer = new VideoWriter(recordName, FourCC.MJPG, 10, new OpenCvSharp.Size(768, 576), true);
                saveVideoFlag = true;
            }

            ////录像保存路径和文件名 the path and file name to save


            sVideoFileName = GetIrImageFilePath(Globals.RecordDirectoryPath, 0, "_OP.mp4");
            CHCNetSDK.NET_DVR_MakeKeyFrame(mUserIDs[0], 1);
            if (!CHCNetSDK.NET_DVR_SaveRealData(mRealHandles[0], sVideoFileName))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "NET_DVR_SaveRealData failed, error code= " + iLastErr;
                MessageBox.Show(str);
                return;
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

            thPlayer = new Thread(DeCoding);
            thPlayer.IsBackground = true;
            thPlayer.Start();

            //ParameterizedThreadStart thStart = new ParameterizedThreadStart(DeCoding);//threadStart委托 
            //Thread thread = new Thread(thStart);
            ////thread.Priority = ThreadPriority.Highest;
            //thread.IsBackground = true; //关闭窗体继续执行
            //thread.Start(num);
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


        private void StartPrewview()
        {
            isStartPrewview = true;

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


            Thread.Sleep(100);

            ParameterizedThreadStart thStart = new ParameterizedThreadStart(ShowIRImageThreadProc);//threadStart委托 
            Thread thread = new Thread(thStart);
            //thread.Priority = ThreadPriority.Highest;
            thread.IsBackground = true; //关闭窗体继续执行
            thread.Start(0);

            SetButtonImg(startPrewviewBtn, "开始(1).png");
            //登录光学相机
            LoginOpDevice(0, Globals.systemParam.op_ip_1, Globals.systemParam.op_username_1, Globals.systemParam.op_psw_1, Globals.systemParam.op_port_1, cbLoginCallBack);

            Thread.Sleep(100);
            //采集预览光学图像
            PreviewOpDevice(0, RealDataCallBack);

            ConnectSocketToReceiveTemp(0);


            Thread GetTmpThread = new Thread(GetTmp);
            GetTmpThread.IsBackground = true;
            GetTmpThread.Start();


            Thread threadAlert = new Thread(new ThreadStart(ThreadAlert));
            threadAlert.Name = "threadAlert";
            threadAlert.Start();

  
        }

        private void GetTmp()
        {
            while (isShowIRImageFlags[0])
            {
                a8Lists[0].Get_globa_temp(out globa_Temp);//获取全局温度信息

                //int i;
                //a8sdk.A8SDK.area_temp area_Temp;
                ////area_Temp = a8.Get_area_temp(1);
                //i = a8Lists[0].Get_area_temp(1, out area_Temp);
                //Console.WriteLine("执行返回： " + i.ToString());
                //Console.WriteLine(area_Temp.max_temp.ToString());

                if (tempRuleInfos.Count > 0)
                {

                    for (int i = 0; i < tempRuleInfos.Count; i++)
                    {
                        if (tempRuleInfos[i].type == (int)DrawMode.DRAW_AREA)
                        {
                            int[] results = getTempAtRect(realTemps[0], tempRuleInfos[i].startPointX / 2, tempRuleInfos[i].startPointY / 2, tempRuleInfos[i].endPointX / 2, tempRuleInfos[i].endPointY / 2);

                            TempRuleInfo temp = tempRuleInfos[i];
                            temp.maxTemp = results[0];
                            temp.maxTempLocX = results[1];
                            temp.maxTempLocY = results[2];
                            tempRuleInfos[i] = temp;
                        }

                        if (tempRuleInfos[i].type == (int)DrawMode.DRAW_CIRCLE)
                        {
                            int startX = tempRuleInfos[i].startPointX / 2;
                            int startY = tempRuleInfos[i].startPointY / 2;
                            int endX = tempRuleInfos[i].endPointX / 2;
                            int endY = tempRuleInfos[i].endPointY / 2;
                            int radiusX = (endX - startX) / 2;
                            int radiusY = (endY - startY) / 2;
                            int[] results = FindMaxValueInEllipse(realTemps[0], startX + radiusX, startY + radiusY, radiusX, radiusY);
                            //int[] results = getTempAtRect(realTemps[0], tempRuleInfos[i].startPointX / 2, tempRuleInfos[i].startPointY / 2, tempRuleInfos[i].endPointX / 2, tempRuleInfos[i].endPointY / 2);

                            TempRuleInfo temp = tempRuleInfos[i];
                            temp.maxTemp = results[0];
                            temp.maxTempLocX = results[1];
                            temp.maxTempLocY = results[2];
                            tempRuleInfos[i] = temp;

                            Console.WriteLine("椭圆最大值" + results[0]);
                        }
                    }





                    //Console.WriteLine("矩形最大值" + results[0]);

                    //int startX = tempRuleInfos[0].startPointX / 2;
                    //int startY = tempRuleInfos[0].startPointY / 2;
                    //int endX = tempRuleInfos[0].endPointX / 2;
                    //int endY = tempRuleInfos[0].endPointY / 2;
                    //int radiusX = (endX - startX) / 2;
                    //int radiusY = (endY - startY) / 2;
                    //int max = FindMaxValueInEllipse(realTemps[0], startX + radiusX, startY + radiusY, radiusX, radiusY);

                    //Console.WriteLine("椭圆最大值" + max);

                }


                //if (tempRuleInfos.Count > 0)
                //{
                //    getTempAtRect(realTemps[0], tempRuleInfos[0].startPointX, tempRuleInfos[0].startPointY, tempRuleInfos[0].endPointX, tempRuleInfos[0].endPointY);
                //}

                Thread.Sleep(100);
            }

        }

        /// <summary>
        /// 停止采集预览
        /// </summary>
        private void StopPrewview()
        {
            isStartPrewview = false;//设置停止采集标志位false   
            saveVideoFlag = false;

            if (thPlayer != null)
            {
                rtmp.Stop();

                thPlayer = null;
            }

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

                CHCNetSDK.NET_DVR_Cleanup();

                //图像预览控件清空，不显示图像
                pics[i * 2].Image = null;
                pics[i * 2 + 1].Image = null;

                irImageLists[i].Clear();
            }

            //停止保存报警图像定时器
            timer3.Enabled = false;

            //设置监控界面功能按钮的图像

            SetButtonImg(startPrewviewBtn, "开始 .png");
            SetButtonImg(stopPrewviewBtn, "stopPressed.png");
            SetButtonImg(startRecordBtn, "开始录制-line.png");
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

        /// <summary>
        /// 设置按钮图片
        /// </summary>
        /// <param name="btn"></param>
        /// <param name="imageName"></param>
        private void SetButtonImg(UISymbolButton btn, string imageName)
        {
            btn.Image = Image.FromFile(Globals.startPathInfo.FullName + "\\Resources\\" + imageName);
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

            //m_struTimeCfg.dwYear = DateTime.Now.Year; ;
            //m_struTimeCfg.dwMonth = DateTime.Now.Month;
            //m_struTimeCfg.dwDay = DateTime.Now.Day;
            //m_struTimeCfg.dwHour = DateTime.Now.Hour;
            //m_struTimeCfg.dwMinute = DateTime.Now.Minute;
            //m_struTimeCfg.dwSecond = DateTime.Now.Second;

            //Int32 nSize = Marshal.SizeOf(m_struTimeCfg);
            //IntPtr ptrTimeCfg = Marshal.AllocHGlobal(nSize);
            //Marshal.StructureToPtr(m_struTimeCfg, ptrTimeCfg, false);

            //if (!CHCNetSDK.NET_DVR_SetDVRConfig(mUserIDs[0], CHCNetSDK.NET_DVR_SET_TIMECFG, -1, ptrTimeCfg, (UInt32)nSize))
            //{
            //    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
            //    str = "NET_DVR_SET_TIMECFG failed, error code= " + iLastErr;
            //    //设置时间失败，输出错误号 Failed to set the time of device and output the error code
            //    MessageBox.Show(str);
            //}
            //else
            //{
            //    //MessageBox.Show("校时成功！");
            //}

            //Marshal.FreeHGlobal(ptrTimeCfg);
        }


        private void ShowIRImageThreadProc(object deviceNum)
        {

            int i = 0;
            int j = 0;
            int num = (int)deviceNum;
            Bitmap bitmap;
            Bitmap oldBmp = null;
            while (true)
            {
                if (isShowIRImageFlags[num])
                {
                    try
                    {
                        //Console.WriteLine(DateTime.Now);
                        //Console.WriteLine("irImageLists[num].Count" + irImageLists[num].Count);
                        //红外图像集合长度不为0

                        if (irImageLists[num].Count > 0)
                        {
                            //Console.WriteLine("  Console.WriteLine(irImageLists[num].Count);" + irImageLists[num].Count);
                            //this.Invoke(new MethodInvoker(() =>
                            //{

                            bitmap = (Bitmap)irImageLists[num][0].Clone();

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
                                        if (j == 1)
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

                                    //Console.WriteLine(" iNowPaint_X_Start" + iNowPaint_X_Start);
                                    //Console.WriteLine(" iNowPaint_Y_Start" + iNowPaint_Y_Start);
                                    //Console.WriteLine(" iNowPaint_X_End" + iNowPaint_X_End);
                                    //Console.WriteLine(" iNowPaint_Y_End" + iNowPaint_Y_End);
                                    //Console.WriteLine(" pics[0].Width" + pics[0].Width);
                                    //Console.WriteLine(" pics[0].Height" + pics[0].Height);

                                    if ((selectType == (int)DrawMode.DRAW_AREA) && iTempType == 3 && idraw == true)
                                    {
                                        //gfx.DrawPoint(Color.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, 4);
                                        gfx.DrawRectangle(Pens.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, iNowPaint_X_End - iNowPaint_X_Start, iNowPaint_Y_End - iNowPaint_Y_Start);

                                        //gfx.DrawEllipse(Pens.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, iNowPaint_X_End - iNowPaint_X_Start, iNowPaint_Y_End - iNowPaint_Y_Start);
                                    }

                                    if ((selectType == (int)DrawMode.DRAW_CIRCLE) && iTempType == 3 && idraw == true)
                                    {
                                        //gfx.DrawPoint(Color.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, 4);
                                        //gfx.DrawPoint(Color.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, 4);

                                        gfx.DrawEllipse(Pens.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, iNowPaint_X_End - iNowPaint_X_Start, iNowPaint_Y_End - iNowPaint_Y_Start);
                                    }

                                    //Console.WriteLine("tempRuleInfos.Count" + tempRuleInfos.Count);
                                    for (int k = 0; k < tempRuleInfos.Count; k++)
                                    {

                                        if (tempRuleInfos[k].type == (int)DrawMode.DRAW_AREA)
                                        {
                                            gfx.DrawRectangle(new Pen(Color.LightGreen, 2), tempRuleInfos[k].startPointX, tempRuleInfos[k].startPointY, tempRuleInfos[k].endPointX - tempRuleInfos[k].startPointX, tempRuleInfos[k].endPointY - tempRuleInfos[k].startPointY);



                                        }
                                        if (tempRuleInfos[k].type == (int)DrawMode.DRAW_CIRCLE)
                                        {
                                            gfx.DrawEllipse(new Pen(Color.LightGreen, 2), tempRuleInfos[k].startPointX, tempRuleInfos[k].startPointY, tempRuleInfos[k].endPointX - tempRuleInfos[k].startPointX, tempRuleInfos[k].endPointY - tempRuleInfos[k].startPointY);
                                        }

                                        DrawCrossLine(gfx, tempRuleInfos[k].maxTempLocX * pt, tempRuleInfos[k].maxTempLocY * pt, pen, 10);
                                        maxTemp = ((float)tempRuleInfos[k].maxTemp / 10).ToString("F1");//全局最高温度，保留一位小数
                                        point = new PointF(tempRuleInfos[k].maxTempLocX * 2, tempRuleInfos[k].maxTempLocY * 2);
                                        gfx.DrawString(maxTemp, font, brush, point);

                                    }

                                    if (saveImageFlag)
                                    {
                                        string IrImagePath = GetIrImageFilePath(Globals.ImageDirectoryPath, 0, "_IR.jpg");

                                        byte[] ss = BitmapToByteArray(bitmap);

                                        //Bitmap a = ByteToBitmap(ss);
                                        //string str = "buffertest.bmp";

                                        using (FileStream fs = new FileStream(IrImagePath, FileMode.Create))
                                        {
                                            int iLen = (int)ss.Length;
                                            fs.Write(ss, 0, iLen);
                                        }
                                        //FileStream fs = new FileStream(IrImagePath, FileMode.Create);
                                        //int iLen = (int)ss.Length;
                                        //fs.Write(ss, 0, iLen);
                                        //fs.Close();

                                        //a.Save(IrImagePath, System.Drawing.Imaging.ImageFormat.Bmp);

                                       // bitmap.Save(IrImagePath, System.Drawing.Imaging.ImageFormat.bmp);

                                        SaveOpImage(0, Globals.ImageDirectoryPath, mRealHandles[0], 1);
                                        saveImageFlag = false;
                                    }


                                    //保存报警图像
                                    if (saveAlarmImageFlag)
                                    {
                                        string IrImagePath = GetIrImageFilePath(Globals.AlarmImageDirectoryPath, 0, "_IR.bmp");
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
                                    pics[num].Image = bitmap;


                                    //鼠标跟随
                                    //if (mouseFollowFlag)
                                    if (selectType == (int)DrawMode.DRAW_MOUSE)
                                    {
                                        //鼠标在图像内
                                        if (isInPic)
                                        {
                                            Console.WriteLine("isInPic");
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
                                            //Console.WriteLine(writer.IsOpened());
                                            var mat = BitmapConverter.ToMat(b);
                                            ///Mat mat = Bitmap2Mat(bitmap);
                                            writer.Write(mat);

                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show("录制视频失败" + ex.ToString());
                                        }
                                    }

                                    if (oldBmp != null)
                                    {
                                        oldBmp.Dispose();
                                    }
                                    oldBmp = bitmap;
                                    b = null;
                                    bitmap = null;

                                }
                            }
                            //}));
                            //红外图像集合数量大于5，删除多余图像，防止阻塞
                            //Console.WriteLine("irImageLists[num].Count" + irImageLists[num].Count);
                            if (irImageLists[num].Count > 1)
                            {
                                irImageLists[num].RemoveRange(0, irImageLists[num].Count - 1);
                            }

                            //irImageLists[num].RemoveAt(0);
                        }

                        //else
                        //{
                        //    Globals.Log("irImageLists[num].Count == 0");
                        //}

                    }
                    catch (Exception ex)
                    {
                        Globals.Log("显示红外图像失败" + ex.ToString());
                        Globals.Log("irImageLists[num].Count" + irImageLists[num].Count);

                        irImageLists[num].Clear();
                        if (thPlayer != null)
                        {
                            rtmp.Stop();
                            thPlayer = null;
                        }

                        thPlayer = new Thread(DeCoding);
                        thPlayer.IsBackground = true;
                        thPlayer.Start();

                        //Console.WriteLine(ex.ToString());
                    }
                    Thread.Sleep(35);

                }
                else
                {
                    break;
                }
            }

            Globals.Log("红外图像显示线程结束");
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
                lpPreviewInfo.dwDisplayBufNum = 5; //播放库播放缓冲区最大缓冲帧数
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
                mRealHandles[deviceNum] = CHCNetSDK.NET_DVR_RealPlay_V40(mUserIDs[deviceNum], ref lpPreviewInfo, null/*RealDatas[deviceNum]*/, pUser);
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

                Console.WriteLine("dwBufSize" + dwBufSize);
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
            if (a8Lists[0] != null)
            {
                a8Lists[0].Get_globa_temp(out globa_Temp);//获取全局温度信息
                                                          //Console.WriteLine(DateTime.Now);
                                                          //Console.WriteLine(globa_Temp.max_temp)
            }

        }

        private void Timer3_Tick(object sender, EventArgs e)
        {
            //Console.WriteLine("定时器3" + DateTime.Now);
            //Console.WriteLine(" mRealHandles[0]" + mRealHandles[0]);
            //读取配置文件          
            SaveOpImage(0, Globals.AlarmImageDirectoryPath, mRealHandles[0], 1);
            saveAlarmImageFlag = true;

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
                //Console.WriteLine("DeCoding run...");
                Bitmap oldBmp = null;
                isShowIRImageFlags[0] = true;

                // 更新图片显示
                tstRtmp.ShowBitmap show = (bmp) =>
                {
                    Console.WriteLine(DateTime.Now);
                    // pics[0].Image = bmp;
                    if (bmp != null)
                    {                        
                        irImageLists[0].Add((Bitmap)bmp.Clone());
                    }
                    else
                    {
                        Globals.Log("Decoding:" + "bmp == null");
                        //if (thPlayer != null)
                        //{
                        //    rtmp.Stop();
                        //    thPlayer = null;
                        //}

                        //thPlayer = new Thread(DeCoding);
                        //thPlayer.IsBackground = true;
                        //thPlayer.Start();
                    }

                    //if (oldBmp != null)
                    //{
                    //    oldBmp.Dispose();
                    //}
                    //oldBmp = bmp;


                };
                rtmp.Start(show, "rtsp://" + Globals.systemParam.ir_ip_1 + "/webcam");

            }
            catch (Exception ex)
            {

                Globals.Log("DeCoding" + ex.ToString());
                //Console.WriteLine(ex);
                // 更新图片显示
                if (thPlayer != null)
                {
                    rtmp.Stop();
                    thPlayer = null;
                }

                thPlayer = new Thread(DeCoding);
                thPlayer.IsBackground = true;
                thPlayer.Start();
            }
            finally
            {
                Console.WriteLine("DeCoding exit");
            }
        }

        private void ClosePictureBox2_Click(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < Globals.systemParam.deviceCount; i++)
                {
                    isShowIRImageFlags[i] = false;
                    socketReceiveFlags[i] = false;

                    if (sockets[i] != null)
                    {
                        sockets[i].Close();
                    }


                    if (mRealHandles[i] >= 0)
                    {
                        CHCNetSDK.NET_DVR_StopRealPlay(mRealHandles[i]);
                        mRealHandles[i] = -1;
                    }

                    if (mUserIDs[i] >= 0)
                    {
                        CHCNetSDK.NET_DVR_Logout(mUserIDs[i]);
                        mUserIDs[i] = -1;

                    }

                }

                if (thPlayer != null)
                {
                    rtmp.Stop();

                    thPlayer = null;
                }

                CHCNetSDK.NET_DVR_Cleanup();

                Globals.fileInfos = null;
            }
            catch (Exception ex)
            {
                Globals.Log("关闭窗口" + ex.ToString());
            }
            this.Close();
            Application.Exit();
            System.Environment.Exit(0);
        }

        private void PictureBox3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;

        }

        /// <summary>
        /// 获取红外图像文件路径
        /// </summary>
        /// <param name="rootPath">根路径</param>
        /// <param name="deviceNum">设备号，从0开始</param>
        /// <returns></returns>
        private string GetIrImageFilePath(string rootPath, int deviceNum, string name)
        {
            string imagePath = rootPath + deviceNum;

            //判断文件夹是否存在，如果不存在，新建文件夹
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }

            string strTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            string IrImagePath = imagePath + "\\" + strTime + name;

            return IrImagePath;
        }


        private void DrawCrossLine(Graphics g, float startX, float startY, Pen pen, int lineLength)
        {
            g.DrawLine(pen, startX, startY, startX + lineLength, startY);
            g.DrawLine(pen, startX, startY, startX - lineLength, startY);
            g.DrawLine(pen, startX, startY, startX, startY + lineLength);
            g.DrawLine(pen, startX, startY, startX, startY - lineLength);
        }

        private void UiNavBar1_MenuItemClick(string itemText, int menuIndex, int pageIndex)
        {

            Console.WriteLine("UiNavBar1_MenuItemClic" + pageIndex);
            Console.WriteLine("isStartPrewview" + isStartPrewview);
            if (pageIndex == PAGE_INDEX)
            {
                StartPrewview();

                SetButtonImg(startPrewviewBtn, "开始(1).png");
                SetButtonImg(stopPrewviewBtn, "stop.png");



                //startPrewviewBtn.Image = Image.FromFile(Globals.startPathInfo.Parent.Parent.FullName + "\\Resources\\" + "开始(1).png");
                //stopPrewviewBtn.Image = Image.FromFile(Globals.startPathInfo.Parent.Parent.FullName + "\\Resources\\" + "stop.png");

            }
            else
            {
                StopPrewview();

            }


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

            //设备校时
            Timing();

            DirectoryInfo dirInfo = new DirectoryInfo(Globals.AlarmImageDirectoryPath + 0);
            //Globals.fileInfos = dirInfo.GetFiles("*.bmp");
            Globals.fileInfos = dirInfo.GetFiles();
            Globals.SortFolderByCreateTime(ref Globals.fileInfos);
            //Console.WriteLine("FormBrowse_Load" + Globals.fileInfos[0].Name);

        }

        private void TabPage1_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void Pics0_MouseLeave(object sender, EventArgs e)
        {
            //mouseFollowFlag = false;
            isInPic = false;
        }

        private void UiButton1_Click(object sender, EventArgs e)
        {
            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = Application.StartupPath + "\\" + "ffmpeg.exe";
                process.StartInfo.Arguments = "-i";
            }
        }

        private void Pics0_MouseUp(object sender, MouseEventArgs e)
        {
            //this.uiToolTip1.SetToolTip(sender as PictureBox, "23.5");
            //if (e.Button == MouseButtons.Left)
            //{
            //    switch (selectType)
            //    {

            //        case (int)DrawMode.DRAW_POINT:
            //            //AddRule(selectType, points);
            //            //points.Clear();
            //            //isDrawing = false;
            //            break;
            //        case (int)DrawMode.DRAW_LINE:
            //        case (int)DrawMode.DRAW_AREA:
            //        case (int)DrawMode.DRAW_CIRCLE:
            //            if (2 == points.Count)
            //            {
            //                TempRuleInfo tempRuleInfo = new TempRuleInfo();
            //                tempRuleInfo.type = selectType;
            //                tempRuleInfo.index = rectModeIndex;
            //                tempRuleInfo.startPointX = points[0].x;
            //                tempRuleInfo.startPointY = points[0].y;
            //                tempRuleInfo.endPointX = points[1].x;
            //                tempRuleInfo.endPointY = points[1].y;

            //                tempRuleInfos.Add(tempRuleInfo);
            //                //AddRule(selectType, points);
            //                points.Clear();
            //                //isDrawing = false;
            //            }
            //            break;
            //    }
            //}
        }


        private void Pics0_MouseMove(object sender, MouseEventArgs e)
        {
            // iTempType = 3;
            if (!isStartPrewview)
            {
                return;
            }

            switch (selectType)
            {
                case (int)DrawMode.DRAW_MOUSE:
                    isInPic = true;
                    //Console.WriteLine("Pics0_MouseMove");

                    PictureBox pic = sender as PictureBox;
                    //Console.WriteLine("pic.Width" + pic.Width);

                    //// 获取鼠标在PictureBox内的位置
                    ///

                    picMouseX[0] = e.X;
                    picMouseY[0] = e.Y;

                    tempMouseX[0] = picMouseX[0] * 1.0f / (pics[0].Width) * 384;
                    tempMouseY[0] = picMouseY[0] * 1.0f / (pics[0].Height) * 288;
                    break;

                case (int)DrawMode.DRAW_LINE:
                case (int)DrawMode.DRAW_AREA:
                case (int)DrawMode.DRAW_CIRCLE:
                    idraw = true;
                    if (iTempType == 3)
                    {
                        iNowPaint_X_End = e.X * 768 / pics[0].Width;
                        iNowPaint_Y_End = e.Y * 576 / pics[0].Height;

                        mouseDownPoint.x = e.X * 768 / pics[0].Width;
                        mouseDownPoint.y = e.Y * 576 / pics[0].Height;

                        if (points.Count == 1)
                        {

                            points.Add(mouseDownPoint);
                        }
                        else if (points.Count == 2)
                        {
                            points[1] = mouseDownPoint;
                        }
                    }

                    break;
            }




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
        /// 设置实时监控界面图像显示控件布局
        /// </summary>
        /// <param name="rowNum">行数</param>
        /// <param name="colNum">列数</param>

        private void SetFmonitorDisplayWnds(uint rowNum, uint colNum)
        {
            //uint w = (uint)(Screen.PrimaryScreen.Bounds.Width - fmonitor.GetControl("uiPanel1").Width);
            //uint w = (uint)(Screen.PrimaryScreen.Bounds.Width - fmonitor.GetControl("uiNavMenu1").Width);

            //uint w = (uint)(this.ClientSize.Width);

            //uint h = (uint)(this.ClientSize.Height - uiNavBar1.Height - fmonitor.GetControl("uiPanel1").Height);

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
                            pics[i * 2 + j].MouseClick += new MouseEventHandler(Pics0_MouseClick);
                            pics[i * 2 + j].MouseDown += new MouseEventHandler(Pics0_MouseDown);
                            pics[i * 2 + j].MouseMove += new MouseEventHandler(Pics0_MouseMove);
                            pics[i * 2 + j].MouseLeave += new EventHandler(Pics0_MouseLeave);
                            //pics[i * 2 + j].MouseHover += new EventHandler(Pics0_MouseUp);
                            pics[i * 2 + j].MouseUp += new MouseEventHandler(Pics0_MouseUp);
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

        private void Pics0_MouseClick(object sender, MouseEventArgs e)
        {
            if (selectType != -1)
            {


                int iX = e.X * 768 / pics[0].Width;
                int iY = e.Y * 576 / pics[0].Height;

                mouseDownPoint.x = iX;
                mouseDownPoint.y = iY;

                iNowPaint_X_End = 0;
                iNowPaint_Y_End = 0;

                if (e.Button == MouseButtons.Left)
                {
                    if (iTempType == 2)//画矩形或画圆形
                    {
                        idraw = false;
                        iNowPaint_X_Start = iX;
                        iNowPaint_Y_Start = iY;

                        points.Add(mouseDownPoint);

                        iTempType = 3;
                    }

                }
                else if (e.Button == MouseButtons.Right)
                {
                    iTempType = 2;
                    idraw = false;
                    switch (selectType)
                    {
                        case (int)DrawMode.DRAW_POINT:
                        case (int)DrawMode.DRAW_LINE:
                        case (int)DrawMode.DRAW_AREA:
                        case (int)DrawMode.DRAW_CIRCLE:

                            if (2 == points.Count)
                            {

                                TempRuleInfo tempRuleInfo = new TempRuleInfo();
                                tempRuleInfo.type = selectType;
                                tempRuleInfo.index = rectModeIndex;
                                tempRuleInfo.startPointX = points[0].x;
                                tempRuleInfo.startPointY = points[0].y;
                                tempRuleInfo.endPointX = points[1].x;
                                tempRuleInfo.endPointY = points[1].y;

                                //tempRuleInfos[rectModeIndex] = tempRuleInfo;
                                tempRuleInfos.Add(tempRuleInfo);

                                //a8sdk.A8SDK.area_pos area_data;

                                //area_data.enable = 1;
                                //area_data.height = points[1].y- points[0].y;
                                //area_data.width = points[1].x- points[0].x;
                                //area_data.x = points[0].x;
                                //area_data.y = points[0].y;
                                //int i = a8Lists[0].Set_area_pos(0, area_data);
                                //Console.WriteLine("执行结果" + i);

                                points.Clear();

                                //int i;
                                //a8sdk.A8SDK.area_pos area_data;

                                //Console.WriteLine(" tempRuleInfos[0].startPointX" + tempRuleInfos[0].startPointX);
                                //Console.WriteLine("tempRuleInfos[0].startPointY " + tempRuleInfos[0].startPointY);
                                //Console.WriteLine("tempRuleInfos[0].endPointX" + tempRuleInfos[0].endPointX);
                                //Console.WriteLine("tempRuleInfos[0].endPointY" + tempRuleInfos[0].endPointY);


                                //int x1 = tempRuleInfos[0].startPointX / 4;
                                //int y1 = tempRuleInfos[0].startPointY / 4;

                                //int x2 = tempRuleInfos[0].endPointX / 4;
                                //int y2 = tempRuleInfos[0].endPointY / 4;

                                //Console.WriteLine("x1" + x1);
                                //Console.WriteLine("y1" + y1);
                                //Console.WriteLine("x2" + x2);
                                //Console.WriteLine("y2" + y2);

                                //area_data.enable = 1;
                                //area_data.height = y2-y1;
                                //area_data.width = x2-x1;
                                //area_data.x = x1;
                                //area_data.y = y1;
                                //i = a8Lists[0].Set_area_pos(1, area_data);


                                //rectModeIndex++;

                            }
                            break;
                    }
                }

            }
        }

        public int[] getTempAtRect(int[,] realTemp, int X1, int Y1, int X2, int Y2)
        {
            int[] result = new int[3];
            int startX = X1 < X2 ? X1 : X2;
            int startY = Y1 < Y2 ? Y1 : Y2;
            int endX = X1 < X2 ? X2 : X1;
            int endY = Y1 < Y2 ? Y2 : Y1;
            result[0] = realTemp[startX, startY];
            result[1] = startX;
            result[2] = startY;

            for (int j = startY; j < endY; ++j)
            {
                for (int i = startX; i < endX; ++i)
                {


                    if (realTemp[i, j] > result[0])
                    {
                        result[0] = realTemp[i, j];
                        result[1] = i;
                        result[2] = j;
                    }

                }
            }
            return result;
        }


        public int[] FindMaxValueInEllipse(int[,] imageData, int ellipseCenterX, int ellipseCenterY, int ellipseRadiusX, int ellipseRadiusY)
        {
            int[] result = new int[3];
            result[0] = int.MinValue;
            //Console.WriteLine("ellipseCenterX:" + ellipseCenterX);
            //Console.WriteLine("ellipseCenterY:" + ellipseCenterY);
            //Console.WriteLine("ellipseRadiusX:" + ellipseRadiusX);
            //Console.WriteLine("ellipseRadiusY:" + ellipseRadiusY);

            for (int y = ellipseCenterY - ellipseRadiusY; y <= ellipseCenterY + ellipseRadiusY; y++)
            {
                for (int x = ellipseCenterX - ellipseRadiusX; x <= ellipseCenterX + ellipseRadiusX; x++)
                {

                    // 检查点是否在椭圆内
                    if (IsPointInEllipse(x, y, ellipseCenterX, ellipseCenterY, ellipseRadiusX, ellipseRadiusY))
                    {
                        //Console.WriteLine(" imageData.GetLength(0)" + imageData.GetLength(0));
                        //Console.WriteLine(" imageData.GetLength(1)" + imageData.GetLength(1));

                        // 确保坐标在图像数组范围内
                        if (x >= 0 && x < imageData.GetLength(0) && y >= 0 && y < imageData.GetLength(1))
                        {
                            int currentValue = imageData[x, y];
                            if (currentValue > result[0])
                            {
                                result[0] = currentValue;
                                result[1] = x;
                                result[2] = y;
                            }
                            // result[0] = Math.Max(result[0], currentValue);

                        }
                    }
                }
            }

            return result;
        }

        // 判断点是否在椭圆内的方法
        private bool IsPointInEllipse(int x, int y, int ellipseCenterX, int ellipseCenterY, int ellipseRadiusX, int ellipseRadiusY)
        {
            // 使用椭圆的标准方程进行检查
            double xDiff = x - ellipseCenterX;
            double yDiff = y - ellipseCenterY;
            double xRadiusSquared = ellipseRadiusX * ellipseRadiusX;
            double yRadiusSquared = ellipseRadiusY * ellipseRadiusY;
            double ratio = xRadiusSquared / yRadiusSquared;

            return (xDiff * xDiff) / xRadiusSquared + (yDiff * yDiff) / (ratio * yRadiusSquared) <= 1;
        }

        private void Pics0_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private byte[] BitmapToBytes(Bitmap bitmap)
        {
            // 1.先将BitMap转成内存流
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            // 2.再将内存流转成byte[]并返回
            byte[] bytes = new byte[ms.Length];
            ms.Read(bytes, 0, bytes.Length);
            ms.Dispose();
            return bytes;
        }
        public Bitmap ByteToBitmap(byte[] ImageByte)
        {
            Bitmap bitmap = null; using (MemoryStream stream = new MemoryStream(ImageByte))
            {
                bitmap = new Bitmap((Image)new Bitmap(stream));
            }
            return bitmap;
        }

        // Bitmap转换为byte数组
        public static byte[] BitmapToByteArray(Bitmap bitmap)
        {
            //int w = bmp.Width;
            //int h = bmp.Height;
            //Rectangle rect = new Rectangle(0, 0, w, h);
            //BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            //// 获取图像数据的地址
            //IntPtr ptr = bmpData.Scan0;
            //// 创建字节数组
            //int bytes = Math.Abs(bmpData.Stride) * h;
            //byte[] rgbValues = new byte[bytes];
            //// 拷贝到字节数组
            //Marshal.Copy(ptr, rgbValues, 0, bytes);
            //bmp.UnlockBits(bmpData);
            //return rgbValues;


            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Bmp);
                return stream.ToArray();
            }
        }

        // byte数组转换为Bitmap
        public static Bitmap ByteArrayToBitmap(byte[] byteArray)
        {
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                return new Bitmap(stream);
            }
        }

    }
}
