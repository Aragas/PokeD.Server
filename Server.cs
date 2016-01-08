using System;
using System.Collections.Generic;
using System.Diagnostics;

using Aragas.Core.Interfaces;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;

using PokeD.Server.Data;
using PokeD.Server.Database;


namespace PokeD.Server
{
    public partial class Server : IUpdatable, IDisposable
    {
        const string FileName = "Server.json";

        #region Settings

        [JsonProperty("World")]
        public World World { get; set; } = new World();

        #endregion Settings

        List<IServerModule> Modules { get; } = new List<IServerModule>();
        
        IThread ListenToConnectionsThread { get; set; }


        [JsonIgnore]
        public bool IsDisposing { get; private set; }

        [JsonIgnore]
        public AsymmetricCipherKeyPair RSAKeyPair { get; private set; }
        const int RsaKeySize = 1024;

        IDatabase Database { get; set; }


        public Server()
        {
            Modules.Add(new ModuleP3D(this));
            Modules.Add(new ModuleSCON(this));
            Modules.Add(new ModulePokeD(this));
            Modules.Add(new ModuleNPC(this));
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


            Logger.Log(LogType.Info, "Generating RSA key pair.");
            RSAKeyPair = GenerateKeyPair();


            const string databasePath = "server";
            Logger.Log(LogType.Info, $"Loading {databasePath}.");
            Database = DatabaseWrapper.Create(databasePath);
            Database.CreateTable<Player>();


            Logger.Log(LogType.Info, $"Starting Server.");
            
            ListenToConnectionsThread = ThreadWrapper.CreateThread(ListenToConnectionsCycle);
            ListenToConnectionsThread.Name = "ListenToConnectionsThread";
            ListenToConnectionsThread.IsBackground = true;
            ListenToConnectionsThread.Start();

            foreach (var module in Modules)
                module.Start();

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
