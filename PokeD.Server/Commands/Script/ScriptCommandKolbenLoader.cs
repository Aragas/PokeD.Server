using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Services;
using PokeD.Server.Storage.Folders.Scripts;

namespace PokeD.Server.Commands
{
    public class ScriptCommandKolbenLoader : ScriptCommandLoader
    {
        private const string Identifier = "command_";

        public override IEnumerable<ScriptCommand> LoadCommands(IServiceContainer serviceContainer) => new KolbenFolder().GetScriptFiles()
            .Where(file => file.Name.ToLower().StartsWith(Identifier))
            .Select(file => new ScriptCommand(serviceContainer, new CommandScriptKolben(serviceContainer, file)));
    }
}