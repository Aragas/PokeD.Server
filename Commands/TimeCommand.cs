using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class TimeCommand : Command
    {
        public override string Name => "time";
        public override string Description => "Shows the current world time.";

        public override void Handle(Client client, string alias, string[] arguments) { client.SendMessage(CommandManager.Server.World.CurrentTime.ToString()); }

        public override void Help(Client client, string alias) { client.SendMessage($"/{alias}: {Description}"); }
    }
}
