using PokeD.Core.Services;
using PokeD.Server.Clients;

namespace PokeD.Server.Commands.Chat
{
    public class SayCommand : Command
    {
        public override string Name => "say";
        public override string Description => "Speak as the Server.";
        public override PermissionFlags Permissions => PermissionFlags.AdministratorOrHigher;

        public SayCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
            {
                var message = arguments[0].TrimStart('"').TrimEnd('"');
                ModuleManager.SendServerMessage(message);
            }
            else if (arguments.Length > 1)
            {
                var message = string.Join(" ", arguments);
                ModuleManager.SendServerMessage(message);
            }
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <Message>");
    }
}
