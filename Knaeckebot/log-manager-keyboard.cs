using System;
using System.Linq;
using System.Windows.Input;
using Knaeckebot.Models;

namespace Knaeckebot.Services
{
    /// <summary>
    /// Part of LogManager that deals with logging keyboard actions
    /// </summary>
    public static partial class LogManager
    {
        /// <summary>
        /// Logs detailed information about a KeyboardAction
        /// </summary>
        public static void LogKeyboardAction(KeyboardAction? action, string source)
        {
            if (action == null)
            {
                Log($"[{source}] KeyboardAction is NULL", LogLevel.KeyDebug);
                return;
            }

            Log($"[{source}] KeyboardAction ID: {action.Id.ToString().Substring(0, 8)}, Name: {action.Name}", LogLevel.KeyDebug);
            Log($"[{source}] - ActionType: {action.ActionType}", LogLevel.KeyDebug);

            if (action.Keys == null)
            {
                Log($"[{source}] - Keys: NULL", LogLevel.KeyDebug);
            }
            else if (action.Keys.Length == 0)
            {
                Log($"[{source}] - Keys: Empty array", LogLevel.KeyDebug);
            }
            else
            {
                Log($"[{source}] - Keys: [{string.Join(", ", action.Keys.Select(k => k.ToString()))}]", LogLevel.KeyDebug);
            }

            if (action.ActionType == KeyboardActionType.TypeText)
            {
                Log($"[{source}] - Text: \"{action.Text}\"", LogLevel.KeyDebug);
            }
        }

        /// <summary>
        /// Logs a Key array with detailed information
        /// </summary>
        public static void LogKeyArray(Key[]? keys, string source)
        {
            if (keys == null)
            {
                Log($"[{source}] Key array: NULL", LogLevel.KeyDebug);
                return;
            }

            if (keys.Length == 0)
            {
                Log($"[{source}] Key array: []", LogLevel.KeyDebug);
                return;
            }

            Log($"[{source}] Key array [{keys.Length}]: [{string.Join(", ", keys.Select(k => k.ToString()))}]", LogLevel.KeyDebug);
        }
    }

    /// <summary>
    /// Represents a log entry
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Timestamp of the entry
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// The log message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The log level
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LogEntry(string message, LogLevel level = LogLevel.Info)
        {
            Timestamp = DateTime.Now;
            Message = message;
            Level = level;
        }

        /// <summary>
        /// String representation of the log entry
        /// </summary>
        public override string ToString()
        {
            return $"{Timestamp:HH:mm:ss} [{Level}] {Message}";
        }
    }
}