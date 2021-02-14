using System;
using System.Collections.Generic;
using System.Linq;

using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class HelpCommand : Command
    {
        public override string Name => "help";
        public override string Description => "Command help menu.";
        public override IEnumerable<string> Aliases => new [] { "h" };
        public override PermissionFlags Permissions => PermissionFlags.UnVerifiedOrHigher;

        public HelpCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length > 1)
            {
                Help(client, alias);
                return;
            }

            var helpAlias = arguments.Length == 1 ? arguments[0] : "1";

            Command found;
            if ((found = CommandManager.FindByName(helpAlias)) != null)
            {
                found.Help(client, helpAlias);
                return;
            }
            if ((found = CommandManager.FindByAlias(helpAlias)) != null)
            {
                found.Help(client, helpAlias);
                return;
            }

            if (int.TryParse(helpAlias, out var pageNumber))
            {
                HelpPage(client, pageNumber);
                return;
            }
            Help(client, alias);
        }
        private void HelpPage(Client client, int page)
        {
            const int perPage = 5;
            var commands = CommandManager.GetCommands().Where(command => (client.Permissions & command.Permissions) != PermissionFlags.None).ToList();
            var numPages = (int) Math.Floor((double) commands.Count / perPage);
            if ((commands.Count % perPage) > 0)
                numPages++;

            if (page < 1 || page > numPages)
                page = 1;

            var startingIndex = (page - 1) * perPage;
            client.SendServerMessage($"--Help page {page} of {numPages}--");
            for (var i = 0; i < perPage; i++)
            {
                var index = startingIndex + i;
                if (index > commands.Count - 1)
                    break;

                var command = commands[index];
                client.SendServerMessage($"/{command.Name} - {command.Description}");
            }
        }

        public override void Help(Client client, string alias) { client.SendServerMessage($"Correct usage is /{alias} <page#/command> [command arguments]"); }
    }
}