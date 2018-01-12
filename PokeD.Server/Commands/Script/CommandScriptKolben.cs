using System.Collections.Generic;

using Kolben.Adapters;

using PokeD.Core.Services;
using PokeD.Server.Clients;
using PokeD.Server.Storage.Files;

namespace PokeD.Server.Commands
{
    public class CommandScriptKolben : BaseCommandScript
    {
        private KolbenFile KolbenFile { get; }

        public override string Name => (string) ScriptOutAdapter.Translate(KolbenFile["Name"]);
        public override string Description => (string) ScriptOutAdapter.Translate(KolbenFile["Description"]) ?? string.Empty;
        public override IEnumerable<string> Aliases => new List<string>() {"pong"};//((IEnumerable<string>) ScriptOutAdapter.Translate(KolbenFile["Aliases"])).Select(s => s.Replace("\"", ""));
        public override PermissionFlags Permission => ParsePermissionFlags((string) ScriptOutAdapter.Translate(KolbenFile["Permission"]) ?? string.Empty);

        public CommandScriptKolben(IServiceContainer serviceContainer, KolbenFile kolbenFile) : base(serviceContainer)
        {
            KolbenFile = kolbenFile;
            KolbenFile.Container = serviceContainer;
            //KolbenFile["World"] = World;
        }

        public override void Handle(Client client, string alias, string[] arguments) => KolbenFile.CallFunction("Handle", new ClientPrototype(client));

        public override void Help(Client client, string alias) => KolbenFile.CallFunction("Help", new ClientPrototype(client), alias);
    }
}