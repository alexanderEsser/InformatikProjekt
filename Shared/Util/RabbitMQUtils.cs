using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Channels;
using RabbitMQ.Client;

namespace Informatikprojekt_DotNetVersion.Shared.Util
{
    public class RabbitMQUtils
    { 
        public readonly static String PRODUCER_EXCHANGE_NAME = "producer_events";
    public readonly static String CONSUMER_EXCHANGE_NAME = "consumer_events";

    //docker run -d --restart always --ip="10.0.0.8" -p 5672:5672 -p 15672:15672 rabbitmq:3.6.6-management
    //docker run --ip="10.0.0.8" -p 5672:5672 -p 15672:15672 rabbitmq:3.6.6-management

    public static void CreateDefaultExchanges(IModel channel){
        Console.WriteLine("Declaring exchanges...");
        //Producer->Consumer
        channel.ExchangeDeclare(PRODUCER_EXCHANGE_NAME, ExchangeType.Direct);
        //Consumer->Producer
        channel.ExchangeDeclare(CONSUMER_EXCHANGE_NAME, ExchangeType.Direct);
        Console.WriteLine("Exchanges declared successfully");
    }

    public static void CreateDefaultQueues(IModel channel){
        Console.WriteLine("Declaring queues...");
        //queueDeclare(name, durable, exclusive, autoDelete, arguments)
        channel.QueueDeclare(Queue.ConsumerRegistrationQueue.ToString(), false, false, false, null);
        channel.QueueDeclare(Queue.ConsumerDataReturnQueue.ToString(), false, false, false, null);
        channel.QueueDeclare(Queue.ConsumerInfoQueue.ToString(), false, false, false, null);
        Console.WriteLine("Queues declared successfully");

        Console.WriteLine("Binding queues...");
        channel.QueueBind(Queue.ConsumerRegistrationQueue.ToString(), CONSUMER_EXCHANGE_NAME, Queue.ConsumerRegistrationQueue.ToString());
        channel.QueueBind(Queue.ConsumerDataReturnQueue.ToString(), CONSUMER_EXCHANGE_NAME, Queue.ConsumerDataReturnQueue.ToString());
        channel.QueueBind(Queue.ConsumerInfoQueue.ToString(), CONSUMER_EXCHANGE_NAME, Queue.ConsumerInfoQueue.ToString());
        Console.WriteLine("Binding of queues completed successfully");
    }
    
    public enum Queue
    {
        ConsumerRegistrationQueue, //registration of new consumers
        ConsumerProductionQueue, //sending data to the clients
        ConsumerDataReturnQueue, //return data to producer
        ConsumerInfoQueue //send consumer info
    };
    
    // Convert an object to a byte array
    public static byte[] ObjectToByteArray(Object obj)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }
// Convert a byte array to an Object
    public static Object ByteArrayToObject(byte[] arrBytes)
    {
        using (var memStream = new MemoryStream())
        {
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = binForm.Deserialize(memStream);
            return obj;
        }
    }
    
    public static byte[] SerializeToByteArray( object obj)
    {
        if (obj == null)
        {
            return null;
        }
        var bf = new BinaryFormatter();
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    public static T Deserialize<T>( byte[] byteArray) where T : class
    {
        if (byteArray == null)
        {
            return null;
        }
        using (var memStream = new MemoryStream())
        {
            var binForm = new BinaryFormatter();
            memStream.Write(byteArray, 0, byteArray.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = (T)binForm.Deserialize(memStream);
            return obj;
        }
    }
    
    
    }
}