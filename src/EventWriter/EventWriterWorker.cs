using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using DomainEvents;
using EventStore.ClientAPI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventWriter
{
    public class EventWriterWorker : BackgroundService
    {
        private readonly IEventStoreConnection conn;
        private readonly ILogger<EventWriterWorker> _logger;

        public EventWriterWorker(ILogger<EventWriterWorker> logger)
        {
            _logger = logger;
            
            conn = EventStoreConnection.Create(
                new Uri("tcp://admin:changeit@localhost:1113"), "test_connection");
            conn.ConnectAsync().Wait();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var data = GenerateDomainSuccessEvent();
            var dataType = typeof(DomainSuccessEvent).ToString();
            
            // Writes 1 event per second to the stream until stopped
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
                await conn.AppendToStreamAsync("test_stream", ExpectedVersion.Any,
                    new EventData(Guid.NewGuid(), dataType, isJson: true, data, metadata: new byte[0]));
            }
        }
        
        private static byte[] GenerateDomainSuccessEvent()
        {
            var domainEvent = new DomainSuccessEvent
            {
                MockData = Guid.NewGuid().ToString(),
                Version = 1
            };

            return ConvertToByteArray(domainEvent);
        }
        
        private static byte[] ConvertToByteArray<TEvent>(TEvent input) where TEvent : DomainEventBase
        {
            var jData = JsonConvert.SerializeObject(input);
            var binaryFormatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                binaryFormatter.Serialize(ms, jData);
                return ms.ToArray();
            }
        }
    }
}