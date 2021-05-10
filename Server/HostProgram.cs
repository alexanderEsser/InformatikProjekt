using System;
using System.Collections.Generic;
using Informatikprojekt_DotNetVersion.Shared.Util;

namespace Informatikprojekt_DotNetVersion.Server
{
    public class HostProgramm
    {
        public static void Programm(string[] args)
        {
            Config config = Config.readConfigFromCLIArgs(args);
            Host host = new Host(config.hostIP, config.username, config.password, config.port);
            List<String> s = new List<String>(PrimeUtil.generateNumberRows(50000000));
            Console.WriteLine(s.Count);
            host.startTaskExecution(s);
        }
    }
}