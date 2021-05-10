using System;
using Informatikprojekt_DotNetVersion.Server;

namespace Informatikprojekt_DotNetVersion
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine("Start s for server and c for client");
            String s = Console.ReadLine();
            if (s.Equals("s"))
            {
                HostProgramm.Programm(args);
            } else if (s.Equals("c"))
            {
                ClientProgram.Programm(args);
            }
        }
    }
}