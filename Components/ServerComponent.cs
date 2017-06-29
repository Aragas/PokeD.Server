using PCLExt.Config;
using PCLExt.Config.Extensions;

using PokeD.Core.Components;

namespace PokeD.Server.Components
{
    public abstract class ServerComponent : IComponent, IStartable, IStoppable
    {
        protected virtual string ComponentName => GetType().Name;
        protected virtual IConfigFile ComponentConfigFile { get; }
        protected ConfigType ConfigType { get; }

        protected ServerComponent(ConfigType configType) { ConfigType = configType; }

        public virtual bool Start()
        {
            if (!FileSystemExtensions.LoadConfig(ComponentConfigFile, this))
            {
                Logger.Log(LogType.Warning, $"Failed to load {ComponentName} settings!");
                return false;
            }

            return true;
        }
        public virtual bool Stop()
        {
            if (!FileSystemExtensions.SaveConfig(ComponentConfigFile, this))
            {
                Logger.Log(LogType.Warning, $"Failed to save {ComponentName} settings!");
                return false;
            }

            return true;
        }

        public abstract void Update();

        public abstract void Dispose();
    }
}