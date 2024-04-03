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

namespace A8_TEST
{
    public partial class FormMain : Form
    {
        tstRtmp rtmp = new tstRtmp();
        Rtsplz rtsplz = new Rtsplz();
        Thread thPlayer;
        Thread thPlayerlz;
        SynchronizationContext m_SyncContext = null; //获取上下文

        List<string> ipLists = new List<string>();
        List<A8SDK> a8Lists = new List<A8SDK>();
        //Thread thPreview;
        //VideoCapture videoCapture = new VideoCapture();

        //Mat IRframe = new Mat();
        //Mat IRmgMatShow = new Mat();
        //private Bitmap IRProspectImage;
        private bool saveVideoFlag = false;

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

            Console.WriteLine("");

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
        //private void SaveVideoThread()
        //{

           
        //    string videoStreamAddress = "rtsp://192.168.100.2/webcam";
        //    videoCapture.Open(videoStreamAddress);

        //    VideoWriter writer = new VideoWriter("output_video.avi", FourCC.XVID, videoCapture.Fps, new OpenCvSharp.Size(videoCapture.FrameWidth, videoCapture.FrameHeight), true);
        //    Console.WriteLine(writer.IsOpened());

        //    if (videoCapture.IsOpened())
        //    {
                
        //        int time = Convert.ToInt32(Math.Round(1000/videoCapture.Fps));
        //        while (true)
        //        {
        //            Thread.Sleep(time);
        //            if (cts.Token.IsCancellationRequested)
        //            {
        //                IRframe = null;
        //                return;
        //            }
        //            videoCapture.Read(IRframe);

        //            if (IRframe.Empty())
        //            {
        //                continue;
        //            }

        //            if (saveVideoFlag)
        //            {
                       
        //                writer.Write(IRframe);
        //            }
        //        }
        //    }

        //    ////Thread.Sleep(100);
        //    //while (true)
        //    //{
        //    //    if (videoCapture.IsOpened())
        //    //    {
        //    //        break;
        //    //    }
        //    //    else
        //    //    {
        //    //        Console.WriteLine("打开红外失败");
        //    //        videoCapture.Open(videoStreamAddress);
        //    //    }

        //    //    Thread.Sleep(100);
        //    //}

        //    //while (videoCapture.IsOpened())
        //    //{
        //    //    bool read_visual_success = videoCapture.Read(IRframe);
        //    //    if (read_visual_success)
        //    //    {
        //    //        //Console.WriteLine("红外" + IRframe.Empty());
        //    //        if (IRframe.Height == 0)
        //    //        {

        //    //            videoCapture.Open(videoStreamAddress);
        //    //            continue;
        //    //        }

        //    //        //OpenCvSharp.Size size = new OpenCvSharp.Size(pic.Width, pic.Height);
        //    //        //Cv2.Resize(IRframe, IRmgMatShow, size, 0, 0, InterpolationFlags.Cubic);

        //    //        VideoWriter writer = new VideoWriter("output_video.avi", FourCC.XVID, 20.0, new OpenCvSharp.Size(IRframe.Width, IRframe.Height), true);


        //    //        // 读取视频帧并写入到输出视频中


        //    //        // 写入帧到输出视频
        //    //        writer.Write(IRframe);
        //    //    }



        //    //}

        //}

        //public void ShowIRImageThreadProc()
        //{
        //    string videoStreamAddress = "rtsp://192.168.100.2/webcam";
        //    videoCapture.Open(videoStreamAddress);
        //    //Thread.Sleep(100);
        //    while (true)
        //    {
        //        if (videoCapture.IsOpened())
        //        {
        //            break;
        //        }
        //        else
        //        {
        //            Console.WriteLine("打开红外失败");
        //            videoCapture.Open(videoStreamAddress);
        //        }

        //        Thread.Sleep(100);
        //    }
        //    int i = 0;
        //    while (videoCapture.IsOpened())
        //    {
        //        bool read_visual_success = videoCapture.Read(IRframe);
        //        if (read_visual_success)
        //        {
        //            //Console.WriteLine("红外" + IRframe.Empty());
        //            if (IRframe.Height == 0)
        //            {

        //                videoCapture.Open(videoStreamAddress);
        //                continue;
        //            }

        //            OpenCvSharp.Size size = new OpenCvSharp.Size(pic.Width, pic.Height);
        //            Cv2.Resize(IRframe, IRmgMatShow, size, 0, 0, InterpolationFlags.Cubic);

        //            IRProspectImage = (Bitmap)IRmgMatShow.ToBitmap().Clone();

        //            //if (i >= 100)
        //            //{
        //            //    byte[] bytes = readBitmapToBytes(VisualProspectImages[(int)deviceNum]);

        //            //    dataArrayList[(int)deviceNum].AddLast(bytes);
        //            //    if (dataArrayList[(int)deviceNum].Count >= 3)
        //            //    {
        //            //        dataArrayList[(int)deviceNum].RemoveFirst();
        //            //    }
        //            //    //VisualProspectImageCpoy = VisualProspectImage;
        //            //    i = 0;
        //            //}


        //            OpenCvSharp.Point cor;
        //            OpenCvSharp.Point corEnd;

        //            // Console.WriteLine("picturboxType" + picturboxType);
        //            //if (bIfGetEnd == true && picturboxType == 1 && (int)deviceNum == 0) //正在画矩形
        //            //{
        //            //    cor.X = iNowPaint_X_Start;
        //            //    cor.Y = iNowPaint_Y_Start;
        //            //    corEnd.X = iNowPaint_X_End;
        //            //    corEnd.Y = iNowPaint_Y_End;

        //            //    Cv2.Rectangle(VisualmgMatShows[(int)deviceNum], cor, corEnd, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);
        //            //    VisualProspectImages[(int)deviceNum] = (Bitmap)VisualmgMatShows[(int)deviceNum].ToBitmap().Clone();
        //            //}

        //            //if (bIfGetEnd == true && picturboxType == 2 && (int)deviceNum == 1) //正在画矩形
        //            //{
        //            //    cor.X = iNowPaint_X_Start;
        //            //    cor.Y = iNowPaint_Y_Start;
        //            //    corEnd.X = iNowPaint_X_End;
        //            //    corEnd.Y = iNowPaint_Y_End;

        //            //    Cv2.Rectangle(VisualmgMatShows[(int)deviceNum], cor, corEnd, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);
        //            //    VisualProspectImages[(int)deviceNum] = (Bitmap)VisualmgMatShows[(int)deviceNum].ToBitmap().Clone();
        //            //}

        //            //if (iTick == 1 && (int)deviceNum == 0)
        //            //{
        //            //    Console.WriteLine("保存图像");

        //            //    try
        //            //    {
        //            //        string strFileName = System.Windows.Forms.Application.StartupPath + "test.bmp";
        //            //        Cv2.ImWrite(@"D:\test.bmp", VisualmgMatShows[(int)deviceNum]);
        //            //        iTick = 0;
        //            //    }
        //            //    catch(Exception e)
        //            //    {
        //            //        Console.WriteLine(e.ToString());
        //            //    }

        //            //}

        //            //if(flag == false)
        //            //{
        //            //flag = true;

        //            //    cor.X = 0;
        //            //    cor.Y = 0;
        //            //    corEnd.X = 100;
        //            //    corEnd.Y = 100;


        //            //    Cv2.Rectangle(VisualmgMatShows[(int)deviceNum], cor, corEnd, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);
        //            //}



        //            //byte[] bytes = readBitmapToBytes(VisualProspectImage);
        //            ////Console.WriteLine(bytes.Length);

        //            //Console.WriteLine("红外图像数组长度" + bytes.Length);

        //            //string path = "E:\\test.jpg";
        //            //VisualProspectImage.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);


        //            //Invalidate();
        //            //visual_pictureBox.Invalidate();

        //            pic.Image = IRProspectImage;
        //            pic.Refresh();

        //            //if (i == 100)
        //            //{
        //            //dataArrayList.AddLast(bytes);
        //            //if (dataArrayList.Count >= 3)
        //            //{
        //            //    dataArrayList.RemoveFirst();
        //            //}
        //            //    i = 0;
        //            //}
        //            IRProspectImage = null;
        //            Thread.Sleep(10);

        //            Application.DoEvents();
        //            GC.Collect();

        //        }
        //    }

        //}


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
                VideoWriter writer = new VideoWriter("output_video.avi", FourCC.XVID,25, new OpenCvSharp.Size(768, 576), true);

                // 更新图片显示
                tstRtmp.ShowBitmap show = (bmp) =>
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.pic.Image = bmp;

                        using (Graphics gfx = Graphics.FromImage(bmp))
                        {
                            // 设置文字的格式
                            Font font = new Font("Arial", 12);
                            Brush brush = Brushes.Black;

                            // 在Bitmap上绘制文字
                            //gfx.DrawString("123", font, brush, 100, 100);

                        }

                        // 保存Bitmap到文件
                        //bmp.Save(@"C:\Users\Dell\Desktop\1.png", System.Drawing.Imaging.ImageFormat.Png);

                             if (saveVideoFlag)
                            {
                                Console.WriteLine(writer.IsOpened());
                                Mat mat = Bitmap2Mat(bmp);
                                writer.Write(mat);
                            }
                       

                        A8SDK.globa_temp globa_Temp;

                        a8Lists[0].Get_globa_temp(out globa_Temp);
                        Console.WriteLine(globa_Temp.max_temp);

                        if (oldBmp != null)
                        {
                            oldBmp.Dispose();
                        }
                        oldBmp = bmp;
                    }));
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
                    button1.Text = "开始播放";
                    button1.Enabled = true;
                }));
            }
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
    }
}
