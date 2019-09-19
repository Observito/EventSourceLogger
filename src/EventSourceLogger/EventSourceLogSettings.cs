using Observito.Trace.EventSourceFormatter;
using System;
using System.Diagnostics.Tracing;

namespace Observito.Trace.EventSourceLogger
{
    /// <summary>
    /// Event logging settings. Determines what events to log and how to log them.
    /// </summary>
    public sealed class EventSourceLogSettings
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="level">Minimum event level</param>
        public EventSourceLogSettings(EventLevel level)
        {
            Level = level;
        }

        /// <summary>
        /// Minimum event level to log.
        /// </summary>
        public EventLevel Level { get; }

        /// <summary>
        /// If true then the payload will be formatted and included in the log message.
        /// </summary>
        public bool IncludePayload { get; set; }

        /// <summary>
        /// Optional filter to include or exclude events.
        /// </summary>
        public Predicate<EventWrittenEventArgs> Filter { get; set; }

        /// <summary>
        /// Optional selector to format payload values.
        /// </summary>
        /// <remarks>
        /// Useful for transforming certain payload values such as sensitive information to e.g. 
        /// null or encrypted ciphertext.
        /// </remarks>
        public PayloadSelector<object> PayloadSelector { get; set; }
    }
}
