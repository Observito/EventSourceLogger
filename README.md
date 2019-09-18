# EventSourceLogger

Forward in-process event source messages to a logger.

## Example

```csharp
var loggerFactory = LoggerFactory.Create(builder => {
    builder.AddConsole();
});
var consoleLogger = loggerFactory.CreateLogger<Program>(); // use your favourite logger here, e.g. event log

using var logger = new EventSourceLogger(consoleLogger);

logger.EnableEvents(EchoEventSource.Log, EventLevel.Informational);
```
