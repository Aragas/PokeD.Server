using System;
using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public sealed class ScriptCommand : Command
    {
        private BaseCommandScript Script { get; }

        public override string Name => Script.Name;
        public override string Description => Script.Description;
        public override IEnumerable<string> Aliases => Script.Aliases;
        public override PermissionFlags Permissions => Script.Permission;

        public ScriptCommand(IServiceProvider serviceProvider, BaseCommandScript script) : base(serviceProvider) => Script = script;

        public override void Handle(Client client, string alias, string[] arguments) => Script.Handle(client, alias, arguments);

        public override void Help(Client client, string alias) => Script.Help(client, alias);
    }
}