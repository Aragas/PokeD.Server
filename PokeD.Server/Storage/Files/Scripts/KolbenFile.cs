using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Kolben;
using Kolben.Adapters;
using Kolben.Types;

using PCLExt.FileStorage;
using PCLExt.FileStorage.Extensions;

using PokeD.Core;
using PokeD.Core.Services;

using ScriptRuntimeException = Kolben.Adapters.ScriptRuntimeException;

namespace PokeD.Server.Storage.Files
{
    public class KolbenFile : BaseFile
    {
        public static IServiceContainer Container { get; set; }
        public ScriptProcessor ScriptProcessor { get; private set; }

        private Dictionary<string, MethodInfo[]> ApiClasses { get; set; }
        private List<SObject> PrototypeBuffer { get; set; }

        public SObject this[string name]
        {
            get => ScriptContextManipulator.GetVariable(ScriptProcessor, name);
            set => ScriptContextManipulator.AddVariable(ScriptProcessor, name, value);
        }

        public object CallFunction(string name, params object[] args)
        {
            var func = (SFunction) this[name];
            return ScriptOutAdapter.Translate(func.Call(ScriptProcessor, null, null, args.Select(arg => ScriptInAdapter.Translate(ScriptProcessor, arg)).ToArray()));
        }

        public KolbenFile(IFile file) : base(file)
        {
            Reload();
        }

        public void Reload()
        {
            var fileContent = this.ReadAllText();
            RunScript(fileContent);
        }

        private void RunScript(string source)
        {
#if NOPE
            Task.Run(() =>
                {
#endif
                    try
                    {
                        ScriptProcessor = CreateProcessor();
                        var result = ScriptProcessor.Run(source);
                        if (ScriptContextManipulator.ThrownRuntimeError(ScriptProcessor))
                        {
                            var exObj = ScriptOutAdapter.Translate(result);
                            if (exObj is ScriptRuntimeException runtimeException)
                                throw runtimeException;
                        }
                    }
                    catch (ScriptRuntimeException ex)
                    {
                        Logger.Log(LogType.Error, $"Script execution failed at runtime. {ex.Type} (L{ex.Line}: {ex.Message})");
                    }
#if NOPE
                }
            );
#endif
        }

        private ScriptProcessor CreateProcessor()
        {
            if (PrototypeBuffer is null)
                InitializePrototypeBuffer();

            var processor = new ScriptProcessor(PrototypeBuffer);
            ScriptContextManipulator.SetCallbackExecuteMethod(processor, ExecuteMethod);
            return processor;
        }

        private void InitializePrototypeBuffer()
        {
            PrototypeBuffer = new List<SObject>();

            var processor = new ScriptProcessor();

            foreach (var o in typeof(KolbenFile).Assembly.GetTypes().Where(t => t.GetCustomAttributes(typeof(ScriptPrototypeAttribute), true).Length > 0))
                PrototypeBuffer.Add(ScriptInAdapter.Translate(processor, o));
        }

        private SObject ExecuteMethod(ScriptProcessor processor, string className, string methodName, SObject[] parameters)
        {
            if (ApiClasses is null)
                InitializeApiClasses();

            if (ApiClasses.ContainsKey(className))
            {
                var method = ApiClasses[className].FirstOrDefault(m => m.Name == methodName);
                if (method != null)
                {
                    var result = method.Invoke(null, new object[] { processor, parameters });
                    return result as SObject;
                }
            }

            return ScriptInAdapter.GetUndefined(processor);
        }

        private void InitializeApiClasses()
        {
            ApiClasses = new Dictionary<string, MethodInfo[]>();

            /*
            foreach (var o in typeof(KolbenFile).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ApiClass)) && t.GetCustomAttributes(typeof(ApiClassAttribute), true).Length > 0))
            {
                var attr = o.GetCustomAttribute<ApiClassAttribute>();
                var methods = o.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m =>
                    m.GetCustomAttributes(typeof(ApiMethodSignatureAttribute), true).Length > 0).ToArray();
            }
            */
        }
    }
}