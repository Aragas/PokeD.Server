using System;
using System.Globalization;
using System.Linq;

using Aragas.Network.Data;
using Aragas.Network.Packets;

using PCLExt.Config;

using PokeD.Core.Packets.P3D.Shared;
using PokeD.Server.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public partial class Server
    {
        private static SClient ServerClient { get; } = new SClient();
        private class SClient : Client
        {
            public override int ID { get; set; } = 0;
            public override string Nickname { get; protected set; } = "SERVER";
            public override Prefix Prefix { get; protected set; } = Prefix.NONE;
            public override string PasswordHash { get; set; } = string.Empty;
            public override Vector3 Position { get; set; } = Vector3.Zero;
            public override string LevelFile { get; set; } = string.Empty;
            public override PermissionFlags Permissions { get; set; } = PermissionFlags.Server;
            public override string IP { get; } = string.Empty;
            public override DateTime ConnectionTime { get; } = DateTime.MinValue;
            public override CultureInfo Language { get; }
            public override GameDataPacket GetDataPacket() => null;


            public override bool RegisterOrLogIn(string passwordHash) => false;
            public override bool ChangePassword(string oldPassword, string newPassword) => false;

            public override void SendPacket(Packet packet) { }

            public override void SendChatMessage(ChatMessage chatMessage) => Logger.Log(LogType.Chat, chatMessage.Message);
            public override void SendServerMessage(string text) => Logger.Log(LogType.Command, text);
            public override void SendPrivateMessage(ChatMessage chatMessage) { }

            public override void LoadFromDB(ClientTable data) {  }

            public override void Join() { }
            public override void Kick(string reason = "") { }
            public override void Ban(string reason = "") { }

            public override void Update() { }

            public override void Dispose() { }
        }

        [ConfigIgnore]
        public CommandManager CommandManager { get; }

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

            if (command.StartsWith("say "))
                NotifyServerMessage(null, message.Remove(0, 4));

            else if (command.StartsWith("message "))
                NotifyServerMessage(null, message.Remove(0, 8));

            else
                return ExecuteClientCommand(ServerClient, message);

            return true;
        }
    }
}