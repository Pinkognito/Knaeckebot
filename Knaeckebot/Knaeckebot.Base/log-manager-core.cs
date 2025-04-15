using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Knaeckebot.Services
{
    /// <summary>
    /// Log level for log entries
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug,
        Trace,   // Added for very detailed logging
        KeyDebug // Specifically for keyboard debugging
    }

    /// <summary>
    /// Manages application logging
    /// </summary>
    public static partial class LogManager
    {
        // Configurable path to store log files
        private static string _logBasePath = string.Empty;
        private static string LogBasePath
        {
            get
            {
                if (string.IsNullOrEmpty(_logBasePath))
                {
                    // Default: AppData\Knaeckebot\logs
                    _logBasePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Knaeckebot",
                        "logs");
                }
                return _logBasePath;
            }
            set
            {
                _logBasePath = value;
            }
        }

        // Current log file path
        private static string LogFile => Path.Combine(
            LogBasePath,
            $"Knaeckebot_{DateTime.Now:yyyy-MM-dd}.log");

        private static readonly object LogLock = new object();

        /// <summary>
        /// Enables detailed logging for keyboard actions
        /// </summary>
        public static bool EnableKeyboardDebugging { get; set; } = true;

        /// <summary>
        /// Collection of log entries for display in the UI
        /// </summary>
        public static ObservableCollection<LogEntry> LogEntries { get; } = new ObservableCollection<LogEntry>();

        /// <summary>
        /// Event for new log entries
        /// </summary>
        public static event EventHandler<LogEntry>? LogEntryAdded;

        /// <summary>
        /// New flag for explicit enabling in Release
        /// </summary>
        public static bool EnableVerboseLogging { get; set; } = false;

        /// <summary>
        /// Static constructor
        /// </summary>
        static LogManager()
        {
            // Ensure the directory exists
            EnsureLogDirectoryExists();

            #if DEBUG
            // Open console only in debug mode
            if (Debugger.IsAttached)
            {
                AllocConsole();
                Console.WriteLine("Debug console opened for Knaeckebot WPF");
                Console.WriteLine("--------------------------------------------");
            }
            #endif
        }

        /// <summary>
        /// Set a custom path for storing log files
        /// </summary>
        /// <param name="basePath">Directory path where logs should be stored</param>
        public static void SetLogPath(string basePath)
        {
            if (!string.IsNullOrEmpty(basePath))
            {
                LogBasePath = basePath;
                EnsureLogDirectoryExists();
                Debug.WriteLine("Log path changed to: " + LogBasePath);
            }
        }

        /// <summary>
        /// Ensures the log directory exists
        /// </summary>
        private static void EnsureLogDirectoryExists()
        {
            string directory = Path.GetDirectoryName(LogFile) ?? "";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        /// <summary>
        /// Writes a log entry
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="level">The log level (default: Info)</param>
        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            // Skip Debug/Trace/KeyDebug logs in Release mode unless explicitly enabled
#if !DEBUG
                if ((level == LogLevel.Debug || level == LogLevel.Trace || level == LogLevel.KeyDebug) 
                    && !EnableVerboseLogging)
                    return;
#endif

            var entry = new LogEntry(message, level);

            // Special handling for keyboard debugging
            if (level == LogLevel.KeyDebug && !EnableKeyboardDebugging)
                return;

            // Add to the collection (if UI is already initialized)
            if (System.Windows.Application.Current != null)
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogEntries.Add(entry);
                        // Limit to the last 1000 entries
                        if (LogEntries.Count > 1000)
                        {
                            LogEntries.RemoveAt(0);
                        }
                    });
                }
                catch (Exception)
                {
                    // Ignore if dispatcher is not available
                }
            }

            // Trigger event
            LogEntryAdded?.Invoke(null, entry);

            // Write to debug output
            var logMessage = $"[{entry.Level}] {entry.Timestamp:HH:mm:ss.fff} - {entry.Message}";
            Debug.WriteLine(logMessage);

            // Write to console if debugger attached or special levels
            if (Debugger.IsAttached || level == LogLevel.Error || level == LogLevel.Warning || level == LogLevel.KeyDebug)
            {
                Console.ForegroundColor = GetColorForLogLevel(level);
                Console.WriteLine(logMessage);
                Console.ResetColor();
            }

            // Write to file (in background)
            WriteToFile(entry);
        }

        /// <summary>
        /// Returns the console color for a log level
        /// </summary>
        private static ConsoleColor GetColorForLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Debug => ConsoleColor.Cyan,
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.KeyDebug => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Writes an entry to the log file
        /// </summary>
        private static void WriteToFile(LogEntry entry)
        {
            Task.Run(() =>
            {
                try
                {
                    lock (LogLock)
                    {
                        // Ensure directory exists before writing
                        EnsureLogDirectoryExists();

                        File.AppendAllText(LogFile, $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{entry.Level}] {entry.Message}{Environment.NewLine}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error writing to log file: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Deletes old logs (older than 30 days)
        /// </summary>
        public static void CleanupOldLogs()
        {
            Task.Run(() =>
            {
                try
                {
                    string directory = Path.GetDirectoryName(LogFile) ?? "";
                    if (Directory.Exists(directory))
                    {
                        var files = Directory.GetFiles(directory, "Knaeckebot_*.log");
                        var cutoffDate = DateTime.Now.AddDays(-30);

                        foreach (var file in files)
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.CreationTime < cutoffDate)
                            {
                                fileInfo.Delete();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning up old logs: {ex.Message}");
                }
            });
        }
    }
}