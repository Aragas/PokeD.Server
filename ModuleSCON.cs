using Aragas.Core.Data;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data.PokeD.Monster;

using PokeD.Server.Clients;
using PokeD.Server.Clients.SCON;

namespace PokeD.Server
{
    public class ModuleSCON : IServerModule
    {
        const string FileName = "ModuleSCON.json";

        #region Settings

        [JsonProperty("Port")]
        public ushort Port { get; private set; } = 15126;

        [JsonProperty("SCON_Password"), JsonConverter(typeof(PasswordHandler))]
        public PasswordStorage SCON_Password { get; private set; } = new PasswordStorage();

        [JsonProperty("EncryptionEnabled")]
        public bool EncryptionEnabled { get; private set; } = true;
        
        #endregion Settings

        [JsonIgnore]
        public Server Server { get; }

        ITCPListener Listener { get; set; }

        [JsonIgnore]
        public ClientList Clients { get; } = new ClientList();


        public ModuleSCON(Server server) { Server = server; }


        public void Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load SCON settings!");

            Logger.Log(LogType.Info, $"Starting SCON.");

        }
        public void Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save SCON settings!");
            
            Logger.Log(LogType.Info, $"Stopping SCON.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped SCON.");
        }
        
        
        public void StartListen()
        {
            Listener = TCPListenerWrapper.CreateTCPListener(Port);
            Listener.Start();
        }
        public void CheckListener()
        {
            if (Listener != null && Listener.AvailableClients)
                if (Listener.AvailableClients)
                    AddClient(new SCONClient(Listener.AcceptTCPClient(), this));
        }


        public void AddClient(IClient client) { Clients.Add(client); }
        public void RemoveClient(IClient client, string reason = "") { Clients.Remove(client); }


        public void Update()
        {
            for (var i = 0; i < Clients.Count; i++)
                Clients[i]?.Update();
        }


        public void OtherConnected(IClient client) { }
        public void OtherDisconnected(IClient client) { }

        public void SendServerMessage(string message) { }
        public void SendPrivateMessage(IClient sender, IClient destClient, string message) { }
        public void SendGlobalMessage(IClient sender, string message) { }

        public void SendTradeRequest(IClient sender, Monster monster, IClient destClient) { }
        public void SendTradeConfirm(IClient sender, IClient destClient) { }
        public void SendTradeCancel(IClient sender, IClient destClient) { }

        public void ExecuteCommand(string command) { }


        public void Dispose()
        {
            for (int i = 0; i < Clients.Count; i++)
                Clients[i].Dispose();
            Clients.Clear();
        }
    }
}
