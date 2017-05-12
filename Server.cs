using System;
using System.Collections.Generic;
using System.IO;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;

using PCLExt.Config;
using PCLExt.Config.Extensions;

using PokeD.Core;
using PokeD.Core.Data.PokeApi;
using PokeD.Server.Chat;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Storage.Files;
using PokeD.Server.Storage.Folders;

using SQLite;

namespace PokeD.Server
{
    public partial class Server : IUpdatable, IDisposable
    {
        private const int RsaKeySize = 1024;

        private IConfigFile ServerConfigFile => new ServerConfigFile(ConfigType);

        [ConfigIgnore]
        public ConfigType ConfigType { get; }

        #region Settings

        public string DatabaseName { get; private set; } = "Database";

        public string PokeApiUrl
        {
            get { return ResourceUri.URL; }
            private set
            {
                if (!value.EndsWith("/"))
                    value += "/";
                ResourceUri.URL = value;
            }
        }

        public PokeApiV2.CacheTypeEnum CacheType { get { return PokeApiV2.CacheType; } private set { PokeApiV2.CacheType = value; } } 
        public bool PreCacheData { get; private set; } = false;

        //public bool AutomaticErrorReporting { get; private set; } = true;

        public World World { get; private set; } = new World();

        #endregion Settings

        [ConfigIgnore]
        public List<ServerModule> Modules { get; } = new List<ServerModule>();


        [ConfigIgnore]
        public bool IsDisposing { get; private set; }

        [ConfigIgnore]
        public AsymmetricCipherKeyPair RSAKeyPair { get; private set; }


        public Server(ConfigType configType)
        {
            ConfigType = configType;

            Modules.Add(new ModuleSCON(this));
            //Modules.Add(new ModuleNPC(this));
            Modules.Add(new ModuleP3D(this));
            //Modules.Add(new ModulePokeD(this));

            Modules.AddRange(LoadModules());

            CommandManager = new CommandManager(this);
            ChatChannelManager = new ChatChannelManager(this);
        }

        private static AsymmetricCipherKeyPair GenerateKeyPair()
        {
            var secureRandom = new SecureRandom(new DigestRandomGenerator(new Sha512Digest()));
            var keyGenerationParameters = new KeyGenerationParameters(secureRandom, RsaKeySize);

            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            return keyPairGenerator.GenerateKeyPair();
        }


        public bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(ServerConfigFile, this);
            if(!status)
                Logger.Log(LogType.Warning, "Failed to load Server settings!");


            if (PreCacheData)
            {
                Logger.Log(LogType.Info, "Pre Cache enabled, caching data.");
                PreCache();
            }

            Logger.Log(LogType.Info, "Generating RSA key pair.");
            RSAKeyPair = GenerateKeyPair();


            Logger.Log(LogType.Info, $"Loading {DatabaseName}...");
            Database = new SQLiteConnection(Path.Combine(new DatabaseFolder().Path, $"{DatabaseName}.sqlite3"));
            CreateTables();

            Logger.Log(LogType.Info, $"Starting Server.");

            Modules.RemoveAll(module => !module.Start()); // -- Removes all modules that failed to start.


            return status;
        }
        public bool Stop()
        {
            var status = FileSystemExtensions.SaveConfig(ServerConfigFile, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save Server settings!");

            Logger.Log(LogType.Info, $"Stopping Server.");


            foreach (var module in Modules)
                module.Stop();


            Dispose();

            Logger.Log(LogType.Info, $"Stopped Server.");

            return status;
        }


        public void Update()
        {
            World.Update();

            foreach (var module in Modules)
                module.Update();
        }


        public void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;


            foreach (var module in Modules)
                module.Dispose();
            Modules.Clear();

            World.Dispose();
        }
    }
}