using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Consumer {

    static void Main(string[] args)
    {
        var config = new ConsumerConfig
        {
            // User-specific properties that you must set
            BootstrapServers = "",
            SaslUsername     = "",
            SaslPassword     = "",

            // Fixed properties
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism    = SaslMechanism.Plain,
            GroupId          = "kafka-dotnet-getting-started",
            AutoOffsetReset  = AutoOffsetReset.Earliest
        };

        const string topic = "purchases";

        CancellationTokenSource cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true; // prevent the process from terminating.
            cts.Cancel();
        };

        var messages = ProduceMessages(topic, 0, 10);

        var firstMessage = messages[0];
        var lastMessage = messages[9];
        using (var consumer = new ConsumerBuilder<byte[], byte[]>(config).Build())
        {
            var timeout = TimeSpan.FromSeconds(10);
            Console.WriteLine($"Result of firstMessage.TopicPartition = {firstMessage.TopicPartition} and firstMessage.Timestamp = {firstMessage.Timestamp.UnixTimestampMs}");
            Console.WriteLine($"Result of lastMessage.TopicPartition = {lastMessage.TopicPartition} and lastMessage.Timestamp = {lastMessage.Timestamp.UnixTimestampMs}");
            var result = consumer.OffsetsForTimes(
                            new[] { new TopicPartitionTimestamp(firstMessage.TopicPartition, firstMessage.Timestamp) },
                            timeout)
                        .ToList();
            Console.WriteLine($"Result of offsetForTime = {result[0].Offset}");

            result = consumer.OffsetsForTimes(
                            new[] { new TopicPartitionTimestamp(lastMessage.TopicPartition, lastMessage.Timestamp) },
                            timeout)
                        .ToList();
            Console.WriteLine($"Result of offsetForTime = {result[0].Offset}");
            consumer.Close();
        }
    }

    private static DeliveryResult<byte[], byte[]>[] ProduceMessages(string topic, int partition, int count)
    {
        var pconfig = new ProducerConfig
        {
            // User-specific properties that you must set
            BootstrapServers = "",
            SaslUsername     = "",
            SaslPassword     = "",

            // Fixed properties
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism    = SaslMechanism.Plain,
            Acks             = Acks.All
        };
        var messages = new DeliveryResult<byte[], byte[]>[count];
        using (var producer = new ProducerBuilder<byte[], byte[]>(pconfig).Build())
        {
                for (var index = 0; index < count; ++index)
                {
                    var message = producer.ProduceAsync(
                        new TopicPartition(topic, partition),
                        new Message<byte[], byte[]>
                        {
                            Key = Serializers.Utf8.Serialize($"test key {index}", SerializationContext.Empty),
                            Value = Serializers.Utf8.Serialize($"test val {index}", SerializationContext.Empty),
                            Headers = null
                        }
                    ).Result;
                    messages[index] = message;
                    Task.Delay(200).Wait();
                }
                producer.Flush(TimeSpan.FromSeconds(10));
            }

            return messages;
    }
}
