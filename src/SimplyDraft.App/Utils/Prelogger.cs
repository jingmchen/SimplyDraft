using Serilog;
using Serilog.Events;
using SimplyDraft.App.Configuration;

namespace SimplyDraft.App.Utils;

/// <summary>
/// Prelogger class for <c>Program.cs</c> before Serilog is initialized.
/// 
/// <para>Performs the following:</para>
/// <list type="table">
///     <item>1. Write to console when Serilog is yet to be initialized</item>
///     <item>2. Flushes buffered message into Serilog once it is ready</item>
///     <item>3. Writes to a fallback log file, which is removed once buffer is flushed into Serilog</item>
/// </list>
/// </summary>
public static class Prelogger
{
    public static bool SerilogReady {get; private set;}
    private record BufferedLog(
        DateTime Timestamp,
        string Level,
        string SourceContext,
        string Message,
        Exception? Exception
    );

    private static readonly string FallbackLogPath = AppConstants.File.Path.PreStartupLog;
    private static readonly List<BufferedLog> _buffer = new();

    // ─── PRE-SERILOG METHODS ───────────────────
    // Method overloading, first method for non-static classes (cleaner), second method for static classes
    public static void Information<T>(string message) =>
        Write(DateTime.Now, "INF", typeof(T).FullName ?? typeof(T).Name, message, null);
    public static void Information(Type source, string message) =>
        Write(DateTime.Now, "INF", source.FullName ?? source.Name, message, null);
    
    public static void Debug<T>(string message) =>
        Write(DateTime.Now, "DBG", typeof(T).FullName ?? typeof(T).Name, message, null);
    public static void Debug(Type source, string message) =>
        Write(DateTime.Now, "DBG", source.FullName ?? source.Name, message, null);
    
    public static void Warning<T>(string message, Exception? ex = null) =>
        Write(DateTime.Now, "WRN", typeof(T).FullName ?? typeof(T).Name, message, ex);
    public static void Warning(Type source, string message, Exception? ex = null) =>
        Write(DateTime.Now, "WRN", source.FullName ?? source.Name, message, ex);
    
    public static void Error<T>(string message, Exception? ex = null) =>
        Write(DateTime.Now, "ERR", typeof(T).FullName ?? typeof(T).Name, message, ex);
    public static void Error(Type source, string message, Exception? ex = null) =>
        Write(DateTime.Now, "ERR", source.FullName ?? source.Name, message, ex);
    
    public static void Fatal<T>(string message, Exception? ex = null) =>
        Write(DateTime.Now, "FTL", typeof(T).FullName ?? typeof(T).Name, message, ex);
    public static void Fatal(Type source, string message, Exception? ex = null) =>
        Write(DateTime.Now, "FTL", source.FullName ?? source.Name, message, ex);
    
    // ─── POST-SERILOG-INIT METHODS ─────────────
    /// <summary>
    /// Method to trigger if App & Serilog initialization succeeds.
    /// Flushes all log entries in <c>_buffer</c> into Serilog to be printout.
    /// </summary>
    public static void FlushToSerilog()
    {
        SerilogReady = true;

        foreach(var entry in _buffer)
        {
            Log.ForContext("SourceContext", entry.SourceContext).Write(ToLogLevel(entry.Level), entry.Exception, entry.Message);
        }

        _buffer.Clear();

        TryDeleteFallbackLogFile();
    }

    /// <summary>
    /// Method to trigger if App fails before Serilog initialization
    /// Flushes all log entries in <c>_buffer</c> into Console before exiting
    /// </summary>
    public static void FlushToConsole()
    {
        foreach(var entry in _buffer)
        {
            WriteToConsole(entry.Timestamp, entry.Level, entry.SourceContext, entry.Message, entry.Exception);
            WriteToFallbackLogFile(entry.Timestamp, entry.Level, entry.SourceContext, entry.Message, entry.Exception);
        }
    }

    // ─── INTERNAL METHODS ──────────────────────
    private static void Write(DateTime t, string level, string source, string message, Exception? ex = null)
    {
        if (SerilogReady)
        {
            Log.ForContext("SourceContext", source).Write(ToLogLevel(level), ex, message);
            return;
        }

        var entry = new BufferedLog(
            Timestamp: t,
            Level: level,
            SourceContext: source,
            Message: message,
            Exception: ex);
        
        _buffer.Add(entry);
    }
    private static void WriteToConsole(DateTime t, string level, string source, string message, Exception? ex = null)
    {
        var line = FormatLine(t, level, source, message, ex);

        if (level is "ERR" or "FTL")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(line);
            Console.ResetColor();
        }
        else
        {
            Console.Out.WriteLine(line);
        }
    }

    private static void WriteToFallbackLogFile(DateTime t, string level, string source, string message, Exception? ex = null)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FallbackLogPath)!);
            var line = FormatLine(t, level, source, message, ex);
            File.AppendAllText(FallbackLogPath, line + Environment.NewLine);
        } catch
        {
            Error(typeof(Prelogger), $"Failed to write to fallback log file at {FallbackLogPath}.", null);
        }
    }

    private static void TryDeleteFallbackLogFile()
    {
        try
        {
            if (File.Exists(FallbackLogPath))
            {
                File.Delete(FallbackLogPath);
            }

        } catch
        {
            Error(typeof(Prelogger), $"Failed to delete fallback log file at {FallbackLogPath}.", null);
        }
    }

    private static string FormatLine(DateTime t, string level, string source, string message, Exception? ex = null)
    {
        var line = $"[{t:dd-MMM-yyyy}] [{t:HH:mm:ss}] [{level}]";
        if (!string.IsNullOrEmpty(source)) line += $" [{source}]";
        line += $" [{message}]";
        if (ex is not null) line += $" [{ex}]";

        return line;
    }

    private static LogEventLevel ToLogLevel(string level) => level switch
    {
        "DBG" => LogEventLevel.Debug,
        "WRN" => LogEventLevel.Warning,
        "ERR" => LogEventLevel.Error,
        "FTL" => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };
}