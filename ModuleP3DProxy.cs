using System.Linq;

using Aragas.Core.Data;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.PokeD.Chat;

using PokeD.Server.Clients;
using PokeD.Server.Clients.P3DProxy;

namespace PokeD.Server
{
    public class ModuleP3DProxy : IServerModule
    {
        const string FileName = "ModuleP3DProxy.json";

        #region Settings

        [JsonProperty("Enabled")]
        public bool Enabled { get; private set; } = false;

        [JsonProperty("Host", NullValueHandling = NullValueHandling.Ignore)]
        public string Host { get; private set; } = "karp.pokemon3d.net";

        [JsonProperty("Port")]
        public ushort Port { get; private set; } = 15124;

        [JsonProperty("PlayerName", NullValueHandling = NullValueHandling.Ignore)]
        public string PlayerName { get; private set; } = "PokeDProxy";

        #endregion Settings

        [JsonIgnore]
        public Server Server { get; }
        bool IsDisposing { get; set; }
        
        [JsonIgnore]
        public IClient Proxy { get; set; }

        [JsonIgnore]
        public ClientList Clients { get; } = new ClientList();
        [JsonIgnore]
        public bool ClientsVisible { get; } = true;


        public ModuleP3DProxy(Server server) { Server = server; }


        public bool Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load P3DProxy settings!");

            if (!Enabled)
            {
                Logger.Log(LogType.Info, $"P3DProxy not enabled!");
                return false;
            }

            Logger.Log(LogType.Info, $"Starting P3DProxy.");
            

            return true;
        }
        public void Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save P3DProxy settings!");

            Logger.Log(LogType.Info, $"Stopping P3DProxy.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped P3DProxy.");
        }


        public void StartListen()
        {
            var client = TCPClientWrapper.CreateTCPClient();
            client.Connect(Host, Port);

            Proxy = new P3DProxyPlayer(client, this, PlayerName);
            Proxy.SendPacket(Proxy.GetDataPacket(), 0);
        }
        public void CheckListener() { }


        public P3DProxyDummy GetDummy(VarInt id) => Clients.FirstOrDefault(c => (c as P3DProxyDummy).SID == id) as P3DProxyDummy;

        public void AddOrUpdateClient(VarInt sid, GameDataPacket packet)
        {
            var client = GetDummy(sid);
            if(client != null)
                client.ParseGameData(packet);
            else
            {
                client = new P3DProxyDummy(sid, packet);
                Server.PeekDBID(client);
                Clients.Add(client);

                Server.ClientConnected(this, client);
            }
        }
        public void RemoveClient(VarInt sid)
        {
            var client = GetDummy(sid);
            if (client != null)
            {
                Clients.Remove(client);

                Server.ClientDisconnected(this, client);
            }
        }


        public void Update() { Proxy?.Update(); }


        public void OtherConnected(IClient client) { }
        public void OtherDisconnected(IClient client) { }


        public void SendServerMessage(string message) { SendServerMessage(message, false); }
        public void SendServerMessage(string message, bool fromDummy)
        {
            if (fromDummy)
                Server.ClientServerMessage(this, message);
            else
                Proxy.SendPacket(new ChatServerMessagePacket() {Message = message});
        }

        public void SendPrivateMessage(IClient sender, IClient destClient, string message) { }
        public void SendGlobalMessage(IClient sender, string message)
        {
            if (sender is P3DProxyDummy)
                Server.ClientGlobalMessage(this, sender, message);
            else
                Proxy.SendPacket(new ChatMessageGlobalPacket {Message = $"<{sender.Name}>: {message}"});
        }

        public void SendTradeRequest(IClient sender, Monster monster, IClient destClient) { }
        public void SendTradeConfirm(IClient sender, IClient destClient) { }
        public void SendTradeCancel(IClient sender, IClient destClient) { }

        public void SendPosition(IClient sender) { }



        public void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;

            Proxy.Dispose();

            for (var i = 0; i < Clients.Count; i++)
                Clients[i].Dispose();
            Clients.Clear();
        }
    }
}
