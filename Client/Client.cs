using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;
using Informatikprojekt_DotNetVersion.Shared;
using Informatikprojekt_DotNetVersion.Shared.Util;
using RabbitMQ.Client;
using System.Diagnostics;
using RabbitMQ.Client.Framing.Impl;

namespace Informatikprojekt_DotNetVersion
{
    public class Client
    {
        System.Timers.Timer dataTimer = new System.Timers.Timer();
        private Stopwatch watch;
        
        
        private List<double> powerList = new List<double>();

        private static int taskCounter = 0;
        public IModel channel;
        public String name;
        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        public double performance;
        private static bool noMoreTasks = false;
        private bool startTimer = true;
        private String timeTaken10000;
        private double timeCalc = 0;
        private double avgPower;

        public double AvgPower
        {
            get => avgPower;
            set => avgPower = value;
        }

        /**
     * @param rabbitMQHost IP-String for the RabbitMQ-Server
     * @param rabbitMQUser RabbitMQ-Username
     * @param rabbitMQPass RabbitMQ-Password
     * @param rabbitMQPort Port of the RabbitMQ-Server
     * @param clientName   Name of this client. Has to be unique in the host-client connection
     * @throws IOException
     */
        public Client(String rabbitMQHost, String rabbitMQUser, String rabbitMQPass, int rabbitMQPort,
            String clientName)
        {
            name = clientName;

            ConnectionFactory factory = new ConnectionFactory();


            factory.Uri = new Uri("amqp://" + rabbitMQUser + ":" + rabbitMQPass + "@" + rabbitMQHost + ":" +
                                  rabbitMQPort + "/");

            initializeRabbitMQConnection(factory);
            listenForTasks();
            notifyProducer();
        }

        /**
     * Triggers creations of all defaults
     *
     * @param factory
     * @throws IOException
     */
        private void initializeRabbitMQConnection(ConnectionFactory factory)
        {
            try
            {
                Console.WriteLine("Creating connection...");

                IConnection connection = factory.CreateConnection();
                Console.WriteLine("Connection created successfully");

                Console.WriteLine("Creating channel...");
                channel = connection.CreateModel();
                Console.WriteLine("Channel created successfully with number " + channel.ChannelNumber);

                RabbitMQUtils.CreateDefaultExchanges(channel);
                RabbitMQUtils.CreateDefaultQueues(channel);
                createClientQueue(channel);
            }
            catch (TimeoutException e)
            {
                Console.WriteLine("Timeout while trying to connect to the RabbitMQ server");
            }
        }

        /**
     * Notify producer that this client is now available
     *
     * @throws IOException
     */
        private void notifyProducer()
        {
            Console.WriteLine("Notifying publisher of creation...");
            channel.BasicPublish(RabbitMQUtils.CONSUMER_EXCHANGE_NAME.ToString(),
                RabbitMQUtils.Queue.ConsumerRegistrationQueue.ToString(), null,
                System.Text.Encoding.UTF8.GetBytes(name));
        }

        /**
     * Create the queue for sending data to this client
     *
     * @param channel
     * @throws IOException
     */
        private void createClientQueue(IModel channel)
        {
            Console.WriteLine("Declaring custom queue for data exchange...");
            channel.QueueDeclare(getProductionQueueName(), false, false, true, null);
            Console.WriteLine("Custom queue declared successfully");

            Console.WriteLine("Binding custom queue for data exchange...");
            channel.QueueBind(getProductionQueueName(), RabbitMQUtils.PRODUCER_EXCHANGE_NAME, getProductionQueueName());
            Console.WriteLine("Binding of custom queue completed successfully");

            triggerDataCollection();
        }

        public class TaskConsumer : DefaultBasicConsumer
        {
            private readonly IModel channel;
            private Client client;

            public TaskConsumer(IModel channel, Client client)
            {
                this.channel = channel;
                this.client = client;
            }

            public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered,
                string exchange, string routingKey,
                IBasicProperties properties, byte[] body)
            {
                IBasicProperties props = properties;

                Prime[] zahlen = new Prime[PrimeUtil.ANZAHL_ZAHLEN_PRO_MESSAGE];

                String numberToCheck = Encoding.UTF8.GetString(body);


                string[] temp = new string[2];


                try
                {
                    temp = numberToCheck.Split("|");

                    if (temp[1].Equals("finished"))
                    {
                        Console.WriteLine(client.AvgPower);
                        Console.WriteLine("Test3");
                        
                        client.watch.Stop();
                        double elapsedMs = client.watch.Elapsed.TotalMilliseconds;
                        Console.WriteLine("Time Taken: " + client.watch.Elapsed.TotalMilliseconds);

                        Console.WriteLine(client.name + "|" + client.performance + "|" + client.timeTaken10000);
                        client.timeTaken10000 = client.timeTaken10000 +
                                                (client.watch.Elapsed.TotalMilliseconds - client.timeCalc).ToString() + "; " + client.AvgPower;
                        
                        channel.BasicPublish(RabbitMQUtils.CONSUMER_EXCHANGE_NAME,
                            RabbitMQUtils.Queue.ConsumerInfoQueue.ToString(), null,
                            System.Text.Encoding.UTF8.GetBytes(client.name + "|" + client.performance + "|" + client.timeTaken10000));
                        
                    }
                    else
                    {
                        if (client.startTimer)
                        {
                            client.watch = Stopwatch.StartNew();
                            client.startTimer = false;
                        }
                            
                            
                        taskCounter++;

                        if (taskCounter % 10000 == 0)
                        {
                            client.timeTaken10000 = client.timeTaken10000 + (client.watch.Elapsed.TotalMilliseconds - client.timeCalc).ToString() + "; " + client.getAVGpower().ToString() + "; ";
                            

                            client.timeCalc = client.watch.Elapsed.TotalMilliseconds;

                            Console.WriteLine(client.timeTaken10000);
                            Console.WriteLine(taskCounter);
                        }

                        client.powerList.Add(client.performance);

                        string[] numbers = temp[1].Split(",");

                        var options = new ParallelOptions()
                        {
                            MaxDegreeOfParallelism = 100
                        };

                        Task<Prime>[] taskarray = new Task<Prime>[PrimeUtil.ANZAHL_ZAHLEN_PRO_MESSAGE];

                        Parallel.For(0, PrimeUtil.ANZAHL_ZAHLEN_PRO_MESSAGE, options, count =>
                        {
                            zahlen[count] = new Prime(Convert.ToInt32(numbers[count]));
                            zahlen[count] = executeTask(zahlen[count], this.client);
                        });
                        
                        for (int i = 0; i < PrimeUtil.ANZAHL_ZAHLEN_PRO_MESSAGE; i++)
                        {
                            zahlen[i] = new Prime(Convert.ToInt32(numbers[i]));
                            zahlen[i] = executeTask(zahlen[i], client);
                        }
                    }
                }
                finally

                {
                    channel.BasicPublish(RabbitMQUtils.CONSUMER_EXCHANGE_NAME,
                        RabbitMQUtils.Queue.ConsumerDataReturnQueue.ToString(), null,
                        System.Text.Encoding.UTF8.GetBytes(temp[0] + "|" + Prime.ArrayToString(zahlen)));
                }
            }
        }


        /**
     * Listen for incoming data packets
     *
     * @throws IOException
     */
        private void listenForTasks()
        {
            channel.BasicConsume(getProductionQueueName(), false, new TaskConsumer(channel, this));
        }

        /**
     * Start collecting client data in a fixed interval
     */
        private void triggerDataCollection()
        {
            dataTimer.Interval = 5000;
            // Hook up the Elapsed event for the timer. 
            dataTimer.Elapsed += OnTimedEvent;
            // Start the timer
            dataTimer.Enabled = true;
            Console.WriteLine("Starting data timers...");
            Console.WriteLine("Started data timers");
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            sendClientInfo();
        }

        private String getProductionQueueName()
        {
            return RabbitMQUtils.Queue.ConsumerProductionQueue.ToString() + "_" + name;
        }


        public double getAVGpower()
        {
            avgPower = 0;

            if (powerList.Count > 0)
            {
                foreach (double power in powerList)
                {
                    avgPower += power;
                }

                avgPower = avgPower / powerList.Count;

                powerList.Clear();
            }

            return avgPower;
        }

        public double GetPerformance()
        {
            float firstValue = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            float secondValue = cpuCounter.NextValue(); // now matches task manager reading

            performance = secondValue;

            return performance;
        }


        public void sendClientInfo()
        {
            GetPerformance();

            try
            {
                if (!(Double.IsNaN(performance)))
                {
                    channel.BasicPublish(RabbitMQUtils.CONSUMER_EXCHANGE_NAME,
                        RabbitMQUtils.Queue.ConsumerInfoQueue.ToString(), null,
                        System.Text.Encoding.UTF8.GetBytes(name + "|" + performance));
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
            }
        }


        public static Prime executeTask(Prime zahlToCheck, Client client)
        {
            long startTime = System.DateTime.Now.Millisecond;
            zahlToCheck.isPrime = PrimeUtil.isPrimeNumber(zahlToCheck.getNumber());
            long endTime = System.DateTime.Now.Millisecond;
            zahlToCheck.setClientName(client.name);
            zahlToCheck.setTimeToCalculate((endTime - startTime));
            return zahlToCheck;
        }


        public static Prime[] executeTask2(string[] numbers, Client client)
        {
            Prime[] finishTask = new Prime[numbers.Length];

            lock (finishTask)
            {
                for (int i = 0; i < numbers.Length; i++)
                {
                    long startTime = System.DateTime.Now.Millisecond;
                    finishTask[i].isPrime = PrimeUtil.isPrimeNumber(Convert.ToInt32(numbers[i]));
                    long endTime = System.DateTime.Now.Millisecond;
                    finishTask[i].setClientName(client.name);
                    finishTask[i].setTimeToCalculate((endTime - startTime));
                }
            }

            return finishTask;
        }
    }
}