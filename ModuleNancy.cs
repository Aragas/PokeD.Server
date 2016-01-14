﻿using System.Collections.Generic;
using System.Linq;

using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data.PokeD.Monster;
    
using PokeD.Server.Clients;

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


        const string FileName = "ModuleNancy.json";

        #region Settings

        [JsonProperty("Host")]
        public string Host { get; private set; } = "localhost";

        [JsonProperty("Port")]
        public ushort Port { get; private set; } = 8765;
        
        #endregion Settings

        [JsonIgnore]
        public Server Server { get; }

        [JsonIgnore]
        public ClientList Clients { get; } = new ClientList();
        [JsonIgnore]
        public bool ClientsVisible { get; } = false;


        public ModuleNancy(Server server) { Server = server; }


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
            var dataApi = new NancyData();
            dataApi.Add("online", GetOnlineClients);

            NancyWrapper.SetDataApi(dataApi);
            NancyWrapper.Start(Host, Port);
        }

        private dynamic GetOnlineClients(dynamic args)
        {
            var response = new OnlineResponseJson(Server.GetAllClientsInfo().Select(playerInfo => new OnlineResponseJson.PlayerJson(playerInfo.Name, playerInfo.Ping, false)));
            var jsonResponse = JsonConvert.SerializeObject(response, Formatting.None);
            return jsonResponse;
        }

        public void CheckListener() { }


        public void AddClient(IClient client) { }
        public void RemoveClient(IClient client, string reason = "") { }


        public void Update() { }


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
            NancyWrapper.Stop();
        }
    }
}
