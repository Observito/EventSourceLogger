using System;
using System.Diagnostics.Tracing;

namespace Sample
{
    [EventSource(Name = "Observito-Test-Echo")]
    public sealed class EchoEventSource : EventSource
    {
        public static readonly EchoEventSource Log = new EchoEventSource();

        public sealed class Tasks
        {
            public const EventTask Echo = (EventTask)1;
        }

        public sealed class Events
        {
            public const int Echo = 1;
            public const int EchoMore = 2;
        }

        [Event(Events.Echo, Level = EventLevel.Warning, Task = Tasks.Echo, Opcode = EventOpcode.Info, Message = "Echo: {0}")]
        public void Echo(string message) { WriteEvent(Events.Echo, message); }

        [Event(Events.EchoMore, Level = EventLevel.Warning, Task = Tasks.Echo, Opcode = EventOpcode.Info, Message = "Echo: {0}")]
        public void EchoMore(string message, DateTime date, int count) { WriteEvent(Events.EchoMore, message, date, count); }
    }
}
