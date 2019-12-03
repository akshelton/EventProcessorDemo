using System;

namespace EventProcessor.Configs
{
    [Serializable]
    public class EventStoreConnectionConfig: IEventStoreConnectionConfig
    {
        public string Username { get; set; }
        
        public string Password { get; set; }
        
        public string IpAddress { get; set; }
        
        public string IpPort { get; set; }
    }

    public interface IEventStoreConnectionConfig
    {
        string Username { get; }
        
        string Password { get; }
        
        string IpAddress { get; }
        
        string IpPort { get; }
    }
}