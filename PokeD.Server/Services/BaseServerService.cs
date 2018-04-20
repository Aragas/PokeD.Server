using PCLExt.Config;
using PCLExt.Config.Extensions;

using PokeD.Core;
using PokeD.Core.Services;

namespace PokeD.Server.Services
{
    public abstract class BaseServerService : IService, IStartable, IStoppable
    {
        [ConfigIgnore]
        public IServiceContainer Services { get; }

        protected virtual string ServiceName => GetType().Name;
        protected virtual IConfigFile ServiceConfigFile { get; }
        protected ConfigType ConfigType { get; }

        protected BaseServerService(IServiceContainer services, ConfigType configType) { Services = services; ConfigType = configType; }

        public virtual bool Start()
        {
            if (!FileSystemExtensions.LoadConfig(ServiceConfigFile, this))
            {
                Logger.Log(LogType.Warning, $"Failed to load {ServiceName} settings!");
                return false;
            }

            return true;
        }
        public virtual bool Stop()
        {
            if (!FileSystemExtensions.SaveConfig(ServiceConfigFile, this))
            {
                Logger.Log(LogType.Warning, $"Failed to save {ServiceName} settings!");
                return false;
            }

            return true;
        }

        public abstract void Dispose();
    }
}