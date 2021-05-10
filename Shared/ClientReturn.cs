using System;
using System.IO;
using System.Runtime.Serialization;

namespace Informatikprojekt_DotNetVersion.Shared
{ 
    [Serializable]
    public class ClientReturn : ISerializable
    {
        public int numberToCheck;
        public bool isPrime;
        public String name;
        public long time;
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return numberToCheck + "|" + isPrime + "|" + name + "|" + time;
        }

        public ClientReturn()
        {
            
        }

        public ClientReturn(String s)
        {
            String[] n = s.Split("|");
            numberToCheck = Convert.ToInt32(n[0]);
            isPrime = Convert.ToBoolean(n[1]);
            name = n[2];
            time = Convert.ToInt64(n[3]);
        }
        
    }
}