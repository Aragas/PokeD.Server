using PCLExt.Config;
using PCLExt.Config.Extensions;
using PCLExt.Network;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Server.Clients;
using PokeD.Server.Clients.VBA;

namespace PokeD.Server
{
    public class ModuleVBA : IServerModule
    {
        const string FileName = "ModuleVBA";

        #region Settings

        public bool Enabled { get; private set; } = false;

        public ushort Port { get; private set; } = 5738;

        
        #endregion Settings

        [ConfigIgnore]
        public Server Server { get; }
        bool IsDisposing { get; set; }

        ITCPListener Listener { get; set; }

        [ConfigIgnore]
        public ClientList Clients { get; } = new ClientList();
        [ConfigIgnore]
        public bool ClientsVisible { get; } = false;


        public ModuleVBA(Server server) { Server = server; }


        public bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load VBA settings!");

            if (!Enabled)
            {
                Logger.Log(LogType.Info, $"VBA not enabled!");
                return false;
            }

            Logger.Log(LogType.Info, $"Starting VBA.");

            return true;
        }
        public void Stop()
        {
            var status = FileSystemExtensions.SaveConfig(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save VBA settings!");
            
            Logger.Log(LogType.Info, $"Stopping VBA.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped VBA.");
        }
        
        
        public void StartListen()
        {
            Listener = SocketServer.CreateTCP(Port);
            Listener.Start();
        }
        public void CheckListener()
        {
            if (Listener != null && Listener.AvailableClients)
                if (Listener.AvailableClients)
                    Clients.Add(new VBAServerClient(Listener.AcceptTCPClient(), this));
        }


        public void AddClient(Client client) { }
        public void RemoveClient(Client client, string reason = "")
        {
            Clients.Remove(client);
        }


        public void Update()
        {
            for (var i = 0; i < Clients.Count; i++)
                Clients[i]?.Update();
        }


        public void ClientConnected(Client client) { }
        public void ClientDisconnected(Client client) { }

        public void SendServerMessage(Client sender, string message, bool fromServer = false) { }
        public void SendPrivateMessage(Client sender, Client destClient, string message, bool fromServer = false) { }
        public void SendGlobalMessage(Client sender, string message, bool fromServer = false) { }

        public void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false) { }
        public void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false) { }
        public void SendTradeCancel(Client sender, Client destClient, bool fromServer = false) { }

        public void SendPosition(Client sender, bool fromServer = false) { }

        public void ExecuteCommand(string command) { }



        public void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;


            for (var i = 0; i < Clients.Count; i++)
                Clients[i].Dispose();
            Clients.Clear();
        }
    }
}
