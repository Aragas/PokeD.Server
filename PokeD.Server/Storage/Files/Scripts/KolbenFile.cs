/*
using Kolben;

using PCLExt.FileStorage;
using PCLExt.FileStorage.Extensions;

namespace PokeD.Server.Storage.Files
{
    public class KolbenFile : BaseFile
    {
        public ScriptProcessor ScriptProcessor { get; private set; }
        
        public KolbenFile(IFile file) : base(file)
        {
            Reload();
        }

        public void Reload()
        {
            ScriptProcessor = new ScriptProcessor();

            var fileContent = this.ReadAllText();
            ScriptProcessor.Run(fileContent);
        }
    }
}
*/