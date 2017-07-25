using PCLExt.FileStorage;
using PCLExt.FileStorage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class ContentFolder : BaseFolder
    {
        public ContentFolder() : base(new ApplicationFolder().CreateFolder("Content", CreationCollisionOption.OpenIfExists)) { }
    }
}