using a8sdk;
using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
namespace A8_TEST
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        A8SDK a8;


        private void Button1_Click(object sender, EventArgs e)
        {
            A8SDK.SDK_initialize();
            //a8 = new A8SDK("192.168.100.2");

        }

        private void button2_Click(object sender, EventArgs e)
        {

            //string device_List = A8SDK.SDK_serch_device(100);
            string device_List;
            int i;
            i= A8SDK.SDK_serch_device(1024, out device_List);
            if (i==0)
            {
                Console.WriteLine("执行返回： " + i.ToString());
                //System.IO.File.WriteAllText(@"D:\1.txt", device_List);
                MessageBox.Show(device_List);
            }
            MessageBox.Show(i.ToString());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //int i = a8.Color_plate;
            //Console.WriteLine("颜色板： " + i.ToString());
            ////i++;

            //a8.Color_plate = 3;
            //MessageBox.Show(a8.Color_plate.ToString());
            int i;
            int a;
            a8.Set_color_plate(0);

            a = a8.Get_color_plate(out i);

            Console.WriteLine(a.ToString());
            MessageBox.Show(i.ToString());


        }

        private void button4_Click(object sender, EventArgs e)
        {
            //int i = a8.Set_led;

            //if (i == 0)
            //{
            //    a8.Set_led = 1;
            //}
            //else
            //{
            //    a8.Set_led = 0;
            //}
            //i = a8.Set_led;
            //MessageBox.Show(i.ToString());
            //A8SDK.network_eth network_Eth = a8.Network_eth;

            a8.Get_led(out int i);
            if (i == 0)
            {
                a8.Set_led(1);
            }
            else
            {
                a8.Set_led(0);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            a8.Shutter_correction();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int i;
            int a;

            //设定自动快门切换时间
            //a8.Shutter_times = 0;//(范围30-1800s否则无效关闭自动切换)
            //i = a8.Shutter_times;
            //MessageBox.Show(i.ToString());
            a8.Set_shutter_auto_correction(0);

            Delay(300);

            a = a8.Get_shutter_auto_correction(out i);

            Console.WriteLine("执行返回： " + a.ToString());
            MessageBox.Show(i.ToString());

            

        }
        public static void Delay(int milliSecond)
        {
            int start = Environment.TickCount;
            while (Math.Abs(Environment.TickCount - start) < milliSecond)//毫秒
            {
                Application.DoEvents();//可执行某无聊的操作
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //设定镜像模式

            //int i;
            ////i= a8.Mirror_mode;
            ////MessageBox.Show(i.ToString());
            //a8.Mirror_mode = 2;

            //i = a8.Mirror_mode;
            //MessageBox.Show(a8.Mirror_mode.ToString());
            //Console.WriteLine("镜像模式： " + a8.Mirror_mode.ToString());


            int a;
            a8.Set_video_mirror(2);

            Delay(300);
            a = a8.Get_video_mirror(out int i);

            Console.WriteLine("执行返回： " + a.ToString());
            MessageBox.Show(i.ToString());

        }

        private void button8_Click(object sender, EventArgs e)
        {
            //设定视频模式
            //int i;
            //i = a8.Video_mode;
            //Console.WriteLine("视频模式修改前： " + i.ToString());
            ////i++;
            //a8.Video_mode = 3;
            //i = a8.Video_mode;

            //MessageBox.Show(i.ToString());
            //Console.WriteLine("视频模式修改后： " + i.ToString());
            int a;
            a8.Set_video_mode(2);

            Delay(300);
            a = a8.Get_video_mode(out int i);

            Console.WriteLine("执行返回： " + a.ToString());
            MessageBox.Show(i.ToString());


        }




        private void button9_Click(object sender, EventArgs e)
        {
            //设定测温区域位置
            int i;
            a8sdk.A8SDK.area_pos area_data;

            area_data.enable = 1;
            area_data.height = 100;
            area_data.width = 100;
            area_data.x = 100;
            area_data.y = 100;
            i = a8.Set_area_pos(1, area_data);

            Console.WriteLine("执行返回： " + i.ToString());
            //MessageBox.Show(i.ToString());
        }

        private void button10_Click(object sender, EventArgs e)
        {

            //area_data = a8.Get_area_pos(1);

            //Console.WriteLine("enable: " + area_data.enable.ToString() + "height:" + area_data.height.ToString() + "width:" + area_data.width.ToString() + "x:" + area_data.x.ToString() + "y:" + area_data.y.ToString());

            int i;
            i = a8.Get_area_pos(1, out A8SDK.area_pos area_data);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine("enable: " + area_data.enable.ToString() + "height:" + area_data.height.ToString() + "width:" + area_data.width.ToString() + "x:" + area_data.x.ToString() + "y:" + area_data.y.ToString());


        }

        private void button11_Click(object sender, EventArgs e)
        {
            //设置单点测温位置
            int i;
            a8sdk.A8SDK.spot_pos spot_Pos;
            spot_Pos.enable = 1;
            spot_Pos.x = 200;
            spot_Pos.y = 100;
            i = a8.Set_spot_pos(1, spot_Pos);
            MessageBox.Show(i.ToString());
            Console.WriteLine("执行返回： " + i.ToString());

        }

        private void button12_Click(object sender, EventArgs e)
        {
            //获取单点测试位置
            int i;
            a8sdk.A8SDK.spot_pos spot_Pos;
            //spot_Pos = a8.Get_spot_pos(2);
            i = a8.Get_spot_pos(2, out spot_Pos);

            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine("enable: " + spot_Pos.enable.ToString() + "x:" + spot_Pos.x.ToString() + "y:" + spot_Pos.y.ToString());
        }

        private void button13_Click(object sender, EventArgs e)
        {
            //设置温线位置
            int i;
            a8sdk.A8SDK.line_pos line_Pos;
            a8sdk.A8SDK.line_pos line_Pos1;
            line_Pos.enable = 1;
            line_Pos.sta_x = 100;
            line_Pos.end_x = 300;
            line_Pos.sta_y = 100;
            line_Pos.end_y = 300;

            //a8.Line_pos = line_Pos;
            a8.Set_line_pos(line_Pos);
            Delay(300);

            i = a8.Get_line_pos(out line_Pos1);

            Console.WriteLine("执行返回： " + i.ToString());
            //Console.WriteLine("enable: " + a8.Line_pos.enable.ToString() + "sta_x:" + a8.Line_pos.sta_x.ToString() + "end_x:" + a8.Line_pos.end_x.ToString() + "sta_y:" + a8.Line_pos.sta_y.ToString() + "end_y:" + a8.Line_pos.end_y.ToString());
            Console.WriteLine("enable: " + line_Pos1.enable.ToString() + "sta_x:" + line_Pos1.sta_x.ToString() + "end_x:" + line_Pos1.end_x.ToString() + "sta_y:" + line_Pos1.sta_y.ToString() + "end_y:" + line_Pos1.end_y.ToString());

        }

        private void button14_Click(object sender, EventArgs e)
        {
            //获取所有测温元件位置
            int i;
            //image_Pos = a8.All_pos;
            i = a8.Get_all_pos(out A8SDK.image_pos image_Pos);
            Console.WriteLine("执行返回： " + i.ToString());

        }

        private void button15_Click(object sender, EventArgs e)
        {
            //设置扩展温度段开闭
            //int i = a8.Temp_range;
            //MessageBox.Show(i.ToString());

            //a8.Temp_range = 0;

            //i = a8.Temp_range;
            //Console.WriteLine("设置扩展温度段开闭： " + i.ToString());

            int i;
            a8.Set_temp_range(1);
            Delay(300);
            i = a8.Get_temp_range(out int j);

            Console.WriteLine("执行返回： " + i.ToString());
            MessageBox.Show(j.ToString());
        }

        private void button16_Click(object sender, EventArgs e)
        {
            //设置融合图像X轴偏移值
            int i;
            a8.Set_video_isp_x_offset(1);
            Delay(300);
            i = a8.Get_video_isp_x_offset(out int j);
            Console.WriteLine("执行返回： " + i.ToString());
            MessageBox.Show(j.ToString());
        }

        private void button17_Click(object sender, EventArgs e)
        {
            //设置融合图像Y轴偏移值
            //i = a8.Video_isp_y_offset;

            int i;
            a8.Set_video_isp_y_offset(1);
            Delay(300);
            i = a8.Get_video_isp_y_offset(out int j);
            Console.WriteLine("执行返回： " + i.ToString());
            MessageBox.Show(j.ToString());
        }

        private void button18_Click(object sender, EventArgs e)
        {
            //设置融合图像X轴缩放比例
            //int i;
            //i = a8.Video_isp_x_scale;

            int i;
            a8.Set_video_isp_x_scale(1);
            Delay(300);
            i = a8.Get_video_isp_x_scale(out int j);
            Console.WriteLine("执行返回： " + i.ToString());
            MessageBox.Show(j.ToString());
        }

        private void button19_Click(object sender, EventArgs e)
        {
            //设置融合图像y轴缩放比例
            //int i;
            //i = a8.Video_isp_y_scale;

            int i;
            a8.Set_video_isp_y_scale(2);
            Delay(300);
            i = a8.Get_video_isp_y_scale(out int j);
            Console.WriteLine("执行返回： " + i.ToString());
            MessageBox.Show(j.ToString());
        }

        private void button20_Click(object sender, EventArgs e)
        {
            //设置email服务
            int i;
            i = a8.Get_email_server(out A8SDK.email_server email_Server);
            //email_Server = a8.Email_server;
            Console.WriteLine("执行返回： " + i.ToString());
            MessageBox.Show(new string(email_Server.recv_addr));
        }

        private void button21_Click(object sender, EventArgs e)
        {
            //设置tftp服务
            int i;
            a8sdk.A8SDK.tftp_server tftp_Server;
            //tftp_Server = a8.Tftp_server;

            i = a8.Get_tftp_server(out tftp_Server);
            Console.WriteLine("执行返回： " + i.ToString());
            string s = "";
            s = new string(tftp_Server.tftp_addr);

            int index = s.IndexOf("\0");
            s = s.Remove(index);


            Console.WriteLine(s);

            Console.WriteLine(tftp_Server.enable.ToString());




        }

        private void button22_Click(object sender, EventArgs e)
        {
            //设置网络连接相关信息
            int i;
            a8sdk.A8SDK.network_eth network_Eth;
            //network_Eth = a8.Network_eth;
            i = a8.Get_network_eth(out network_Eth);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(new string(network_Eth.netmask));

        }

        private void button24_Click(object sender, EventArgs e)
        {
            //设置融合距离
            int i;
            int j;
            //i = a8.Fusion_distance;
            i = a8.Get_fusion_distance(out j);
            Console.WriteLine("执行返回： " + i.ToString());
            MessageBox.Show(j.ToString());

        }

        private void button25_Click(object sender, EventArgs e)
        {
            //设置单个对象环境参数
            int i;
            a8sdk.A8SDK.envir_param envir_Param;
            //例子,结构体和之前C++的一致
            envir_Param.airTemp = 10f;//
            envir_Param.atmosTrans = 10f;
            envir_Param.distance = 10f;
            envir_Param.emissivity = 10f;
            envir_Param.infraredRadia = 10f;
            envir_Param.infraredTemp = 10f;
            envir_Param.method = 1;
            envir_Param.num = 1;
            envir_Param.targetTemp = 10f;
            i = a8.Set_envir_param(envir_Param);

            Console.WriteLine("执行返回： " + i.ToString());


        }

        private void button26_Click(object sender, EventArgs e)
        {
            //获取单个对象环境参数
            int i;
            a8sdk.A8SDK.envir_param envir_Param;
            //envir_Param = a8.Get_area_envir_param(0);
            i = a8.Get_area_envir_param(0, out envir_Param);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(envir_Param.method.ToString());
        }

        private void button27_Click(object sender, EventArgs e)
        {
            //获取单个测温点环境参数
            int i;
            a8sdk.A8SDK.envir_param envir_Param;
            //envir_Param = a8.Get_spot_envir_param(0);
            i = a8.Get_spot_envir_param(0,out envir_Param);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(envir_Param.method.ToString());
        }

        private void button28_Click(object sender, EventArgs e)
        {
            //获取测温线环境参数
            int i;
            a8sdk.A8SDK.envir_param envir_Param;
            //envir_Param = a8.Get_line_envir_param();
            i = a8.Get_line_envir_param(out envir_Param);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(envir_Param.method.ToString());
        }

        private void button23_Click(object sender, EventArgs e)
        {
            //获取全局环境参数
            int i;
            a8sdk.A8SDK.envir_param envir_Param;
            //envir_Param = a8.Get_globa_envir_param();
            i = a8.Get_globa_envir_param(out envir_Param);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(envir_Param.method.ToString());
        }

        private void button29_Click(object sender, EventArgs e)
        {
            //设置单个对象报警参数
            int i;
            a8sdk.A8SDK.alarm_param alarm_Param;
            //例子,结构体和之前C++的一致
            alarm_Param.active = 1;
            alarm_Param.captrue = 0;
            alarm_Param.condition = 0;
            alarm_Param.digital = 0;
            alarm_Param.disableCalib = 0;
            alarm_Param.email = 0;
            alarm_Param.ftp = 2;
            alarm_Param.hysteresis = 0;
            alarm_Param.method = 3;
            alarm_Param.num = 0;
            alarm_Param.threshold = 0;
            alarm_Param.thresholeTime = 0;

            i = a8.Set_alarm_param(alarm_Param);
            Console.WriteLine("执行返回： " + i.ToString());
        }

        private void button30_Click(object sender, EventArgs e)
        {
            //获取单个对象报警参数
            int i;
            a8sdk.A8SDK.alarm_param alarm_Param;
            //alarm_Param = a8.Get_area_alarm_param(1);
            i = a8.Get_area_alarm_param(0,out alarm_Param);
            int a;
            a = alarm_Param.ftp;
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(alarm_Param.method.ToString());

        }

        private void button31_Click(object sender, EventArgs e)
        {
            //获取单个测温点报警信息
            int i;
            a8sdk.A8SDK.alarm_param alarm_Param;
            //alarm_Param = a8.Get_spot_alarm_param(1);
            i = a8.Get_spot_alarm_param(1, out alarm_Param);
            Console.WriteLine("执行返回： " + i.ToString());
            //Console.WriteLine(alarm_Param.method.ToString());
            Console.WriteLine(alarm_Param.ftp.ToString());
        }

        private void button32_Click(object sender, EventArgs e)
        {
            //获取测温线报警信息
            int i;
            a8sdk.A8SDK.alarm_param alarm_Param;
            //alarm_Param = a8.Get_line_alarm_param();
            i = a8.Get_line_alarm_param(out alarm_Param);
            int a;
            a = alarm_Param.ftp;
            Console.WriteLine("执行返回： " + i.ToString());
            //Console.WriteLine(alarm_Param.method.ToString());
            Console.WriteLine(alarm_Param.ftp.ToString());

        }

        private void button33_Click(object sender, EventArgs e)
        {
            //获取单个测温区温度信息
            int i;
            a8sdk.A8SDK.area_temp area_Temp;
            //area_Temp = a8.Get_area_temp(1);
            i = a8.Get_area_temp(1,out area_Temp);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(area_Temp.min_temp.ToString());

        }

        private void button34_Click(object sender, EventArgs e)
        {
            //获取单个测温点温度信息
            int i;
            a8sdk.A8SDK.spot_temp spot_Temp;
            //spot_Temp = a8.Get_spot_temp(1);
            i = a8.Get_spot_temp(1, out spot_Temp);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(spot_Temp.temp.ToString());

        }

        private void button35_Click(object sender, EventArgs e)
        {
            //获取测温线温度信息
            int i;
            a8sdk.A8SDK.line_temp line_Temp;
            //line_Temp = a8.Get_line_temp();
            i = a8.Get_line_temp(out line_Temp);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(line_Temp.min_temp.ToString());
        }

        private void button36_Click(object sender, EventArgs e)
        {
            //获取全局温度信息
            int i;
            a8sdk.A8SDK.globa_temp globa_Temp;
            //globa_Temp = a8.Get_globa_temp();
            i = a8.Get_globa_temp(out globa_Temp);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(globa_Temp.max_temp.ToString());
        }

        private void button37_Click(object sender, EventArgs e)
        {
            //获取所有温度信息
            int i;
            a8sdk.A8SDK.image_temp image_Temp;
            //image_Temp = a8.Get_all_temp();
            i = a8.Get_all_temp(out image_Temp);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(image_Temp.globa.min_temp.ToString());
        }

        private void button38_Click(object sender, EventArgs e)
        {
            //重启
            int i;
            i = a8.Power_reboot();
            Console.WriteLine("执行返回： " + i.ToString());
        }

        private void button39_Click(object sender, EventArgs e)
        {
            //参数初始化
            int i;
            i = a8.Param_recover();
            Console.WriteLine("执行返回： " + i.ToString());
        }

        private void button40_Click(object sender, EventArgs e)
        {
            //固件升级指令
            int i;
            i = a8.Update();
            Console.WriteLine("执行返回： " + i.ToString());
        }

        private void button41_Click(object sender, EventArgs e)
        {
            //心跳检测
            int i;
            i = a8.Heartbeat();
            Console.WriteLine("执行返回： " + i.ToString());
        }

        private void button42_Click(object sender, EventArgs e)
        {
            //串口参数配置指令
            int i;
            int nSpeed = 1;
            int nBits = 1;
            char nEvent = 'N';
            int nStop = 1;

            i = a8.Set_uart(nSpeed, nBits, nEvent, nStop);
            Console.WriteLine("执行返回： " + i.ToString());
        }

        private void button43_Click(object sender, EventArgs e)
        {
            //串口发送
            int i;
            byte[] bt = new Byte[32];
            i = a8.Send_uart_command(bt);
            Console.WriteLine("执行返回： " + i.ToString());
        }

        private void button44_Click(object sender, EventArgs e)
        {
            //自动聚焦
            int i;
            a8sdk.A8SDK.focus_param focus_Param;
            focus_Param.height = 1;
            focus_Param.method = 1;
            focus_Param.width = 1;
            focus_Param.x = 1;
            focus_Param.y = 1;
            i = a8.Autofocus(focus_Param);
            Console.WriteLine("执行返回： " + i.ToString());
        }

        private void button45_Click(object sender, EventArgs e)
        {
            //设置设备名
            int i;
            i = a8.Set_device_name("mrc123");
            Console.WriteLine("执行返回： " + i.ToString());
            MessageBox.Show(i.ToString());
        }

        private void button46_Click(object sender, EventArgs e)
        {
            //获取设备名称
            int i;
            string str = "";
            i = a8.Get_device_name(out str);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine("device_name： " + str);
            MessageBox.Show(str);
        }

        private void button47_Click(object sender, EventArgs e)
        {
            //获取探测器编号
            int i;
            string str;
            byte[] bt = new Byte[6];
            i = a8.Get_detect_number(out bt);
            //str = System.Text.Encoding.UTF8.GetString(bt);

            
            str= System.Text.Encoding.GetEncoding("GB2312").GetString(bt).TrimEnd('\0');

            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(str);
            MessageBox.Show(str);
        }

        private void button48_Click(object sender, EventArgs e)
        {
            //设置tcp温度
            int i;
            //sc = a8.Temp_frame;
            i = a8.Get_temp_frame(out char sc);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(Convert.ToString(sc));



        }

        private void button49_Click(object sender, EventArgs e)
        {
            //设置报警输出电平
            int i;
            //sc = a8.Alarm;
            i = a8.Get_alarm_in(out char sc);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine(Convert.ToString(sc));

        }

        private void button50_Click(object sender, EventArgs e)
        {
            //获取温补值
            int i;
            //i = a8.Comp_temp;
            i = a8.Get_comp_temp(out int j);
            Console.WriteLine("执行返回： " + i.ToString());
            Console.WriteLine("Comp_temp： " + j.ToString());
        }

        private void button51_Click(object sender, EventArgs e)
        {
            //设置设备当前时间
            int i;
            a8sdk.A8SDK.time_param time_Param;
            time_Param.year = Convert.ToChar("2021");
            time_Param.month = Convert.ToChar("04");
            time_Param.day = Convert.ToChar("30");
            time_Param.hour = Convert.ToChar("18");
            time_Param.minute = Convert.ToChar("30");
            time_Param.second = Convert.ToChar("30");
            i = a8.Set_time(time_Param);
            Console.WriteLine("执行返回： " + i.ToString());
        }

        private void button52_Click(object sender, EventArgs e)
        {
            int i;
            a8sdk.A8SDK.tftp_server tftp_Server;
            char[] s= new char[20];
            s[0] = '1';
            s[1] = '2';
            s[2] = '7';
            s[3] = '.';
            s[4] = '0';
            s[5] = '.';
            s[6] = '0';
            s[7] = '.';
            s[8] = '1';
            s[9] = (char)0;
            s[10] = (char)0;
            s[11] = (char)0;
            s[12] = (char)0;
            s[13] = (char)0;
            s[14] = (char)0;
            s[15] = (char)0;
            s[16] = (char)0;
            s[17] = (char)0;
            s[18] = (char)0;
            s[19] = (char)0;

            tftp_Server.enable = 0;

            tftp_Server.tftp_addr = s;

            i = a8.Set_tftp_server(tftp_Server);
            Console.WriteLine("执行返回： " + i.ToString());

            string str = "";
            str = new string(tftp_Server.tftp_addr);

            int index = str.IndexOf("\0");
            str = str.Remove(index);
            Console.WriteLine(str);

        }

        private void button53_Click(object sender, EventArgs e)
        {
            a8 = new A8SDK("192.168.100.2");
        }

        private void button54_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    //internal class StreamWriter
    //{

    //}
}

