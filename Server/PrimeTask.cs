using System;
using System.Collections.Generic;
using Informatikprojekt_DotNetVersion.Shared.Util;

namespace Informatikprojekt_DotNetVersion.Server
{
    public class PrimeTask
    {
        public RegisteredClient assignedClient;
        public String numberRowToCheck;
        public int taskID;

        public int TaskId
        {
            get => taskID;
            set => taskID = value;
        }

        public bool completed = false;

        public Prime[] primes;


        public PrimeTask(String numberRowToCheck)
        {
            string[] temp = numberRowToCheck.Split("|");
            this.taskID = Convert.ToInt32(temp[0]);
            this.numberRowToCheck = temp[1];
            this.primes = primes;
        }

        public PrimeTask(int taskId, Prime[] primes)
        {
            this.taskID = taskId;
            this.primes = primes;
            completed = true;
        }

        public bool ContainsTaskID(int id)
        {
            if (taskID == id) return true;

            return false;

        }

        public int GetTaskID()
        {
            return taskID;
        }
    }
}