using System;

namespace PokeD.Server
{
    /// <summary>
    /// Message Log Type
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// General Log Type.
        /// </summary>
        Info,

        /// <summary>
        /// Chat Log Type.
        /// </summary>
        Chat,

        /// <summary>
        /// Server Chat Log Type.
        /// </summary>
        Event,

        /// <summary>
        /// Trade Log Type.
        /// </summary>
        Trade,

        /// <summary>
        /// PvP Log Type.
        /// </summary>
        PvP,

        /// <summary>
        /// Command Log Type.
        /// </summary>
        Command,

        /// <summary>
        /// Should be reported.
        /// </summary>
        Error,
        /// <summary>
        /// Error Log Type.
        /// </summary>
        Warning,

        /// <summary>
        /// Debug Log Type.
        /// </summary>
        Debug,
    }

    public static class Logger
    {
        public static Action<DateTime, string, string> LogAction;

        public static void Log(LogType type, string message)
        {
            LogAction(DateTime.Now, $"[{type}]: {message}", "[{0:yyyy-MM-dd HH:mm:ss}] {1}");
        }

        public static void LogChatMessage(string player, string chatChannel, string message)
        {
            LogAction(DateTime.Now, $"[{LogType.Chat}]: <{chatChannel}> {player}: {message}", "[{0:yyyy-MM-dd HH:mm:ss}] {1}");
        }

        public static void LogCommandMessage(string player, string message)
        {
            LogAction(DateTime.Now, $"[{LogType.Command}]: {player}: {message}", "[{0:yyyy-MM-dd HH:mm:ss}] {1}");
        }
    }
}