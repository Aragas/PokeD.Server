using System;

using PokeD.Core.Services;
using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class ShowPermissionsCommand : Command
    {
        public override string Name => "showperm";
        public override string Description => "Show available Client permissions.";
        public override PermissionFlags Permissions => PermissionFlags.AdministratorOrHigher;

        public ShowPermissionsCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments) => client.SendServerMessage(string.Join(",", Enum.GetNames(typeof(PermissionFlags))));

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <Permission>");
    }
}