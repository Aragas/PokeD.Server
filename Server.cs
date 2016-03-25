using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Aragas.Core.Interfaces;
using Aragas.Core.Wrappers;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;

using PokeD.Core.Data.PokeApi;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public partial class Server : IUpdatable, IDisposable
    {
        const string FileName = "Server";

        #region Settings

        public string PlayerDatabaseName { get; set; } = "Players";

        public string PokeApiUrl { get; set; } = "http://pokeapi.co/";

        public bool AutomaticErrorReporting { get; private set; } = true;

        public World World { get; set; } = new World();

        public List<string> Blacklist { get; private set; }
        public List<string> Whitelist { get; private set; }
        public List<string> Oplist { get; private set; }

        #endregion Settings

        [ConfigIgnore]
        public List<IServerModule> Modules { get; } = new List<IServerModule>();
        
        IThread ListenToConnectionsThread { get; set; }


        [ConfigIgnore]
        public bool IsDisposing { get; private set; }

        [ConfigIgnore]
        public AsymmetricCipherKeyPair RSAKeyPair { get; private set; }
        const int RsaKeySize = 1024;

        Aragas.Core.Wrappers.Database Database { get; set; }


        public Server()
        {
            Modules.Add(new ModuleP3D(this));
            Modules.Add(new ModuleSCON(this));
            Modules.Add(new ModulePokeD(this));
            Modules.Add(new ModuleNPC(this));
            Modules.Add(new ModuleNancy(this));
            Modules.Add(new ModuleP3DProxy(this));

            CommandManager =     new CommandManager(this);
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
            var status = FileSystemWrapper.LoadSettings(FileName, this);
            if(!status)
                Logger.Log(LogType.Warning, "Failed to load Server settings!");


            if (!PokeApiUrl.EndsWith("/"))
                PokeApiUrl = PokeApiUrl + "/";
            ResourceUri.URL = PokeApiUrl;


            Logger.Log(LogType.Info, "Generating RSA key pair.");
            RSAKeyPair = GenerateKeyPair();


            Logger.Log(LogType.Info, $"Loading {PlayerDatabaseName}.");
            Database = DatabaseWrapper.Create(PlayerDatabaseName);
            Database.CreateTable<Player>();


            Logger.Log(LogType.Info, $"Starting Server.");

            var toRemove = Modules.Where(module => !module.Start()).ToList();
            foreach (var module in toRemove)
                Modules.Remove(module);

            ListenToConnectionsThread = ThreadWrapper.Create(ListenToConnectionsCycle);
            ListenToConnectionsThread.Name = "ListenToConnectionsThread";
            ListenToConnectionsThread.IsBackground = true;
            ListenToConnectionsThread.Start();
          

            return status;
        }
        public bool Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save Server settings!");

            Logger.Log(LogType.Info, $"Stopping Server.");


            if (ListenToConnectionsThread.IsRunning)
                ListenToConnectionsThread.Abort();

            foreach (var module in Modules)
                module.Stop();


            Dispose();

            Logger.Log(LogType.Info, $"Stopped Server.");

            return status;
        }


        public static long ClientConnectionsThreadTime { get; private set; }
        private void ListenToConnectionsCycle()
        {
            foreach (var module in Modules)
                module.StartListen();
            

            var watch = Stopwatch.StartNew();
            while (!IsDisposing)
            {
                foreach (var module in Modules)
                    module.CheckListener();


                if (watch.ElapsedMilliseconds < 250)
                {
                    ClientConnectionsThreadTime = watch.ElapsedMilliseconds;

                    var time = (int) (250 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    ThreadWrapper.Sleep(time);
                }

                watch.Reset();
                watch.Start();
            }
        }


        public void Update()
        {
            foreach (var module in Modules)
                module.Update();
        }

        
        public void Dispose()
        {
            if(IsDisposing)
                return;

            IsDisposing = true;


            World.Dispose();
        }
    }
}
