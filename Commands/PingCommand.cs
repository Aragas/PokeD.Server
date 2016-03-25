using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class PingCommand : Command
    {
        public override string Name => "ping";
        public override string Description => "Ping pong";

        public override void Handle(Client client, string alias, string[] arguments) { client.SendMessage("Pong!"); }

        public override void Help(Client client, string alias) { client.SendMessage($"Correct usage is /{alias}"); }
    }
}
