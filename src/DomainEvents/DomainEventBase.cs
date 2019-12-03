using System;

namespace DomainEvents
{
    public class DomainEventBase
    {
        public int Version;

        public DateTime Occurred { get; } = DateTime.UtcNow;
    }
}