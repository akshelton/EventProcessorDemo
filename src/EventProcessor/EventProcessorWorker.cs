using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DomainEvents;
using EventProcessor.Configs;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventProcessor
{
    public class EventProcessorWorker : BackgroundService
    {
        private readonly UserCredentials userCredentials;
        private readonly IEventStoreConnection conn;
        private EventStorePersistentSubscriptionBase subscription;
        private readonly string streamName;
        private readonly string groupName;
        private CancellationToken cancellationToken;
        
        private readonly ILogger<EventProcessorWorker> _logger;
        
        public EventProcessorWorker(ILogger<EventProcessorWorker> logger, IEventStoreConnectionConfig connectionConfig)
        {
            _logger = logger;
            streamName = "test_stream";
            groupName = "test_group";
            
            userCredentials = new UserCredentials(connectionConfig.Username, connectionConfig.Password);
            var connectionSettings = ConnectionSettings.Create();
            connectionSettings.SetDefaultUserCredentials(userCredentials);

            conn = EventStoreConnection.Create(
                new Uri(
                    $"tcp://{connectionConfig.Username}:{connectionConfig.Password}@{connectionConfig.IpAddress}:{connectionConfig.IpPort}"),
                "test_connection");
            conn.ConnectAsync().Wait();
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            cancellationToken = stoppingToken;
            CreateSubscription();

            await ConnectToSubscription();
        }
        
        // Based on code found at https://codeopinion.com/event-store-persistent-subscriptions-demo/
        private async Task ConnectToSubscription()
        {
            _logger.LogInformation($"Connecting to subscription at: {DateTimeOffset.Now}");
            var bufferSize = 10;
            var autoAck = true;
            
            subscription = await conn.ConnectToPersistentSubscriptionAsync(streamName, groupName, 
                (_, x) =>
                {
                    var data = Encoding.ASCII.GetString(x.Event.Data);
                    Console.WriteLine($"Received: {x.Event.EventStreamId}:{x.Event.EventId}");
                    Console.WriteLine(data);
                },
                (_, reason, ex) =>
                {
                    Console.WriteLine(ex);
                    Console.WriteLine(reason);
                    ConnectToSubscription().Wait(cancellationToken);        
                }, userCredentials, bufferSize, autoAck);
        }

        private void CreateSubscription()
        {
            var settings = PersistentSubscriptionSettings.Create()
                .DoNotResolveLinkTos()
                .StartFromCurrent();

            try
            {
                conn.CreatePersistentSubscriptionAsync(streamName, groupName, settings, userCredentials).Wait();
            }
            catch (Exception e)
            {
                if (e.InnerException?.GetType() != typeof(InvalidOperationException) && e.InnerException?.Message !=
                    $"Subscription group {groupName} on stream {streamName} already exists")
                {
                    throw;
                }
            }
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

        private static TEvent ConvertFromByteArray<TEvent>(byte[] data) where TEvent : DomainEventBase
        {
            if (data == null)
            {
                return default;
            }

            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream(data))
            {
                var obj = bf.Deserialize(ms);

                if (obj is TEvent)
                {
                    var result = (TEvent) obj;
                    return result;
                } 
                if (obj is string)
                {
                    var result = JsonConvert.DeserializeObject<TEvent>((string) obj);
                    return result;
                }
            }
            return default;
        }
        
        public override void Dispose()
        {
            subscription.Stop(TimeSpan.Zero);
            conn?.Dispose();
            base.Dispose();
        }
    }
}