using System;

using PCLExt.Config;
using PCLExt.Config.Extensions;

using PokeD.Core;
using PokeD.Core.Services;
using PokeD.Server.Services;
using PokeD.Server.Storage.Files;

namespace PokeD.Server
{
    public class Server : IDisposable
    {
        private ConfigType ConfigType { get; }
        private IConfigFile ServerConfigFile => new ServerConfigFile(ConfigType);


        #region Settings

        private string _pokeApiUrl = "https://pokeapi.co/";
        public string PokeApiUrl
        {
            get => _pokeApiUrl;
            private set
            {
                if (value?.EndsWith("/") == false)
                    value += "/";
                _pokeApiUrl = value;
            }
        }

        public bool EnableDebug { get => Logger.EnableDebug; private set => Logger.EnableDebug = value; }

        //public bool AutomaticErrorReporting { get; private set; } = true;

        #endregion Settings

        [ConfigIgnore]
        public ServiceContainer Services { get; } = new ServiceContainer();

        private bool IsDisposed { get; set; }


        public Server(ConfigType configType)
        {
            ConfigType = configType;

            Logger.Log(LogType.Debug, "Adding basic services to Server...");
            Services.AddService(new SecurityService(Services, ConfigType));
            Services.AddService(new DatabaseService(Services, ConfigType));
            Services.AddService(new WorldService(Services, ConfigType));
            Services.AddService(new ChatChannelManagerService(Services, ConfigType));
            Services.AddService(new CommandManagerService(Services, ConfigType));
            Services.AddService(new ModuleManagerService(Services, ConfigType));
            Logger.Log(LogType.Debug, "Added basic services to Server.");
        }


        public bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(ServerConfigFile, this);
            if(!status)
                Logger.Log(LogType.Warning, "Failed to load Server settings!");
            
            Logger.Log(LogType.Debug, "Starting Services...");
            foreach (var service in Services)
                (service as IStartable)?.Start();
            Logger.Log(LogType.Debug, "Started Services.");

            return status;
        }
        public bool Stop()
        {
            var status = FileSystemExtensions.SaveConfig(ServerConfigFile, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save Server settings!");

            Logger.Log(LogType.Debug, "Stopping Server.");

            foreach (var service in Services)
                (service as IStoppable)?.Stop();

            Logger.Log(LogType.Debug, "Stopped Server.");

            return status;
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Logger.Log(LogType.Debug, "Disposing Server...");

                    foreach (var service in Services)
                        service?.Dispose();

                    Logger.Log(LogType.Debug, "Disposed Server.");
                }

                IsDisposed = true;
            }
        }
        ~Server()
        {
            Dispose(false);
        }
    }
}