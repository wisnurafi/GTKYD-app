using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Logging;

/// <summary>
/// Minimal rolling file logger provider. Writes structured lines to a dated log file in the
/// application data directory. Intentionally simple: no external logging dependency, no sensitive
/// data captured by the framework (callers must not log secrets).
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly LogLevel _minLevel;
    private readonly BlockingCollection<string> _queue = new(new ConcurrentQueue<string>());
    private readonly Thread _worker;
    private readonly object _fileLock = new();
    private volatile bool _disposed;

    public FileLoggerProvider(string directory, LogLevel minLevel = LogLevel.Information)
    {
        _directory = directory;
        _minLevel = minLevel;
        Directory.CreateDirectory(directory);
        _worker = new Thread(ProcessQueue) { IsBackground = true, Name = "FileLogger" };
        _worker.Start();
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _minLevel, Enqueue);

    private void Enqueue(string line)
    {
        if (!_disposed && !_queue.IsAddingCompleted) _queue.Add(line);
    }

    private void ProcessQueue()
    {
        foreach (var line in _queue.GetConsumingEnumerable())
        {
            try
            {
                var file = Path.Combine(_directory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
                lock (_fileLock) File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);
            }
            catch { /* logging must never throw */ }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _queue.CompleteAdding();
        try { _worker.Join(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
        _queue.Dispose();
    }

    private sealed class FileLogger(string category, LogLevel minLevel, Action<string> write) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= minLevel && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var msg = formatter(state, exception);
            var shortCat = category.Split('.').LastOrDefault() ?? category;
            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel,-11}] {shortCat}: {msg}";
            if (exception is not null) line += $"{Environment.NewLine}    {exception.GetType().Name}: {exception.Message}";
            write(line);
        }
    }
}
