using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CreoPost
{
    public enum LogLevel { Info, Important, Error }

    /// <summary>
    /// Some minimal interfaces for logging.
    /// </summary>
    public class Log
    {
        public delegate void LogDelegate(LogLevel level, string msg, params object[] args);

        public static void LogToConsole(LogLevel level, string msg, params object[] args)
        {
            System.Console.WriteLine(msg, args);
        }
    }
}
