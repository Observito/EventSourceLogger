using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using Microsoft.Extensions.Logging;

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

            _source.EventWrittenImpl += OnEventWritten;
        }

        private readonly ILogger _logger;
        private readonly Source _source;
        private readonly Dictionary<string, EventSourceLogSettings> _queries = new Dictionary<string, EventSourceLogSettings>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Enable event logging for an event source.
        /// </summary>
        /// <param name="log">Event source to log events from</param>
        /// <param name="level">Minimum event level to log</param>
        /// <exception cref="ArgumentNullException">If the event source is null</exception>
        public void EnableEvents(EventSource log, EventLevel level)
        {
            if (log is null) throw new ArgumentNullException(nameof(log));

            var query = new EventSourceLogSettings(level);
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
                    msg = FormatEvent(@event, query);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Failure during log formatting of event: {event}" + @event.EventName);
                }
                _logger.Log(logLevel, msg);
            }
        }

        private static string FormatEvent(EventWrittenEventArgs @event, EventSourceLogSettings settings)
        {
            // Create log message builder
            var sbLog = new StringBuilder();

            // Append log header line
            sbLog.AppendLine($"{@event.EventName}: {@event.Message}");
            sbLog.AppendLine();

            // Data dictionary
            var map = new Dictionary<string, object>();
            map["EventSourceName"] = @event.EventSource.Name;
            map["EventSourceGuid"] = @event.EventSource.Guid;
            map["Version"] = @event.Version;

            // Add payload to data
            if (settings.IncludePayload || true)
            {
                var index = 0;
                foreach (var name in @event.PayloadNames)
                {
                    var payload = @event.Payload[index];
                    if (settings.PayloadSelector != null)
                        payload = settings.PayloadSelector(name, payload);
                    map[$"@{name}"] = payload;
                    index++;
                }
            }

            foreach (var kv in map)
            {
                string val = kv.Value.ToString();

                sbLog.AppendLine($"{kv.Key}={val}");
            }

            var fmt = sbLog.ToString();
            return fmt;
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
