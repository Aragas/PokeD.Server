using System;

using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class HelpCommand : Command
    {
        public override string Name { get; protected set; } = "help";
        public override string Description { get; protected set; } = "Command help menu.";

        public HelpCommand(Server server) : base(server) { }

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

            int pageNumber;
            if (int.TryParse(helpAlias, out pageNumber))
            {
                HelpPage(client, pageNumber);
                return;
            }
            Help(client, alias);
        }
        private void HelpPage(Client client, int page)
        {
            const int perPage = 5;
            var numPages = (int) Math.Floor((double) CommandManager.Commands.Count / perPage);
            if ((CommandManager.Commands.Count % perPage) > 0)
                numPages++;

            if (page < 1 || page > numPages)
                page = 1;

            var startingIndex = (page - 1) * perPage;
            client.SendServerMessage($"--Help page {page} of {numPages}--");
            for (var i = 0; i < perPage; i++)
            {
                var index = startingIndex + i;
                if (index > CommandManager.Commands.Count - 1)
                    break;
                
                var command = CommandManager.Commands[index];
                client.SendServerMessage($"/{command.Name} - {command.Description}");
            }
        }

        public override void Help(Client client, string alias) { client.SendServerMessage($"Correct usage is /{alias} <page#/command> [command arguments]"); }
    }
}