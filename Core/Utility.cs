using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class Utility
    {
        /// <summary> [HH:mm:ss]꼴 시간 스트링 </summary>
        public static string GetTimeStamp()
        {
            return DateTime.Now.ToString("[HH:mm:ss]");
        }
        public static void PrintServerTitle()
        {
            string str =
                "■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■\n" +
                "■　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　■\n" +
                "■　　■■■　■■■　■■■■　■　　　■　■■■　■■■■　■\n" +
                "■　■　　　　■　　　■　　■　■　　　■　■　　　■　　■　■\n" +
                "■　　■■　　■■■　■■■　　■　　　■　■■■　■■■　　■\n" +
                "■　　　　■　■　　　■　　■　　■　■　　■　　　■　　■　■\n" +
                "■　■■■　　■■■　■　　■　　　■　　　■■■　■　　■　■\n" +
                "■　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　■\n" +
                "■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■\n";
            Console.WriteLine(str);
        }
        public static void PrintClientTitle()
        {
            string str =
                "■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■\n" +
                "■　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　■\n" +
                "■　　■■■　■　　　■■■　■■■　■　　　■　■■■■■　■\n" +
                "■　■　　　　■　　　　■　　■　　　■■　　■　　　■　　　■\n" +
                "■　■　　　　■　　　　■　　■■■　■　■　■　　　■　　　■\n" +
                "■　■　　　　■　　　　■　　■　　　■　　■■　　　■　　　■\n" +
                "■　　■■■　■■■　■■■　■■■　■　　　■　　　■　　　■\n" +
                "■　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　■\n" +
                "■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■\n";
            Console.WriteLine(str);
        }
    }
}
