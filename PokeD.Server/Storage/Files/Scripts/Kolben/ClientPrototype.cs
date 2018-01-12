using Kolben.Adapters;
using PokeD.Core.Services;
using PokeD.Server.Clients;
using PokeD.Server.Services;

namespace PokeD.Server.Storage.Files
{
    [ScriptPrototype(VariableName = "Client")]
    public class ClientPrototype
    {
        public static Client GetClient(object prototyte) => ((ClientPrototype) prototyte)._reference;
        
        [Reference]
        private Client _reference;

        private static IServiceContainer _serviceContainer;
        private static ModuleManagerService ModuleManager => _serviceContainer.GetService<ModuleManagerService>();

        public ClientPrototype() { }
        public ClientPrototype(IServiceContainer serviceContainer) => _serviceContainer = serviceContainer;
        public ClientPrototype(Client client) => _reference = client;

        [ScriptFunction(ScriptFunctionType.Constructor, VariableName = "constructor")]
        public static object Constructor(object @this, ScriptObjectLink objectLink, object[] parameters)
        {
            if (parameters.Length > 0 && parameters[0] is int)
                objectLink.SetReference(nameof(_reference), ModuleManager.GetClient((int) parameters[0]));

            return NetUndefined.Instance;
        }

        [ScriptFunction(ScriptFunctionType.Getter, VariableName = "name")]
        public static object GetName(object @this, ScriptObjectLink objectLink, object[] parameters) => GetClient(@this).Name;

        [ScriptFunction(ScriptFunctionType.Getter, VariableName = "id")]
        public static object GetID(object @this, ScriptObjectLink objectLink, object[] parameters) => GetClient(@this).ID;

        [ScriptFunction(ScriptFunctionType.Standard, VariableName = "sendServerMessage")]
        public static object SendServerMessage(object @this, ScriptObjectLink objectLink, object[] parameters)
        {
            if (parameters.Length > 0 && parameters[0] is string)
                GetClient(@this).SendServerMessage((string) parameters[0]);

            return NetUndefined.Instance;
        }
    }
}