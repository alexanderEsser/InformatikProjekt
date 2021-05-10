using System;
using System.Management;
using System.Collections.Generic;
using System.Diagnostics;


namespace Informatikprojekt_DotNetVersion.Shared
{
    public class ClientDataReturn
    {
        public double performance;

        public List<double> timeTaken;

        public String clientName;


        public ClientDataReturn(String clientName, string performance)
        {
            this.clientName = clientName;
            this.performance = Convert.ToDouble(performance);
        }
        
        public ClientDataReturn(String clientName, string performance, List<double> timeTaken)
        {
            this.clientName = clientName;
            this.performance = Convert.ToDouble(performance);
            this.timeTaken = timeTaken;
        }

        
    }
}