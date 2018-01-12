using Kolben;
using Kolben.Adapters;
using Kolben.Types;
using PokeD.Core.Services;
using PokeD.Server.Chat;
using PokeD.Server.Services;

namespace PokeD.Server.Storage.Files
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
            if (parameters.Length > 2 && parameters[0] is int && parameters[1] is string && parameters[2] is string)
                ChatManager.FindByName((string) parameters[1]).MessageSend(new ChatMessage(ModuleManager.GetClient((int) parameters[0]), (string) parameters[2]));

            return ScriptInAdapter.GetUndefined(processor);
        }
    }
}