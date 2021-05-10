using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Informatikprojekt_DotNetVersion
{
    public class ClientInfoCollector
    {
        private static double wattUsage;

        public static double getWattUsage()
        {
            return wattUsage;
        }
    }
}