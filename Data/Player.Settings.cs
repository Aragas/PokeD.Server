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
                    command = command.Remove(0, 8);

                    Weather weather;
                    if (Enum.TryParse(command, true, out weather))
                    {
                        CustomWorld.Weather = weather;
                        SendCommandResponse(string.Format("Set Weather to {0}!", weather));
                    }
                    else
                        SendCommandResponse("Weather not found!");
                }
                #endregion Weather

                #region Season
                else if (command.StartsWith("season "))
                {
                    command = command.Remove(0, 7);

                    Season season;
                    if (Enum.TryParse(command, true, out season))
                    {
                        CustomWorld.Season = season;
                        SendCommandResponse(string.Format("Set Season to {0}!", season));
                    }
                    else
                        SendCommandResponse("Season not found!");
                }
                #endregion Season

                #region Time
                else if (command.StartsWith("time "))
                {
                    command = command.Remove(0, 5);

                    TimeSpan time;
                    if (TimeSpan.TryParseExact(command, "hh\\:mm\\:ss", null, out time))
                    {
                        CustomWorld.CurrentTime = time;
                        CustomWorld.UseRealTime = false;
                        SendCommandResponse(string.Format("Set time to {0}!", time));
                        SendCommandResponse("Disabled Real Time!");
                    }
                    else
                        SendCommandResponse("Invalid time!");
                }
                #endregion Time

                #region DayCycle
                else if (command.StartsWith("daycycle "))
                {
                    command = command.Remove(0, 9);

                    CustomWorld.DoDayCycle = command.StartsWith("true");
                    SendCommandResponse(string.Format("Set Day Cycle to {0}!", CustomWorld.DoDayCycle));
                }
                #endregion DayCycle

                #region Realtime
                else if (command.StartsWith("realtime "))
                {
                    command = command.Remove(0, 9);

                    CustomWorld.UseRealTime = command.StartsWith("true");
                    CustomWorld.DoDayCycle = true;
                    SendCommandResponse(string.Format("Set Real Time to {0}!", CustomWorld.UseRealTime));
                    SendCommandResponse("Enabled Day Cycle!");
                }
                #endregion Realtime

                #region Location
                else if (command.StartsWith("location "))
                {
                    command = command.Remove(0, 9);

                    CustomWorld.Location = command;
                    CustomWorld.UseLocation = true;
                    SendCommandResponse(string.Format("Set Location to {0}!", CustomWorld.Location));
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
                    SendCommandResponse(string.Format("Successfull muted {0} !", name));
                    break;

                case MuteStatus.MutedYourself:
                    SendCommandResponse("You can't mute yourself!");
                    break;

                case MuteStatus.PlayerNotFound:
                    SendCommandResponse(string.Format("Player {0} not found.", name));
                    break;
            }
        }

        private void ExecuteUnmuteCommand(string name)
        {
            switch (_server.UnMutePlayer(ID, name))
            {
                case MuteStatus.Completed:
                    SendCommandResponse(string.Format("Successfull unmuted {0} !", name));
                    break;

                case MuteStatus.IsNotMuted:
                    SendCommandResponse(string.Format("Player {0} is not muted!", name));
                    break;

                case MuteStatus.PlayerNotFound:
                    SendCommandResponse(string.Format("Player {0} not found.", name));
                    break;
            }
        }

        private void SendCommandResponse(string message)
        {
            SendPacket(new ChatMessagePacket { Message = message }, -1);
        }
    }
}