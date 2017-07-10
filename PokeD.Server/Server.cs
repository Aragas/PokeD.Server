using System;

using PCLExt.Config;
using PCLExt.Config.Extensions;

using PokeD.Core.Data.PokeApi;
using PokeD.Server.Services;
using PokeD.Server.Storage.Files;

namespace PokeD.Server
{
    public partial class Server : IDisposable
    {
        private ConfigType ConfigType { get; }

        private IConfigFile ServerConfigFile => new ServerConfigFile(ConfigType);


        #region Settings

        public string PokeApiUrl
        {
            get => ResourceUri.URL;
            private set
            {
                if (!value.EndsWith("/"))
                    value += "/";
                ResourceUri.URL = value;
            }
        }

        public PokeApiV2.CacheTypeEnum CacheType { get => PokeApiV2.CacheType; private set => PokeApiV2.CacheType = value; } 
        public bool PreCacheData { get; private set; } = false;

        public bool EnableDebug { get => Logger.EnableDebug; private set => Logger.EnableDebug = value; }


        //public bool AutomaticErrorReporting { get; private set; } = true;

        #endregion Settings

        [ConfigIgnore]
        public bool IsDisposing { get; private set; }


        public Server(ConfigType configType)
        {
            ConfigType = configType;

            Logger.Log(LogType.Debug, $"Adding basic services to Server...");
            //Services.AddService(new SecurityService(this, ConfigType));
            Services.AddService(new DatabaseService(this, ConfigType));
            Services.AddService(new WorldService(this, ConfigType));
            Services.AddService(new ChatChannelManagerService(this, ConfigType));
            Services.AddService(new CommandManagerService(this, ConfigType));
            Services.AddService(new ModuleManagerService(this, ConfigType));
            Logger.Log(LogType.Debug, $"Added basic services to Server.");
        }


        public bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(ServerConfigFile, this);
            if(!status)
                Logger.Log(LogType.Warning, "Failed to load Server settings!");


            if (PreCacheData)
            {
                Logger.Log(LogType.Info, "Pre Cache enabled, caching data...");
                PreCache();
                Logger.Log(LogType.Info, "Caching data done.");
            }

            Logger.Log(LogType.Debug, $"Starting Services...");
            foreach (var service in Services)
                (service as IStartable)?.Start();
            Logger.Log(LogType.Debug, $"Started Services.");

            return status;
        }
        public bool Stop()
        {
            var status = FileSystemExtensions.SaveConfig(ServerConfigFile, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save Server settings!");

            Logger.Log(LogType.Debug, $"Stopping Server.");

            foreach (var service in Services)
                (service as IStoppable)?.Stop();

            Logger.Log(LogType.Debug, $"Stopped Server.");

            return status;
        }

        
        /*
        public void Update()
        {
            foreach (var service in Services)
                (service as IUpdatable)?.Update();
        }
        */


        public void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;

            Logger.Log(LogType.Debug, $"Disposing Server...");

            foreach (var service in Services)
                (service as IDisposable)?.Dispose();

            Logger.Log(LogType.Debug, $"Disposed Server.");
        }
    }
}