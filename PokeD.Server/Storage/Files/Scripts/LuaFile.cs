using System;
using System.IO;
using System.Linq;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop.RegistrationPolicies;
using MoonSharp.Interpreter.Loaders;

using PCLExt.FileStorage;
using PCLExt.FileStorage.Extensions;

using PokeD.Core.Data.P3D;
using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Storage.Files
{
    [Flags]
    public enum LuaModules
    {
        None = 0,
        Hook = 1,
        Translator = 2
    }

    public class LuaFile : BaseFile
    {
        private class FileSystemScriptLoader : IScriptLoader
        {
            private static IFolder Modules => new LuaFolder().CreateFolder("modules", CreationCollisionOption.OpenIfExists);
            
            public string ResolveFileName(string filename, Table globalContext) => $"{filename}.lua";
            public string ResolveModuleName(string modname, Table globalContext) => $"module_{modname}";
            
            public object LoadFile(string file, Table globalContext)
            {
                if (file.StartsWith("module_"))
                {
                    using (var stream = new LuaFolder().GetFile(file).Open(PCLExt.FileStorage.FileAccess.Read))
                    using (var reader = new StreamReader(stream))
                        return reader.ReadToEnd();
                }
                else
                {
                    using (var stream = Modules.GetFile(file).Open(PCLExt.FileStorage.FileAccess.Read))
                    using (var reader = new StreamReader(stream))
                        return reader.ReadToEnd();
                }
            }
        }

        static LuaFile()
        {
            UserData.RegistrationPolicy = new AutomaticRegistrationPolicy();

            UserData.RegisterAssembly(typeof(P3DData).Assembly);
            UserData.RegisterAssembly(typeof(LuaFile).Assembly);
        }

        public Script Script { get; private set; }
        private LuaModules Modules { get; }

        public LuaFile(IFile luaFile, LuaModules luaModules) : base(luaFile)
        {
            Modules = luaModules;
            
            Reload();
        }

        public void Reload()
        {
            // Preset_HardSandbox = Bit32 | Math | Table | String | TableIterators | GlobalConsts | Basic,
            // Preset_SoftSandbox = Preset_HardSandbox | Json | Dynamic | OS_Time | Coroutine | ErrorHandling | Metatables,
            // Preset_Default = Preset_SoftSandbox | IO | OS_System | LoadMethods,
            // Preset_Complete = Preset_Default | Debug,
            Script = new Script(CoreModules.Preset_SoftSandbox)
            {
                Options = {ScriptLoader = new FileSystemScriptLoader()}
            };

            RegisterModules(Modules);
            
            var fileContent = this.ReadAllText();
            Script.DoString(fileContent);
        }

        private void RegisterModules(LuaModules luaModules, CoreModules modules = CoreModules.Preset_SoftSandbox)
        {
            foreach (Enum value in Enum.GetValues(typeof(LuaModules)))
                if (luaModules.HasFlag(value) && Convert.ToInt32(value) != 0)
                {
                    var name = value.ToString().ToLowerInvariant();
                    var table = AddDefaultFunctions(new Table(Script).RegisterCoreModules(modules));
                    Script.DoFile(name, table);
                    Script.Globals[name] = table;
                }
        }
        private Table CompileFile(string path)
        {
            var modules = CoreModules.Preset_SoftSandbox;
            IFolder folder = new LuaFolder();

            var dirs = path.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).Reverse().Skip(1).Reverse();
            foreach (var dir in dirs)
                folder = folder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists).Result;

            var file = System.IO.Path.GetFileName(path);
            var text = folder.GetFileAsync(file).Result.ReadAllTextAsync().Result;

            var table = AddDefaultFunctions(new Table(Script).RegisterCoreModules(modules));
            Script.DoString(text, table);

            return table;
        }
        private Table GetFiles(string path)
        {
            IFolder folder = new LuaFolder();

            var dirs = path.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).Reverse().Skip(1).Reverse();
            foreach (var dir in dirs)
                folder = folder.CreateFolder(dir, CreationCollisionOption.OpenIfExists);

            var files = folder.GetFilesAsync().Result;

            var table = new Table(Script);
            foreach (var file in files)
                table.Append(DynValue.NewString(file.Name));
            //table["files"] = files.Select(file => file.Name).ToList();
            return table;
        }

        private Table AddDefaultFunctions(Table table)
        {
            table["CompileFile"] = (Func<string, Table>)CompileFile;
            table["GetFiles"] = (Func<string, Table>)GetFiles;

            return table;
        }
    }
}