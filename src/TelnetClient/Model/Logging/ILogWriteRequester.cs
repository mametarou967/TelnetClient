namespace TelnetClient.Model.Logging
{
    public interface ILogWriteRequester
    {
        void WriteRequest(LogLevel logLevel, string message);
    }
}
