using Kolben;
using Kolben.Adapters;
using Kolben.Types;
using PokeD.Core.Services;
using PokeD.Server.Chat;
using PokeD.Server.Services;

namespace PokeD.Server.Storage.Files.Scripts.Kolben
{
    [ApiClass("Chat")]
    public class ChatWrapper : ApiClass
    {
        private static IServiceContainer _serviceContainer;
        private static ChatChannelManagerService ChatManager => _serviceContainer.GetService<ChatChannelManagerService>();
        private static ModuleManagerService ModuleManager => _serviceContainer.GetService<ModuleManagerService>();

        public ChatWrapper(IServiceContainer serviceContainer) => _serviceContainer = serviceContainer;

        //[ApiMethodSignature]
        public static SObject Send(ScriptProcessor processor, object[] parameters)
        {
            if (parameters.Length > 2 && parameters[0] is int clientID && parameters[1] is string name && parameters[2] is string message)
                ChatManager.FindByName(name).MessageSend(new ChatMessage(ModuleManager.GetClient(clientID), message));

            return ScriptInAdapter.GetUndefined(processor);
        }
    }
}