using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Xml.Linq;
using Informatikprojekt_DotNetVersion.Shared;
using Informatikprojekt_DotNetVersion.Shared.Util;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Informatikprojekt_DotNetVersion.Server
{
    //import static util.RabbitMQUtils.PRODUCER_EXCHANGE_NAME;

    public class Scheduler
    {
        // private readonly ConcurrentQueue<PrimeTask> openTasks = new ConcurrentQueue<PrimeTask>();
        public static readonly Queue<PrimeTask> openTasks = new Queue<PrimeTask>();
        public int anzahlClients;
        private bool lastTaskAssigned = true;
        private bool evaluateClients = false;

        // private static readonly ConcurrentStack<PrimeTask> currentlyExecutingTasks = new ConcurrentStack<PrimeTask>();
        public static List<PrimeTask> currentlyExecutingTasks = new List<PrimeTask>();
        public static List<string> missingTasks = new List<string>();

        //public static ConcurrentBag<PrimeTask> currentlyExecutingTasksBag = new ConcurrentBag<PrimeTask>();
        public static List<String> AllTasks = new List<String>();
        public static List<string> checkList = new List<string>();
        public static List<int> primZahlen = new List<int>();
        public static List<PrimeTask> incomingTask = new List<PrimeTask>();
        private List<RegisteredClient> sortedClients = new List<RegisteredClient>();

        private static int index = 0;

        // private readonly ConcurrentStack<PrimeTask> tasks = new ConcurrentStack<PrimeTask>();
        private static readonly List<PrimeTask> closedTasks = new List<PrimeTask>();
        public static List<long> times = new List<long>();
        public static int primes, zahlmes;
        bool listening = false;
        private ScheduleingStrategy strategy;

        public Scheduler(ScheduleingStrategy strategy)
        {
            this.strategy = strategy;
        }

        public void addTask(String numberRow)
        {
            openTasks.Enqueue(new PrimeTask(numberRow));
        }

        /**
     * Runs the scheduler once with the assigned strategy
     *
     * @param clients
     * @param channel
     * @throws IOException
     */
        public void scheduleTasks(List<RegisteredClient> clients, IModel channel)
        {
            switch (strategy)
            {
                case ScheduleingStrategy.EqualScheduleing:
                    scheduleTasksEqually(clients, channel);
                    break;
                case ScheduleingStrategy.WattScheduleing:
                    scheduleTasksByWattUsage(clients, channel);
                    break;
            }
        }

        public void scheduleTasksEqually(List<RegisteredClient> clients, IModel channel)
        {
            if (!listening)
            {
                listenForReturns(channel);
                listening = true;
            }

            List<RegisteredClient> availableClients = clients.OrderBy(o => o.tasksAssigned <= 10).ToList();


            PrimeTask temp;
            foreach (RegisteredClient client in availableClients)
            {
                if (openTasks.Count == 0)
                {
                    if (lastTaskAssigned)
                    {
                        foreach (RegisteredClient finishClient in availableClients)
                        {
                            assignAndStartTask(finishClient, new PrimeTask("0|finished"), channel);
                            Console.WriteLine(client.tasksAssigned + " send finish Task to " + finishClient.getName());
                        }
                    }

                    lastTaskAssigned = false;
                    break;
                }

                PrimeTask p = openTasks.Dequeue();
                assignAndStartTask(client, p, channel);
            }
        }

        public void PrimZahlen()
        {
            Console.WriteLine(primZahlen.Count);
        }

        int assignTasks = 0;

        public void startEvaluatingClients(List<RegisteredClient> clients)
        {
            double totalCPUPerformance = 0;
            int correctionValue;
            
            
            foreach (RegisteredClient client in clients)
            {
                if (client.getPerformance() < 5)
                {
                    client.setPerformance(5);
                }

                totalCPUPerformance += client.getPerformance();
            }
            
            sortedClients = clients.OrderBy(o => o.getPerformance()).ToList();
            anzahlClients = sortedClients.Count;
            
            foreach (RegisteredClient client in sortedClients)
            {
                Console.WriteLine(client.getPerformance() + " getPerformance " + client.getName());
                Console.WriteLine(totalCPUPerformance + " totalCPUPerformance " + client.getName());

                client.PercentageCpu = client.getPerformance() / totalCPUPerformance;
            }


            for (int j = 0; j < sortedClients.Count; j++)
            {
                sortedClients[j].ToAssigneTasks = Convert.ToInt32(Math.Floor(openTasks.Count * sortedClients[(sortedClients.Count - j) - 1].PercentageCpu));
                assignTasks += sortedClients[j].ToAssigneTasks;
            }

            
                correctionValue = openTasks.Count - assignTasks;

                if (correctionValue != 0)
                {
                    if (correctionValue < 0)
                    {
                        throw new Exception(correctionValue.ToString());
                    }

                    sortedClients[0].ToAssigneTasks += correctionValue;
                }
        }


        public void scheduleTasksByWattUsage(List<RegisteredClient> clients, IModel channel)
        {
            if (!listening)
            {
                listenForReturns(channel);
                listening = true;
            }

            if (openTasks.Count != 0)
            {
                if (!evaluateClients)
                {
                    startEvaluatingClients(clients);
                    evaluateClients = true;
                }
            }

            foreach (RegisteredClient client in sortedClients)
            {
                if (openTasks.Count == 0)
                {
                    if (lastTaskAssigned)
                    {
                        foreach (RegisteredClient finishClient in sortedClients)
                        {
                            Console.WriteLine(client.ToAssigneTasks+1 + " send finish Task to " + finishClient.getName());
                            assignAndStartTask(finishClient, new PrimeTask("0|finished"), channel);
                        }
                    }

                    lastTaskAssigned = false;
                    break;
                }
                
                if (client.ToAssigneTasks > 0)
                {
                    PrimeTask p = openTasks.Dequeue();
                    assignAndStartTask(client, p, channel);
                    client.ToAssigneTasks--;
                }
            }
        }


        public void percentClientValue(List<RegisteredClient> sortedClients, IModel channel)
        {
            double totalCPUPerformance = 0;
            int roundSumPerformance = 0;
            int correctionValue;

            foreach (RegisteredClient client in sortedClients)
            {
                totalCPUPerformance += client.getPerformance();
            }

            foreach (RegisteredClient client in sortedClients)
            {
                client.PercentageCpu = Convert.ToInt32(Math.Floor(client.getPerformance() / totalCPUPerformance));

                client.tasksAssigned = openTasks.Count * Convert.ToInt32(client.PercentageCpu);
            }

            correctionValue = 100 - roundSumPerformance;

            if (correctionValue != 0)
            {
                sortedClients[sortedClients.Count - 1].tasksAssigned += openTasks.Count;
            }
        }


        public void correctCPUpercent(List<RegisteredClient> sortedClients)
        {
            int roundSumPerformance = 0;

            foreach (RegisteredClient clients in sortedClients)
            {
                roundSumPerformance += Convert.ToInt32(Math.Floor(clients.getPerformance()));
            }
        }

        /**
     * Send task to client for execution
     *
     * @param client
     * @param task
     * @param channel
     * @throws IOException
     */
        private void assignAndStartTask(RegisteredClient client, PrimeTask task, IModel channel)
        {
            task.assignedClient = client;
            client.tasksAssigned++;

            currentlyExecutingTasks.Add(task);
            AllTasks.Add(task.taskID.ToString());

            channel.BasicPublish(RabbitMQUtils.PRODUCER_EXCHANGE_NAME, client.getProductionQueueName(), null,
                Encoding.UTF8.GetBytes(task.taskID + "|" + task.numberRowToCheck));
        }

        private void listenForReturns(IModel channel)
        {
            channel.BasicConsume(RabbitMQUtils.Queue.ConsumerDataReturnQueue.ToString(), true, "myConsumerTag2",
                new ClientReturnConsumer(channel));
        }

        private static List<string> slin = new List<string>();

        public class ClientReturnConsumer : DefaultBasicConsumer
        {
            private readonly IModel channel;

            public ClientReturnConsumer(IModel channel)
            {
                this.channel = channel;
            }

            public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered,
                string exchange, string routingKey,
                IBasicProperties properties, byte[] body)
            {
                int taskSum = 0;
                Prime[] primes = new Prime[PrimeUtil.ANZAHL_ZAHLEN_PRO_MESSAGE];

                string returnString = Encoding.UTF8.GetString(body);
                string[] temp = new string[2];

                temp = returnString.Split("|");


                string[] sLines = temp[1].Split("\n");

                for (int i = 0; i < sLines.Length - 1; i++)
                {
                    primes[i] = new Prime(sLines[i]);

                    if (primes[i].isPrime)
                    {
                        primZahlen.Add(primes[i].getNumber());
                    }


                    checkList.Add(primes[i].getNumber().ToString());
                }


                PrimeTask p = new PrimeTask(Convert.ToInt32(temp[0]), primes);
                incomingTask.Add(p);

                closedTasks.Add(p);

                slin.Add(temp[0]); //Es kommen alle Task an 
                if (incomingTask.Count % 10000 == 0)
                {
                    Console.WriteLine(incomingTask.Count);
                }
            }
        }


        public static void checkTask(string[] task, int taskSum)
        {
            int j = Convert.ToInt32(task[0]) * 1000;
            int checkSum = 1000;

            for (int i = j; i < j + 1000; i++)
            {
                checkSum += i;
            }

            if (!(checkSum.Equals(taskSum)))
            {
                Console.WriteLine("Something went wrong really wrong");
                Console.WriteLine(checkSum + " Checksumme");
                Console.WriteLine(taskSum + " Takssumme");
                Console.WriteLine(j + " TaskID");
                Console.ReadLine();
            }
        }

        public void ClosedTasks()
        {
            for (int j = 0; j < closedTasks.Count; j++)
            {
                for (int k = 0; k < closedTasks[j].primes.Length; k++)
                {
                    Console.WriteLine(closedTasks[j].primes[k].ToString());
                }
            }
        }

        public void CheckList()
        {

            using (StreamWriter sw =
                new StreamWriter(
                    "C:\\Users\\Alex\\RiderProjects\\InformatikprojektDotNetVersion\\Informatikprojekt DotNetVersion\\File.txt")
            )
            {
                for (int j = 0; j < currentlyExecutingTasks.Count; j++)
                {
                    sw.Write(currentlyExecutingTasks[j].taskID);
                    sw.WriteLine(currentlyExecutingTasks[j].completed);

                }

                using (StreamWriter sw2 =
                    new StreamWriter(
                        "C:\\Users\\Alex\\RiderProjects\\InformatikprojektDotNetVersion\\Informatikprojekt DotNetVersion\\File2.txt")
                )
                {
                    for (int j = 0; j < incomingTask.Count; j++)
                    {
                        sw2.WriteLine(incomingTask[j].taskID);
                    }
                }
            }
        }

        static bool allTrue(bool[] arr)
        {
            foreach (bool b in arr)
            {
                if (!b)
                {
                    return false;
                }
            }

            return true;
        }

        //TODO check shit again hier
        public static PrimeTask getCurrentlyExecutedTask(int id)
        {
            for (int j = 0; j < currentlyExecutingTasks.Count; j++)
            {
                if (currentlyExecutingTasks[j].taskID == id)
                {
                    return currentlyExecutingTasks[j];
                }
            }

            return null;
        }

        private int i = 0;
        private int q = 500;
        private int[] z = new int[1000];
        private int[] y = new int[1000];

        public bool tasksLeft()
        {
            // lock (currentlyExecutingTasks)
            {
                // Boolean isArrayEqual = true;
                //
                // //Console.WriteLine(openTasks.Count + " open tasks");
                // //Console.WriteLine(closedTasks.Count + " closed tasks");
                // z[i] = closedTasks.Count;
                // y[q] = closedTasks.Count;
                // i++;
                // q++;
                // q = q % 1000;
                // i = i % 1000;
                // if (z.Length == y.Length) {
                //     for (int i = 0; i < y.Length; i++) {
                //         if (y[i] != z[i]) {
                //             isArrayEqual = false;
                //         }
                //     }
                // } else {
                //     isArrayEqual = false;
                // }
                // if (isArrayEqual) {
                //     Console.WriteLine(currentlyExecutingTasks.Count + " curr");
                //     Console.WriteLine(openTasks.Count + " open");
                //     Console.WriteLine(checkList.Count + " check");
                //     Console.WriteLine(index + " index");
                //     CheckList();
                //     Console.WriteLine("fin");
                //     Console.ReadLine();
                // }


                if (checkList.Count == PrimeUtil.AnzahlZahlen)
                {
                    Console.WriteLine("Start checking");

                    List<PrimeTask> SortedList = incomingTask.OrderBy(o => o.taskID).ToList();


                    for (int j = 0; j < SortedList.Count; j++)
                    {
                        if (SortedList[i].taskID != j)
                        {
                            Console.WriteLine(SortedList[j].taskID + " missing incoming Task");
                            i--;
                        }

                        i++;
                    }


                    foreach (PrimeTask task in incomingTask.ToList())
                    {
                        foreach (PrimeTask currTask in currentlyExecutingTasks.ToList())
                        {
                            if (currTask.ContainsTaskID(task.taskID))
                            {
                                currentlyExecutingTasks.Remove(currTask);
                                task.completed = true;
                            }
                        }
                    }

                    Console.WriteLine(incomingTask.Count + " incoming Tasks");
                    Console.WriteLine(currentlyExecutingTasks.Count + " Currently Executed Task");
                }
                return currentlyExecutingTasks.Count != 0 || openTasks.Count != 0;
            }
        }


        public void GetTaskInfo()
        {

            Console.WriteLine(openTasks.Count + " OpenTasks");
            Console.WriteLine(checkList.Count + " CheckList");
            Console.WriteLine(slin.Count + " Ankommende Tasks");
            Console.WriteLine(closedTasks.Count + " geschlossene Tasks");
            Console.WriteLine(currentlyExecutingTasks.Count + " CurrTasks");
            Console.WriteLine(index + " index");
            Console.WriteLine(primZahlen.Count + " Anz. Primzahlen");
            
        }

        public enum ScheduleingStrategy
        {
            EqualScheduleing,
            WattScheduleing,
            PerformanceScheduleing
        }
    }
}