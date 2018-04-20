using System.Collections.Generic;

using PokeD.Core.Services;
using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class LoginCommand : Command
    {
        public override string Name => "login";
        public override string Description => "Log in the Server.";
        public override IEnumerable<string> Aliases => new [] { "l" };
        public override PermissionFlags Permissions => PermissionFlags.UnVerified;
        public override bool LogCommand => false;

        public LoginCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
                client.SendServerMessage(client.RegisterOrLogIn(arguments[0]) ? "Succesfully logged in!" : "Wrong password!");
            else
                client.SendServerMessage("Invalid arguments given.");
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <Password>");
    }
}