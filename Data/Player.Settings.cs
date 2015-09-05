using System;

using Newtonsoft.Json;

using PokeD.Core.Packets.Chat;

namespace PokeD.Server.Data
{
    public partial class Player
    {
        [JsonProperty]
        public bool UseCustomWorld { get; private set; }
        [JsonProperty]
        World CustomWorld { get; set; }


        private void ExecuteCommand(string message)
        {
            var command = message.Remove(0, 1).ToLower();
            message = message.Remove(0, 1);

            if (command.StartsWith("help"))
                ExecuteHelpCommand();

            else if (command.StartsWith("world "))
                ExecuteWorldCommand(command.Remove(0, 6));

            else if (command.StartsWith("mute "))
                ExecuteMuteCommand(message.Remove(0, 5));

            else if (command.StartsWith("unmute "))
                ExecuteUnmuteCommand(message.Remove(0, 7));

            else if (command.StartsWith("move "))
                ExecuteMoveCommand(message.Remove(0, 5));

            else
                SendCommandResponse("Invalid command!");
        }

        private void ExecuteHelpCommand()
        {

        }

        private void ExecuteWorldCommand(string command)
        {
            if (command.StartsWith("enable") || command.StartsWith("enable custom"))
            {
                if (CustomWorld == null)
                    CustomWorld = new World();

                UseCustomWorld = true;
                SendCommandResponse("Enabled Custom World!");
            }

            else if (command.StartsWith("disable") || command.StartsWith("disable custom"))
            {
                UseCustomWorld = false;
                SendCommandResponse("Disabled Custom World!");
            }

            else if (command.StartsWith("set "))
            {
                if (!UseCustomWorld) { SendCommandResponse("Can't do! Custom World is disabled!"); return; }

                command = command.Remove(0, 4);

                #region Weather
                if (command.StartsWith("weather "))
                {
                    command = command.Remove(0, 8).Trim();

                    Weather weather;
                    if (Enum.TryParse(command, true, out weather))
                    {
                        CustomWorld.Weather = weather;
                        SendCommandResponse($"Set Weather to {weather}!");
                    }
                    else
                        SendCommandResponse("Weather not found!");
                }
                #endregion Weather

                #region Season
                else if (command.StartsWith("season "))
                {
                    command = command.Remove(0, 7).Trim();

                    Season season;
                    if (Enum.TryParse(command, true, out season))
                    {
                        CustomWorld.Season = season;
                        SendCommandResponse($"Set Season to {season}!");
                    }
                    else
                        SendCommandResponse("Season not found!");
                }
                #endregion Season

                #region Time
                else if (command.StartsWith("time "))
                {
                    command = command.Remove(0, 5).Trim();

                    TimeSpan time;
                    if (TimeSpan.TryParseExact(command, "HH\\:mm\\:ss", null, out time))
                    {
                        CustomWorld.CurrentTime = time;
                        CustomWorld.UseRealTime = false;
                        SendCommandResponse($"Set time to {time}!");
                        SendCommandResponse("Disabled Real Time!");
                    }
                    else
                        SendCommandResponse("Invalid time!");
                }
                #endregion Time

                #region DayCycle
                else if (command.StartsWith("daycycle "))
                {
                    command = command.Remove(0, 9).Trim();

                    CustomWorld.DoDayCycle = command.StartsWith("true");
                    SendCommandResponse($"Set Day Cycle to {CustomWorld.DoDayCycle}!");
                }
                #endregion DayCycle

                #region Realtime
                else if (command.StartsWith("realtime "))
                {
                    command = command.Remove(0, 9).Trim();

                    CustomWorld.UseRealTime = command.StartsWith("true");
                    CustomWorld.DoDayCycle = true;
                    SendCommandResponse($"Set Real Time to {CustomWorld.UseRealTime}!");
                    SendCommandResponse("Enabled Day Cycle!");
                }
                #endregion Realtime

                #region Location
                else if (command.StartsWith("location ") && false)
                {
                    command = command.Remove(0, 9).Trim();

                    CustomWorld.Location = command;
                    CustomWorld.UseLocation = true;
                    SendCommandResponse($"Set Location to {CustomWorld.Location}!");
                    SendCommandResponse("Enabled Location!");
                }
                #endregion Location

                else
                    SendCommandResponse("Invalid command!");
            }

            else
                SendCommandResponse("Invalid command!");
        }

        private void ExecuteMuteCommand(string message)
        {
            var name = message.Remove(0, 5);
            switch (_server.MutePlayer(ID, name))
            {
                case MuteStatus.Completed:
                    SendCommandResponse($"Successfull muted {name} !");
                    break;

                case MuteStatus.MutedYourself:
                    SendCommandResponse("You can't mute yourself!");
                    break;

                case MuteStatus.PlayerNotFound:
                    SendCommandResponse($"Player {name} not found.");
                    break;
            }
        }

        private void ExecuteUnmuteCommand(string name)
        {
            switch (_server.UnMutePlayer(ID, name))
            {
                case MuteStatus.Completed:
                    SendCommandResponse($"Successfull unmuted {name} !");
                    break;

                case MuteStatus.IsNotMuted:
                    SendCommandResponse($"Player {name} is not muted!");
                    break;

                case MuteStatus.PlayerNotFound:
                    SendCommandResponse($"Player {name} not found.");
                    break;
            }
        }

        private void ExecuteMoveCommand(string command)
        {
            if (command.StartsWith("set "))
            {
                command = command.Remove(0, 4);

                if (command.StartsWith("updaterate "))
                {
                    command = command.Remove(0, 11).Trim();

                    int updateRate;
                    if (command.StartsWith("normal"))
                    {
                        MovingUpdateRate = 60;
                        SendCommandResponse("Set moving correction updaterate to Normal!");
                    }
                    else if (command.StartsWith("fast"))
                    {
                        MovingUpdateRate = 30;
                        SendCommandResponse("Set moving correction updaterate to Fast!");
                    }
                    else if (int.TryParse(command, out updateRate) && updateRate >= 0 && updateRate <= 100)
                    {
                        MovingUpdateRate = updateRate;
                        SendCommandResponse($"Set moving correction updaterate to {updateRate}!");
                    }
                    else
                        SendCommandResponse("Number invalid!");
                }

                else
                    SendCommandResponse("Invalid command!");
            }

            else
                SendCommandResponse("Invalid command!"); 
        }

        private void SendCommandResponse(string message)
        {
            SendPacket(new ChatMessagePacket { Message = message }, -1);
        }
    }
}