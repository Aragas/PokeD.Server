using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PCLExt.AppDomain;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server
{
    public partial class Server
    {
        private IEnumerable<ServerModule> LoadModules()
        {
            AppDomain.AssemblyResolve(AssemblyResolve);

            foreach (var moduleFile in new ModulesFolder().GetModuleFiles())
            {
                var assembly = moduleFile.GetModule();

                var serverModule = assembly?.ExportedTypes.SingleOrDefault(type => type.GetTypeInfo().IsSubclassOf(typeof(ServerModule)) && !type.GetTypeInfo().IsAbstract);
                if (serverModule != null)
                    yield return (ServerModule)Activator.CreateInstance(serverModule, new object[] { this });
            }
        }
        private Assembly AssemblyResolve(string name, Assembly assemblyCaller)
        {
            foreach (var moduleFile in new ModulesFolder().GetModuleFiles())
            {
                var assembly = moduleFile.GetAssembly(name);
                if (assembly != null) return assembly;
            }
            return null;
        }
    }
}