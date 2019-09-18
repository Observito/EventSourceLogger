namespace Observito.Trace.EventSourceLogger
{
    /// <summary>
    /// Delegate to select a payload value.
    /// </summary>
    /// <param name="payloadName">Payload name</param>
    /// <param name="payloadValue">Payload value</param>
    /// <returns>The selected object</returns>
    public delegate object PayloadValueSelector(string payloadName, object payloadValue);
}
