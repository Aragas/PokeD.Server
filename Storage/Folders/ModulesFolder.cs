using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Aragas.Network.Extensions;

using ICSharpCode.SharpZipLib.Zip;

using PCLExt.AppDomain;
using PCLExt.FileStorage;

using PokeD.Core.Storage.Folders;

namespace PokeD.Server.Storage.Folders
{
    /// <summary>
    /// A .Zip file pls.
    /// </summary>
    public class ModuleFile : BaseFile
    {
        private Assembly ModuleAssembly { get; }
        private IList<string> DependencyAssemblyNames { get; } = new List<string>();

        public ModuleFile(IFile file) : base(file)
        {
            using (var stream = Open(FileAccess.Read))
            using (var zipFile = new ZipFile(stream))
            {
                foreach (ZipEntry zipEntry in zipFile)
                {
                    var name = PortablePath.GetFileNameWithoutExtension(zipEntry.Name);
                    if (!zipEntry.IsFile || !string.Equals(PortablePath.GetExtension(zipEntry.Name), ".dll", StringComparison.OrdinalIgnoreCase))
                        continue; // -- Ignore directories

                    DependencyAssemblyNames.Add(name);

                    if (name.StartsWith("m_"))
                        using (var zipStream = zipFile.GetInputStream(zipEntry))
                            ModuleAssembly = AppDomain.LoadAssembly(zipStream.ReadFully());
                }
            }
        }

        public Assembly GetAssembly(string assemblyName)
        {
            var assnName = new AssemblyName(assemblyName).Name;
            if (DependencyAssemblyNames.Contains(assnName))
            {
                using (var stream = Open(FileAccess.Read))
                using (var zipFile = new ZipFile(stream))
                {
                    foreach (ZipEntry zipEntry in zipFile)
                    {
                        if (!zipEntry.IsFile)
                            continue; // -- Ignore directories

                        if(string.Equals(PortablePath.GetFileNameWithoutExtension(zipEntry.Name), assnName, StringComparison.OrdinalIgnoreCase))
                            using (var zipStream = zipFile.GetInputStream(zipEntry))
                                return AppDomain.LoadAssembly(zipStream.ReadFully());
                    }
                }
            }

            return null;
        }
        public async Task<Assembly> GetAssemblyAsync(string assemblyName)
        {
            var assnName = new AssemblyName(assemblyName).Name;
            if (DependencyAssemblyNames.Contains(assnName))
            {
                using (var stream = await OpenAsync(FileAccess.Read))
                using (var zipFile = new ZipFile(stream))
                {
                    foreach (ZipEntry zipEntry in zipFile)
                    {
                        if (!zipEntry.IsFile)
                            continue; // -- Ignore directories

                        if (string.Equals(PortablePath.GetFileNameWithoutExtension(zipEntry.Name), assnName, StringComparison.OrdinalIgnoreCase))
                            using (var zipStream = zipFile.GetInputStream(zipEntry))
                                return AppDomain.LoadAssembly(zipStream.ReadFully());
                    }
                }
            }

            return null;
        }

        public Assembly GetModule() => ModuleAssembly;
    }

    public class ModulesFolder : BaseFolder
    {
        public ModulesFolder() : base(new MainFolder().CreateFolder("Modules", CreationCollisionOption.OpenIfExists)) { }

        public IList<ModuleFile> GetModuleFiles() => GetFiles("*.mod").Select(moduleFile => new ModuleFile(moduleFile)).ToList();
        public async Task<IList<ModuleFile>> GetModuleFilesAsync() => (await GetFilesAsync("*.mod")).Select(moduleFile => new ModuleFile(moduleFile)).ToList();
    }
}