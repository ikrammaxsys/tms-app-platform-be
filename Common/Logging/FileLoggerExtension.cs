namespace tms_template_net8.Common.Logging;

using Microsoft.Extensions.Logging;

public class FileLogger : ILogger
{
    private readonly string _filePath;
    private static readonly object _lock = new();
    public FileLogger(string path) => _filePath = path;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (formatter == null) return;

        lock (_lock)
        {
            Directory.CreateDirectory(_filePath);

            var logFile = Path.Combine(_filePath, $"{DateTime.Now:yyyy-MM-dd}_log.txt");
            var message = $"{DateTime.Now} [{logLevel}] {formatter(state, exception)}{Environment.NewLine}";

            File.AppendAllText(logFile, message);
        }
    }
}

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    public FileLoggerProvider(string path) => _path = path;
    public ILogger CreateLogger(string categoryName) => new FileLogger(_path);
    public void Dispose() { }
}

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string path)
    {
        builder.AddProvider(new FileLoggerProvider(path));
        return builder;
    }
}
