using System;
using PokeD.Core.Wrappers;

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
        /// Error Log Type.
        /// </summary>
        Warning,

        /// <summary>
        /// Debug Log Type.
        /// </summary>
        Debug,

        /// <summary>
        /// Chat Log Type.
        /// </summary>
        Chat,

        /// <summary>
        /// PM Log Type.
        /// </summary>
        PM,

        /// <summary>
        /// Server Chat Log Type.
        /// </summary>
        Server,

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
    }

    public static class Logger
    {
        public static void Log(LogType type, string message)
        {
            InputWrapper.LogWriteLine(string.Format("[{0:yyyy-MM-dd_hh:mm:ss}]_[{1}]:{2}", DateTime.Now, type, message));
        }

        public static void LogChatMessage(string player, string message)
        {
            InputWrapper.LogWriteLine(string.Format("[{0:yyyy-MM-dd_hh:mm:ss}]_<{1}>_{2}", DateTime.Now, player, message));
        }
    }
}
