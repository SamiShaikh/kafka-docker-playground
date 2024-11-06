using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        const string topic = "purchases";

        CancellationTokenSource cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true; // prevent the process from terminating.
            cts.Cancel();
        };

        using (var producer = new ProducerBuilder<byte[], byte[]>(pconfig).Build())
        using (var consumer = new ConsumerBuilder<byte[], byte[]>(config).Build())
        {
                const string checkValue = "check value";
                var dr = producer.ProduceAsync(new TopicPartition(topic, 0), new Message<byte[], byte[]> { Value = Serializers.Utf8.Serialize(checkValue, SerializationContext.Empty) }).Result;
                var dr2 = producer.ProduceAsync(new TopicPartition(topic, 0), new Message<byte[], byte[]> { Value = Serializers.Utf8.Serialize("second value", SerializationContext.Empty) }).Result;
                var dr3 = producer.ProduceAsync(new TopicPartition(topic, 0), new Message<byte[], byte[]> { Value = Serializers.Utf8.Serialize("third value", SerializationContext.Empty) }).Result;

                consumer.Assign(new TopicPartitionOffset[] { new TopicPartitionOffset(topic, 0, dr.Offset) });

                var record = consumer.Consume(TimeSpan.FromSeconds(10));
                var firstRecord = record;
		record = consumer.Consume(TimeSpan.FromSeconds(10));
		record = consumer.Consume(TimeSpan.FromSeconds(10));
		Console.WriteLine($"Result of consume before seek : record.Message.Value = {System.Text.Encoding.UTF8.GetString(record.Message.Value)}");

                consumer.Seek(dr.TopicPartitionOffset);

                record = consumer.Consume(TimeSpan.FromSeconds(10));
		Console.WriteLine($"Result of consume : record.Message.Value = {System.Text.Encoding.UTF8.GetString(record.Message.Value)}, checkValue = {checkValue}");
		Console.WriteLine($"Seeking to Offset : firstRecord.TopicPartitionOffset.Offset = {firstRecord.TopicPartitionOffset.Offset}");

                consumer.Seek(firstRecord.TopicPartitionOffset);

                // position shouldn't be equal to the seek position.
                var tpo = consumer.PositionTopicPartitionOffset(record.TopicPartition);

                record = consumer.Consume(TimeSpan.FromSeconds(10));
		Console.WriteLine($"Result of consume : record.Message.Value = {System.Text.Encoding.UTF8.GetString(record.Message.Value)}, checkValue = {checkValue}");

                // position should be equal to last consumed message position + 1.
                tpo = consumer.PositionTopicPartitionOffset(record.TopicPartition);
		Console.WriteLine($"Result of position : record.Offset = {record.Offset}, tpo.Offset = {tpo.Offset}");

            consumer.Close();
            producer.Flush(TimeSpan.FromSeconds(10));
        }
    }

}