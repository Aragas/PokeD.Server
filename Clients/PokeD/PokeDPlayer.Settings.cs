using Aragas.Core.Data;

using PokeD.Core.Packets.P3D.Chat;

namespace PokeD.Server.Clients.PokeD
{
    public partial class PokeDPlayer
    {
        private void ExecuteCommand(string message)
        {
            var command = message.Remove(0, 1).ToLower();
            message = message.Remove(0, 1);

            if (command.StartsWith("login "))
                ExecuteLoginCommand(message.Remove(0, 6));

            else if(command.StartsWith("changepassword ") && IsInitialized)
                ExecuteChangePasswordCommand(message.Remove(0, 15));

            else
                SendCommandResponse("Invalid command!");
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

            //Module.P3DPlayerChangePassword(this, oldPassword, newPassword);
            SendCommandResponse("Please use /login %PASSWORD% for logging in or registering");
        }

        private void SendCommandResponse(string message)
        {
            SendPacket(new ChatMessageGlobalPacket { Message = message }, -1);
        }
    }
}