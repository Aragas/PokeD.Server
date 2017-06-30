using PCLExt.FileStorage;

using PokeD.Core.Storage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class ContentFolder : BaseFolder
    {
        public ContentFolder() : base(new MainFolder().CreateFolder("Content", CreationCollisionOption.OpenIfExists)) { }
    }
}