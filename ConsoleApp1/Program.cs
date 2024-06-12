using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {

            long timestamp = DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;
            long timestamp5 = DateTime.UtcNow.Ticks/ TimeSpan.TicksPerMillisecond;



            long timestamp3 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            long timestamp2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long timestamp4 = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            long timestamp1 = GetTimestamp();

           
            DateTime a = GetDateTimeMilliseconds(timestamp5);

            byte[] bits = BitConverter.GetBytes(500);



        }

        /// <summary>
        /// 13位时间戳转 日期格式   1652338858000 -> 2022-05-12 03:00:58
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeMilliseconds(long timestamp)//时间戳转日期
        {
            long begtime = timestamp * 10000;
            DateTime dt_1970 = new DateTime(1970, 1, 1, 0, 0, 0);
            long tricks_1970 = dt_1970.Ticks;//1970年1月1日刻度
            long time_tricks = tricks_1970 + begtime;//日志日期刻度
            DateTime dt = new DateTime(time_tricks);//转化为DateTime
            return dt;
        }


        /// <summary>
        /// 13位时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetTimestamp()//获取当前日期时间戳
        {
            TimeSpan ts = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1,0,0,0);
            return (long)ts.TotalMilliseconds;
        }
    }
}
