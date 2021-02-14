using System;
using System.Collections.Generic;

namespace PokeD.Server.Commands
{
    public abstract class ScriptCommandLoader
    {
        public abstract IEnumerable<ScriptCommand> LoadCommands(IServiceProvider serviceProvider);
    }
}