using System;

namespace Informatikprojekt_DotNetVersion.Shared.Util
{
    public class Prime
    {
        private string clientName;
        private int number;
        public bool isPrime;
        private long timeToCalculate;

        public Prime(int number)
        {
            this.number = number;
            this.isPrime = false;
            this.timeToCalculate = 0;
        }

        public Prime(string FromString)
        {
            string[] s = FromString.Split(',');
            this.clientName = s[0];
            this.number = Convert.ToInt32(s[1]);
            this.isPrime = Convert.ToBoolean(s[2]);
            this.timeToCalculate = Convert.ToInt64(s[3]);
        }

        public Prime(string clientName, int number, bool isPrime, long timeToCalculate)
        {
            this.clientName = clientName;
            this.number = number;
            this.isPrime = isPrime;
            this.timeToCalculate = timeToCalculate;
        }

        public int getNumber()
        {
            return number;
        }
        
        public void setTimeToCalculate(long time)
        {
            this.timeToCalculate = time;
        }
        
        public long setTimeToCalculate()
        {
            return timeToCalculate;
        }
        
        public void setClientName(string name)
        {
            this.clientName = name;
        }
        
        public string getClientName()
        {
            return clientName;
        }

        public override string ToString()
        {
            return this.clientName + "," + number + "," + isPrime + "," + timeToCalculate;
        }

        public static string ArrayToString(Prime[] array)
        {
            string temp = "";
            for (int i = 0; i < array.Length; i++)
            {
                temp += array[i].ToString() + "\n";
            }
            return temp;
        }
    }
}