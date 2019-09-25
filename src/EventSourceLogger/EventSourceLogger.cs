using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using Microsoft.Extensions.Logging;
using Observito.Trace.EventSourceFormatter;

namespace Observito.Trace.EventSourceLogger
{
    /// <summary>
    /// Forwards <see cref="EventSource"/> messages to the internal <see cref="ILogger"/> implementation.
    /// </summary>
    public sealed class EventSourceLogger : IDisposable
    {
        /// <summary>
        /// Creates a new instance with the given logger.
        /// </summary>
        /// <param name="logger">Logger to forward tracing to</param>
        /// <exception cref="ArgumentNullException">If the logger is null</exception>
        public EventSourceLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _source = new Source();
            _queries = new Dictionary<string, EventSourceLogSettings>(StringComparer.OrdinalIgnoreCase);
            _payloadMetadata = new ConcurrentDictionary<(EventSourceIdentifier, int, int), PayloadType>();

            _source.EventWrittenImpl += OnEventWritten;
        }

        private readonly ILogger _logger;
        private readonly Source _source;
        private readonly Dictionary<string, EventSourceLogSettings> _queries;
        private readonly ConcurrentDictionary<(EventSourceIdentifier, int, int), PayloadType> _payloadMetadata;

        /// <summary>
        /// Enable event logging for an event source.
        /// </summary>
        /// <param name="log">Event source to log events from</param>
        /// <param name="level">Minimum event level to log</param>
        /// <exception cref="ArgumentNullException">If the event source is null</exception>
        public void EnableEvents(EventSource log, EventLevel level)
        {
            if (log is null) throw new ArgumentNullException(nameof(log));

            var query = new EventSourceLogSettings(level) { IncludePayload = true };
            EnableEvents(log, query);
        }

        /// <summary>
        /// Enable event logging for an event source.
        /// </summary>
        /// <param name="log">Event source to log events from</param>
        /// <param name="level">Logging settings</param>
        /// <exception cref="ArgumentNullException">If any argument is null</exception>
        public void EnableEvents(EventSource log, EventSourceLogSettings query)
        {
            if (log is null) throw new ArgumentNullException(nameof(log));
            if (query is null) throw new ArgumentNullException(nameof(query));

            var id = log.GetIdentfier();
            foreach (var kv in log.GetPayloadMetadata())
            {
                if (kv.Item2 != null)
                    _payloadMetadata[(id, kv.Item1.EventId, kv.Item1.Index)] = kv.Item2.Value;
            }

            _queries[log.Name] = query;
            _source.EnableEvents(log, query.Level);
        }

        /// <summary>
        /// Disposes internal resources.
        /// </summary>
        public void Dispose()
        {
            _source.EventWrittenImpl -= OnEventWritten;
            _source.Dispose();
        }

        #region Implementation
        private void OnEventWritten(object sender, EventWrittenEventArgs @event)
        {
            if (_queries.TryGetValue(@event.EventSource.Name, out var query) && (query.Filter == null || query.Filter(@event)))
            {
                var logLevel = ToLogLevel(@event.Level);
                var msg = "";
                try
                {
                    object PayloadSelector(PayloadData payload)
                    {
                        if (_payloadMetadata.TryGetValue((@event.EventSource.GetIdentfier(), @event.EventId, payload.Index), out var payloadType))
                        {
                            if (payloadType == PayloadType.Sensitive)
                                return "(Sensitive information omitted)";
                        }
                        return query.PayloadSelector == null
                            ? payload.Value
                            : query.PayloadSelector(payload);
                    }

                    msg = EventSourceFormatter.EventSourceFormatter.Format(@event, query.IncludePayload, PayloadSelector);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Failure during log formatting of event: {event}" + @event.EventName);
                }
                _logger.Log(logLevel, msg);
            }
        }

        private class Source : EventListener
        {
            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                EventWrittenImpl?.Invoke(this, eventData);
            }

            // Strange name due to the fact that in .NET Core 3 (and std 2.1) this event actually exists, so in the future this event can be removed.
            public event EventHandler<EventWrittenEventArgs> EventWrittenImpl;
        }

        private LogLevel ToLogLevel(EventLevel level)
        {
            // Determine log level
            LogLevel logLevel = default;
            switch (level)
            {
                case EventLevel.LogAlways:
                    logLevel = LogLevel.Debug;
                    break;

                case EventLevel.Critical:
                    logLevel = LogLevel.Critical;
                    break;

                case EventLevel.Error:
                    logLevel = LogLevel.Error;
                    break;

                case EventLevel.Warning:
                    logLevel = LogLevel.Warning;
                    break;

                case EventLevel.Informational:
                    logLevel = LogLevel.Information;
                    break;

                case EventLevel.Verbose:
                    logLevel = LogLevel.None;
                    break;
            }
            return logLevel;
        }
        #endregion
    }
}
