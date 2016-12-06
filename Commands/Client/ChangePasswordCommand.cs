using System.Collections.Generic;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class ChangePasswordCommand : Command
    {
        public override string Name { get; protected set; } = "changepassword";
        public override string Description { get; protected set; } = "Change Players Password.";
        public override IEnumerable<string> Aliases { get; protected set; } = new [] { "cp" };
        public override PermissionFlags Permissions { get; protected set; } = PermissionFlags.VerifiedOrHigher;
        public override bool LogCommand { get; protected set; } = false;

        public ChangePasswordCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 2)
                client.SendServerMessage(client.ChangePassword(arguments[0], arguments[1]) ? $"Succesfully changed password!" : $"Wrong old password!");
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias){ client.SendServerMessage($"Correct usage is /{alias} <OldPassword> <NewPassword>"); }
    }
}