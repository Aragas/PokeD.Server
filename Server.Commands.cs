using System;

using PokeD.Core.Wrappers;

using PokeD.Server.Data;

namespace PokeD.Server
{
    public partial class Server
    {

        public void ExecuteCommand(string message)
        {
            var command = message.ToLower();

            if (message.StartsWith("say "))
                SendGlobalChatMessageToAll(message.Remove(0, 4));

            else if (message.StartsWith("message "))
                SendServerMessageToAll(message.Remove(0, 8));

            else if (command.StartsWith("help server"))    // help from program
                ExecuteHelpCommand(message.Remove(0, 11));

            else if (command.StartsWith("help"))           // internal help from remote
                ExecuteHelpCommand(message.Remove(0, 4));

            else if (command.StartsWith("world "))
                ExecuteWorldCommand(command.Remove(0, 6));

            else
                InputWrapper.ConsoleWrite("Invalid command!");
        }

        private void ExecuteWorldCommand(string command)
        {
            if (command.StartsWith("enable") || command.StartsWith("enable custom"))
            {
                CustomWorldEnabled = true;
                InputWrapper.ConsoleWrite("Enabled Custom World!");
            }

            else if (command.StartsWith("disable") || command.StartsWith("disable custom"))
            {
                CustomWorldEnabled = false;
                InputWrapper.ConsoleWrite("Disabled Custom World!");
            }

            else if (command.StartsWith("set "))
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
                        InputWrapper.ConsoleWrite($"Set Weather to {weather}!");
                    }
                    else
                        InputWrapper.ConsoleWrite("Weather not found!");
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
                        InputWrapper.ConsoleWrite($"Set Season to {season}!");
                    }
                    else
                        InputWrapper.ConsoleWrite("Season not found!");
                }
                #endregion Season

                #region Time
                else if (command.StartsWith("time "))
                {
                    command = command.Remove(0, 5);

                    TimeSpan time;
                    if (TimeSpan.TryParseExact(command, "hh\\:mm\\:ss", null, out time))
                    {
                        World.CurrentTime = time;
                        World.UseRealTime = false;
                        InputWrapper.ConsoleWrite($"Set time to {time}!");
                        InputWrapper.ConsoleWrite("Disabled Real Time!");
                    }
                    else
                        InputWrapper.ConsoleWrite("Invalid time!");
                }
                #endregion Time

                #region DayCycle
                else if (command.StartsWith("daycycle "))
                {
                    command = command.Remove(0, 9);

                    World.DoDayCycle = command.StartsWith("true");
                    InputWrapper.ConsoleWrite($"Set Day Cycle to {World.DoDayCycle}!");
                }
                #endregion DayCycle

                #region Realtime
                else if (command.StartsWith("realtime "))
                {
                    command = command.Remove(0, 9);

                    World.UseRealTime = command.StartsWith("true");
                    World.DoDayCycle = true;
                    InputWrapper.ConsoleWrite($"Set Real Time to {World.UseRealTime}!");
                    InputWrapper.ConsoleWrite("Enabled Day Cycle!");
                }
                #endregion Realtime

                #region Location
                else if (command.StartsWith("location "))
                {
                    command = command.Remove(0, 9);

                    World.Location = command;
                    World.UseLocation = true;
                    InputWrapper.ConsoleWrite($"Set Location to {World.Location}!");
                    InputWrapper.ConsoleWrite("Enabled Location!");
                }
                #endregion Location

                else
                    InputWrapper.ConsoleWrite("Invalid command!");
            }

            else
                InputWrapper.ConsoleWrite("Invalid command!");
        }

        private static void ExecuteHelpCommand(string command)
        {

        }
    }
}
