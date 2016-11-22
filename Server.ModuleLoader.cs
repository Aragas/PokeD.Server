using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Aragas.Network.Extensions;

using PCLExt.AppDomain;
using PCLExt.FileStorage;

namespace PokeD.Server
{
    public partial class Server
    {
        private const string Identifier = "m_";
        private const string Extension = ".dll";

        private IEnumerable<ServerModule> LoadModules()
        {
            AppDomain.AssemblyResolve(AssemblyResolve);

            var files = Storage.ContentFolder.GetFilesAsync().Result;
            var fModules = files.Where(file => file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension));

            foreach(var fModule in fModules)
            {
                using (var stream = fModule.OpenAsync(FileAccess.Read).Result)
                {
                    var data = stream.ReadFully();
                    var assembly = AppDomain.LoadAssembly(data);

                    var serverModule = assembly.ExportedTypes.SingleOrDefault(type => type.GetTypeInfo().IsSubclassOf(typeof(ServerModule)) && !type.GetTypeInfo().IsAbstract);
                    if(serverModule != null)
                        yield return (ServerModule) Activator.CreateInstance(serverModule, new object[] { this });
                }
            }
        }
        private Assembly AssemblyResolve(string name, Assembly assemblyCaller)
        {
            var assemblyName = $"{new AssemblyName(name).Name}.dll";
            if (Storage.ContentFolder.CheckExistsAsync(assemblyName).Result == ExistenceCheckResult.FileExists)
            {
                using (var stream = Storage.ContentFolder.GetFileAsync(assemblyName).Result.OpenAsync(FileAccess.Read).Result)
                {
                    var data = stream.ReadFully();
                    return AppDomain.LoadAssembly(data);
                }
            }

            return null;
        }
    }
}