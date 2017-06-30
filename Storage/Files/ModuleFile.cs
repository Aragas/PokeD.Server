using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

using Aragas.Network.Extensions;

using PCLExt.FileStorage;

namespace PokeD.Server.Storage.Folders
{
    /// <summary>
    /// A .Zip file pls.
    /// </summary>
    public class ModuleFile : BaseFile
    {
        private Assembly ModuleAssembly { get; }
        private Dictionary<string, Assembly> DependencyAssemblyNames { get; } = new Dictionary<string, Assembly>();

        public ModuleFile(IFile file) : base(file)
        {
            using (var stream = Open(FileAccess.Read))
            using (var zipFile = new ZipArchive(stream))
            {
                foreach (var zipEntry in zipFile.Entries)
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(zipEntry.Name);
                    if (string.IsNullOrEmpty(zipEntry.Name) || !string.Equals(System.IO.Path.GetExtension(zipEntry.Name), ".dll", StringComparison.OrdinalIgnoreCase))
                        continue; // -- Ignore directories

                    using (var zipStream = zipEntry.Open())
                    {
                        var data = zipStream.ReadFully();
                        DependencyAssemblyNames.Add(name, AppDomain.CurrentDomain.Load(data));
                    }

                    if (name.StartsWith("m_"))
                        using (var zipStream = zipEntry.Open())
                            ModuleAssembly = AppDomain.CurrentDomain.Load(zipStream.ReadFully());
                }
            }
        }

        public Assembly GetAssembly(string assemblyName)
        {
            var assnName = new AssemblyName(assemblyName).Name;
            if (DependencyAssemblyNames.ContainsKey(assnName))
            {
                return DependencyAssemblyNames[assnName];
                /*
                using (var stream = Open(FileAccess.Read))
                using (var zipFile = new ZipArchive(stream))
                {
                    foreach (var zipEntry in zipFile.Entries)
                    {
                        if (string.IsNullOrEmpty(zipEntry.Name))
                            continue; // -- Ignore directories

                        if(string.Equals(System.IO.Path.GetFileNameWithoutExtension(zipEntry.Name), assnName, StringComparison.OrdinalIgnoreCase))
                            using (var zipStream = zipEntry.Open())
                                return AppDomain.CurrentDomain.Load(zipStream.ReadFully());
                    }
                }
                */
            }

            return null;
        }
        public async Task<Assembly> GetAssemblyAsync(string assemblyName)
        {
            var assnName = new AssemblyName(assemblyName).Name;
            if (DependencyAssemblyNames.ContainsKey(assnName))
            {
                return DependencyAssemblyNames[assnName];
                /*
                using (var stream = await OpenAsync(FileAccess.Read))
                using (var zipFile = new ZipArchive(stream))
                {
                    foreach (var zipEntry in zipFile.Entries)
                    {
                        if (string.IsNullOrEmpty(zipEntry.Name))
                            continue; // -- Ignore directories

                        if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(zipEntry.Name), assnName, StringComparison.OrdinalIgnoreCase))
                            using (var zipStream = zipEntry.Open())
                                return AppDomain.CurrentDomain.Load(zipStream.ReadFully());
                    }
                }
                */
            }

            return null;
        }

        public Assembly GetModule() => ModuleAssembly;
    }
}