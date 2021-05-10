using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Channels;
using Informatikprojekt_DotNetVersion.Shared.Util;
using RabbitMQ.Client;

namespace Informatikprojekt_DotNetVersion.Server
{
    public class RegisteredClient
    {
        public readonly ConcurrentBag<long> executionDurations = new ConcurrentBag<long>();
        public int tasksAssigned = 0;
        private String name;
        private double performance = Double.PositiveInfinity;
        private double percentageCPU;
        private int toAssigneTasks;

        public int ToAssigneTasks
        {
            get => toAssigneTasks;
            set => toAssigneTasks = value;
        }
        

        public double PercentageCpu
        {
            get => percentageCPU;
            set => percentageCPU = value;
        }

        public RegisteredClient(String name, IModel channel){
            this.name = name;

            Console.WriteLine("Declaring custom queue for data exchange with client " + name + "...");
            //queueDeclare(name, durable, exclusive, autoDelete, arguments)
            channel.QueueDeclare(getProductionQueueName(), false, false, true, null);
            Console.WriteLine("Custom queue declared successfully");

            Console.WriteLine("Binding custom queue for data exchange...");
            channel.QueueBind(getProductionQueueName(), RabbitMQUtils.PRODUCER_EXCHANGE_NAME, getProductionQueueName());
            Console.WriteLine("Binding of custom queue completed successfully");
            
        }

        /**
     * Constructs the name of the queue this client will write to
     *
     * @return Queue name
     */
        public String getProductionQueueName() { 
            // return RabbitMQUtils.Queue.CONSUMER_PRODUCTION_QUEUE.getName() + "_" + name;
            return RabbitMQUtils.Queue.ConsumerProductionQueue.ToString() + "_" + name;
        }

        public String getName() {
            return name;
        }

        public double getPerformance() {
            return performance;
        }

        public void setPerformance(double performance) {
            this.performance = performance;
        }
    }
}