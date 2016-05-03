using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using PCLExt.Config;
using PCLExt.Nancy;

using PokeD.Core.Data.PokeD.Monster;
    
using PokeD.Server.Clients;
using PokeD.Server.Extensions;

namespace PokeD.Server
{
    public class ModuleNancy : IServerModule
    {
        public class OnlineResponseJson
        {
            public class PlayerJson
            {
                public string Name { get; set; }
                public int Ping { get; set; }
                public bool Online { get; set; }

                public PlayerJson(string name, int ping, bool online) { Name = name; Ping = ping; Online = online; }
            }
            public List<PlayerJson> Players { get; }

            public OnlineResponseJson(IEnumerable<PlayerJson> players) { Players = new List<PlayerJson>(players); }
        }


        const string FileName = "ModuleNancy";

        #region Settings

        public bool Enabled { get; private set; } = false;

        public string Host { get; private set; } = "localhost";

        public ushort Port { get; private set; } = 8765;
        
        #endregion Settings

        [ConfigIgnore]
        public Server Server { get; }

        [ConfigIgnore]
        public ClientList Clients { get; } = new ClientList();
        [ConfigIgnore]
        public bool ClientsVisible { get; } = false;


        public ModuleNancy(Server server) { Server = server; }


        public bool Start()
        {
            var status = FileSystemExtensions.LoadSettings(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load Nancy settings!");

            if (!Enabled)
            {
                Logger.Log(LogType.Info, $"Nancy not enabled!");
                return false;
            }

            Logger.Log(LogType.Info, $"Starting Nancy.");

            return true;
        }
        public void Stop()
        {
            var status = FileSystemExtensions.SaveSettings(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save Nancy settings!");
            
            Logger.Log(LogType.Info, $"Stopping Nancy.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped Nancy.");
        }


        public void StartListen()
        {
            var dataApi = new NancyData();
            dataApi.Add("online", GetOnlineClients);

            Nancy.SetDataApi(dataApi);
            Nancy.Start(Host, Port);
        }

        private dynamic GetOnlineClients(dynamic args)
        {
            var response = new OnlineResponseJson(Server.GetAllClientsInfo().Select(playerInfo => new OnlineResponseJson.PlayerJson(playerInfo.Name, playerInfo.Ping, false)));
            var jsonResponse = JsonConvert.SerializeObject(response, Formatting.None);
            return jsonResponse;
        }

        public void CheckListener() { }


        public void AddClient(Client client) { }
        public void RemoveClient(Client client, string reason = "") { }


        public void Update() { }


        public void OtherConnected(Client client) { }
        public void OtherDisconnected(Client client) { }

        public void SendServerMessage(Client sender, string message) { }
        public void SendPrivateMessage(Client sender, Client destClient, string message) { }
        public void SendGlobalMessage(Client sender, string message) { }

        public void SendTradeRequest(Client sender, Monster monster, Client destClient) { }
        public void SendTradeConfirm(Client sender, Client destClient) { }
        public void SendTradeCancel(Client sender, Client destClient) { }

        public void SendPosition(Client sender) { }

        public void ExecuteCommand(string command) { }


        public void Dispose()
        {
            Nancy.Stop();
        }
    }
}