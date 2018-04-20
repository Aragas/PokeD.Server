using System.Collections.Generic;

using Aragas.Network.Data;

using PokeD.Core.Services;
using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class ChangePasswordCommand : Command
    {
        public override string Name => "changepassword";
        public override string Description => "Change your Password.";
        public override IEnumerable<string> Aliases => new [] { "cp" };
        public override PermissionFlags Permissions => PermissionFlags.UserOrHigher ^ PermissionFlags.Server;
        public override bool LogCommand => false;

        public ChangePasswordCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 2)
                client.SendServerMessage(client.ChangePassword(new PasswordStorage(arguments[0]).Hash, new PasswordStorage(arguments[1]).Hash) ? "Succesfully changed password!" : "Wrong old password!");
            else
                client.SendServerMessage("Invalid arguments given.");
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <OldPassword> <NewPassword>");
    }
}