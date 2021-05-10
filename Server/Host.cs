using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Xml.Schema;
using RabbitMQ.Client;
using Informatikprojekt_DotNetVersion.Shared;
using Informatikprojekt_DotNetVersion.Shared.Util;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.Impl;

namespace Informatikprojekt_DotNetVersion.Server
{
    public class Host
    {
        public int openTask;
        public int currentlyExecutedTask;

        public static List<ClientDataReturn> returnData = new List<ClientDataReturn>();
        public static List<RegisteredClient> registeredClients = new List<RegisteredClient>();
        public static List<RegisteredClient> powerSortedClients = new List<RegisteredClient>();
        public IModel channel;
        public static Scheduler scheduler;

        /**
     * @param rabbitMQHost IP-String for the RabbitMQ-Server
     * @param rabbitMQUser RabbitMQ-Username
     * @param rabbitMQPass RabbitMQ-Password
     * @param rabbitMQPort Port of the RabbitMQ-Server
     * @throws IOException
     */
        public Host(String rabbitMQHost, String rabbitMQUser, String rabbitMQPass, int rabbitMQPort)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri("amqp://" + rabbitMQUser + ":" + rabbitMQPass + "@" + rabbitMQHost + ":" +
                                  rabbitMQPort + "/");

            initializeRabbitMQConnection(factory);
            startListeningForNewClients();
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
                IConnection conn = factory.CreateConnection();
                Console.WriteLine("Connection created successfully");

                Console.WriteLine("Creating channel...");
                channel = conn.CreateModel();
                Console.WriteLine("Channel created successfully with number " + channel.ChannelNumber);

                RabbitMQUtils.CreateDefaultExchanges(channel);
                RabbitMQUtils.CreateDefaultQueues(channel);
            }
            catch (TimeoutException e)
            {
                Console.WriteLine("Timeout while trying to connect to the RabbitMQ server");
            }
        }

        /**
     * Listen for client registrations in queue
     *
     * @throws IOException
     */
        private void startListeningForNewClients()
        {
            channel.BasicConsume(RabbitMQUtils.Queue.ConsumerRegistrationQueue.ToString(), true, "myConsumerTag", new NewClientConsumer(channel));
            
            Console.WriteLine("Start listings vor clients");
        }

        public class NewClientConsumer : DefaultBasicConsumer
        {
            private readonly IModel channel;

            public NewClientConsumer(IModel channel)
            {
                this.channel = channel;
            }

            public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
                IBasicProperties properties, byte[] body)
            {
                String name = Encoding.UTF8.GetString(body);
                RegisteredClient rc = new RegisteredClient(name, channel);
                Console.WriteLine(name);
                // lock (registeredClients)
                {
                    registeredClients.Add(rc);
                }
            }
        }

        /**
     * Start scheduleing of tasks until they are finished
     *
     * @param numberRowsToCheck Numbers to initialize the scheduler with
     * @throws IOException
     */
        public void startTaskExecution(List<String> numberRowsToCheck)
        {
            scheduler = new Scheduler(Scheduler.ScheduleingStrategy.PerformanceScheduleing);
            // numberRowsToCheck.ForEach(e => scheduler.addTask(e));
            
            for (int j = 0; j < numberRowsToCheck.Count; j++)
            { 
                scheduler.addTask(j + "|" + numberRowsToCheck[j]);
            }
            
            startListeningForClientInfo();
            
            Console.WriteLine("Press Enter key to continue...");
            try
            {
                Console.ReadLine();
            }
            catch (Exception e)
            {
            }

            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            while (scheduler.tasksLeft())
            {
                // lock (registeredClients)
                {
                    // scheduler.scheduleTasks(registeredClients, channel);
                    scheduler.scheduleTasksByWattUsage(registeredClients, channel);
                }
            }
            
            

            
            long milliseconds2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            scheduler.PrimZahlen();
            scheduler.CheckList();
            scheduler.GetTaskInfo();
            Console.WriteLine("Finished! " + "(In " + (milliseconds2 - milliseconds) + "ms)");
            Console.ReadLine();
            
        }

        /**
     * Listen for metadata from clients and write it to the client object
     *
     * @throws IOException
     */
        public void startListeningForClientInfo()
        {
            channel.BasicConsume(RabbitMQUtils.Queue.ConsumerInfoQueue.ToString(), true, "myConsumerTag4",
                new ClientInfoConsumer(channel));
        }

        public class ClientInfoConsumer : DefaultBasicConsumer
        {

            private readonly IModel channel;
        

        public ClientInfoConsumer(IModel channel)

            {
                this.channel = channel;
            }

            public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered,
                string exchange, string routingKey,
                IBasicProperties properties, byte[] body)
            {
                
                string returnString = Encoding.UTF8.GetString(body);
                string[] temp = new string[returnString.Split("|").Length];

                temp = returnString.Split("|");
                ClientDataReturn clientDataReturn = new ClientDataReturn(temp[0], temp[1]);
            

                RegisteredClient registeredClientOptional = tryFindClientByName(clientDataReturn);

                    returnData.Add(clientDataReturn);

                    powerSortedClients = registeredClients.OrderBy(o => o.getPerformance()).ToList();

                    Console.WriteLine(registeredClientOptional.getName() + ", " +
                                      registeredClientOptional.getPerformance());
            }
        }
        

        public static RegisteredClient tryFindClientByName(ClientDataReturn clientDataReturn)
        {
            foreach (RegisteredClient client in registeredClients)
            {
                if (client.getName().Equals(clientDataReturn.clientName))
                {
                    client.setPerformance(clientDataReturn.performance);
                    return client;
                }
            }

            // synchronized (registeredClients) {
            //     return registeredClients.stream()
            //             .filter(client -> client.getName().equals(name)).findFirst();
            // }
            return null;
        }
    }
}