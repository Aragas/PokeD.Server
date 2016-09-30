using System;
using System.Linq;

using PokeD.Server.Clients;
using PokeD.Server.Commands;
using PokeD.Server.Data;

namespace PokeD.Server
{
    public partial class Server
    {
        private CommandManager CommandManager { get; }

        /// <summary>
        /// Return <see langword="false"/> if <see cref="Command"/> not found.
        /// </summary>
        public bool ExecuteClientCommand(Client client, string message)
        {
            var commandWithoutSlash = message.TrimStart('/');
            var messageArray = commandWithoutSlash.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (messageArray.Length <= 0)
                return false; // command not found

            var alias = messageArray[0];
            var trimmedMessageArray = messageArray.Skip(1).ToArray();

            if(!CommandManager.Commands.Any(c => c.Name == alias || c.Aliases.Any(a => a == alias)))
                return false; // command not found

            CommandManager.HandleCommand(client, alias, trimmedMessageArray);
            
            return true;
        }

        /// <summary>
        /// Return <see langword="false"/> if <see cref="Command"/> not found.
        /// </summary>
        public bool ExecuteServerCommand(string message)
        {
            var command = message.ToLower();

            if (message.StartsWith("say "))
                NotifyServerMessage(null, ServerClient, message.Remove(0, 4));

            else if (message.StartsWith("message "))
                NotifyServerMessage(null, ServerClient, message.Remove(0, 8));

            else if (command.StartsWith("help server"))    // help from program
                return ExecuteServerHelpCommand(message.Remove(0, 11));

            else if (command.StartsWith("help"))           // internal help from remote
                return ExecuteServerHelpCommand(message.Remove(0, 4));

            else if (command.StartsWith("world "))
                return ExecuteServerWorldCommand(command.Remove(0, 6));

            else
                return false;

            return true;
        }
        /// <summary>
        /// Return <see langword="false"/> if <see cref="Command"/> not found.
        /// </summary>
        private bool ExecuteServerWorldCommand(string command)
        {
            if (command.StartsWith("set "))
            {
                command = command.Remove(0, 4);

                #region Weather
                if (command.StartsWith("weather "))
                {
                    command = command.Remove(0, 8);

                    Weather weather;
                    if (Enum.TryParse(command, true, out weather))
                    {
                        World.Weather = weather;
                        Logger.Log(LogType.Command, $"Set Weather to {weather}!");
                    }
                    else
                        Logger.Log(LogType.Command, "Weather not found!");
                }
                #endregion Weather

                #region Season
                else if (command.StartsWith("season "))
                {
                    command = command.Remove(0, 7);

                    Season season;
                    if (Enum.TryParse(command, true, out season))
                    {
                        World.Season = season;
                        Logger.Log(LogType.Command, $"Set Season to {season}!");
                    }
                    else
                        Logger.Log(LogType.Command, "Season not found!");
                }
                #endregion Season

                #region Time
                else if (command.StartsWith("time "))
                {
                    command = command.Remove(0, 5);

                    TimeSpan time;
                    if (TimeSpan.TryParseExact(command, "HH\\:mm\\:ss", null, out time))
                    {
                        World.CurrentTime = time;
                        World.UseRealTime = false;
                        Logger.Log(LogType.Command, $"Set time to {time}!");
                        Logger.Log(LogType.Command, "Disabled Real Time!");
                    }
                    else
                        Logger.Log(LogType.Command, "Invalid time!");
                }
                #endregion Time

                #region DayCycle
                else if (command.StartsWith("daycycle "))
                {
                    command = command.Remove(0, 9);

                    World.DoDayCycle = command.StartsWith("true");
                    Logger.Log(LogType.Command, $"Set Day Cycle to {World.DoDayCycle}!");
                }
                #endregion DayCycle

                #region Realtime
                else if (command.StartsWith("realtime "))
                {
                    command = command.Remove(0, 9);

                    World.UseRealTime = command.StartsWith("true");
                    World.DoDayCycle = true;
                    Logger.Log(LogType.Command, $"Set Real Time to {World.UseRealTime}!");
                    Logger.Log(LogType.Command, "Enabled Day Cycle!");
                }
                #endregion Realtime

                #region Location
                else if (command.StartsWith("location "))
                {
                    command = command.Remove(0, 9);

                    World.Location = command;
                    World.UseLocation = true;
                    Logger.Log(LogType.Command, $"Set Location to {World.Location}!");
                    Logger.Log(LogType.Command, "Enabled Location!");
                }
                    #endregion Location

                else
                    return false;
            }

            else
                return false;

            return true;
        }
        /// <summary>
        /// Return <see langword="false"/> if <see cref="Command"/> not found.
        /// </summary>
        private static bool ExecuteServerHelpCommand(string command)
        {
            return false;
        }
    }
}