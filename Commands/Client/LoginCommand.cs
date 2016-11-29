using System.Collections.Generic;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class LoginCommand : Command
    {
        public override string Name { get; protected set; } = "login";
        public override string Description { get; protected set; } = "Log in the Server.";
        public override IEnumerable<string> Aliases { get; protected set; } = new string[] { "l" };
        public override PermissionFlags Permissions { get; protected set; } = PermissionFlags.UnVerified;

        public LoginCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
                client.SendServerMessage(client.RegisterOrLogIn(arguments[0]) ? $"Succesfully logged in!" : $"Wrong password!");
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias){ client.SendServerMessage($"Correct usage is /{alias} <Password>"); }
    }
}