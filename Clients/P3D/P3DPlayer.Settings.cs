using Aragas.Core.Data;

using PokeD.Core.Packets.P3D.Chat;

namespace PokeD.Server.Clients.P3D
{
    public partial class P3DPlayer
    {
        private bool ExecuteCommand(string message)
        {
            var command = message.Remove(0, 1).ToLower();
            message = message.Remove(0, 1);

            if (command.StartsWith("login "))
                ExecuteLoginCommand(message.Remove(0, 6));

            else if(command.StartsWith("changepassword ") && IsInitialized)
                ExecuteChangePasswordCommand(message.Remove(0, 15));

            else if (command.StartsWith("help"))
                ExecuteHelpCommand();

            else if (command.StartsWith("mute ") && IsInitialized)
                ExecuteMuteCommand(message.Remove(0, 5));

            else if (command.StartsWith("unmute ") && IsInitialized)
                ExecuteUnmuteCommand(message.Remove(0, 7));

            else if (command.StartsWith("move ") && false)
                ExecuteMoveCommand(message.Remove(0, 5));

            else
            {
                return false;
            }

            return true;
        }

        private void ExecuteLoginCommand(string password)
        {
            PasswordHash = new PasswordStorage(password).Hash;
            Initialize();
        }
        private void ExecuteChangePasswordCommand(string command)
        {
            var array = command.Split(' ');
            var oldPassword = new PasswordStorage(array[0]).Hash;
            var newPassword = new PasswordStorage(array[1]).Hash;

            Module.P3DPlayerChangePassword(this, oldPassword, newPassword);
            SendCommandResponse("Please use /login %PASSWORD% for logging in or registering");
        }

        private void ExecuteHelpCommand()
        {

        }

        private void ExecuteMuteCommand(string message)
        {
            var name = message.Remove(0, 5);
            switch (Module.MutePlayer(ID, name))
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
            switch (Module.UnMutePlayer(ID, name))
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
                        //MovingUpdateRate = 60;
                        SendCommandResponse("Set moving correction updaterate to Normal!");
                    }
                    else if (command.StartsWith("fast"))
                    {
                        //MovingUpdateRate = 30;
                        SendCommandResponse("Set moving correction updaterate to Fast!");
                    }
                    else if (int.TryParse(command, out updateRate) && updateRate >= 0 && updateRate <= 100)
                    {
                        //MovingUpdateRate = updateRate;
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
            SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = message });
        }
    }
}