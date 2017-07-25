using System.Collections.Generic;

using PokeD.Core.Services;

namespace PokeD.Server.Commands
{
    public abstract class ScriptCommandLoader
    {
        public abstract IEnumerable<ScriptCommand> LoadCommands(IServiceContainer serviceContainer);
    }
}