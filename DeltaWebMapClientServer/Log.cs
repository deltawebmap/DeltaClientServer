using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMapClientServer
{
    public static class Log
    {
        private static void BaseLog(string level, ConsoleColor color, string topic, string msg)
        {
            string output = $"{level}> [{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}] [{topic}] {msg}";

            Console.ForegroundColor = color;
            Console.WriteLine(output);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Information log. Low level.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="msg"></param>
        public static void I(string topic, string msg)
        {
            BaseLog("I", ConsoleColor.White, topic, msg);
        }

        /// <summary>
        /// Information log. Medium level.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="msg"></param>
        public static void W(string topic, string msg)
        {
            BaseLog("W", ConsoleColor.Yellow, topic, msg);
        }

        /// <summary>
        /// Error log. High level.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="msg"></param>
        public static void E(string topic, string msg)
        {
            BaseLog("E", ConsoleColor.Red, topic, msg);
        }
    }
}
