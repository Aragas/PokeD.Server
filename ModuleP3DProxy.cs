﻿using System.Linq;

using PCLExt.Config;
using PCLExt.Config.Extensions;
using PCLExt.Network;

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
        const string FileName = "ModuleP3DProxy";

        #region Settings

        public bool Enabled { get; private set; } = false;

        public string Host { get; private set; } = "karp.pokemon3d.net";

        public ushort Port { get; private set; } = 15124;

        public string PlayerName { get; private set; } = "PokeDProxy";

        #endregion Settings

        [ConfigIgnore]
        public Server Server { get; }
        bool IsDisposing { get; set; }

        [ConfigIgnore]
        public Client Proxy { get; set; }

        [ConfigIgnore]
        public ClientList Clients { get; } = new ClientList();
        [ConfigIgnore]
        public bool ClientsVisible { get; } = true;


        public ModuleP3DProxy(Server server) { Server = server; }


        public bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(Server.ConfigType, FileName, this);
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
            var status = FileSystemExtensions.SaveConfig(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save P3DProxy settings!");

            Logger.Log(LogType.Info, $"Stopping P3DProxy.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped P3DProxy.");
        }


        public void StartListen()
        {
            var client = SocketClient.CreateTCP();
            client.Connect(Host, Port);

            Proxy = new P3DProxyPlayer(client, this, PlayerName);

            var packet = Proxy.GetDataPacket();
            packet.Origin = 0;
            Proxy.SendPacket(packet);
        }
        public void CheckListener() { }


        public P3DProxyDummy GetDummy(int id) => Clients.FirstOrDefault(c => (c as P3DProxyDummy).SID == id) as P3DProxyDummy;

        public void AddOrUpdateClient(int sid, GameDataPacket packet)
        {
            var client = GetDummy(sid);
            if(client != null)
                client.ParseGameData(packet);
            else
            {
                client = new P3DProxyDummy(sid, packet);
                Server.DatabasePlayerGetID(client);
                Clients.Add(client);

                Server.NotifyClientConnected(this, client);
            }
        }
        public void RemoveClient(int sid)
        {
            var client = GetDummy(sid);
            if (client != null)
            {
                Clients.Remove(client);

                Server.NotifyClientDisconnected(this, client);
            }
        }


        public void Update() { Proxy?.Update(); }


        public void ClientConnected(Client client) { }
        public void ClientDisconnected(Client client) { }


        public void SendServerMessage(Client sender, string message, bool fromServer = false)
        {
            if (sender is P3DProxyDummy)
                Server.NotifyServerMessage(this, sender, message);
            else
                Proxy.SendPacket(new ChatServerMessagePacket() { Message = message });
        }

        public void SendPrivateMessage(Client sender, Client destClient, string message, bool fromServer = false) { }
        public void SendGlobalMessage(Client sender, string message, bool fromServer = false)
        {
            if (sender is P3DProxyDummy)
                Server.NotifyServerGlobalMessage(this, sender, message);
            else
                Proxy.SendPacket(new ChatMessageGlobalPacket {Message = $"<{sender.Name}>: {message}"});
        }

        public void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false) { }
        public void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false) { }
        public void SendTradeCancel(Client sender, Client destClient, bool fromServer = false) { }

        public void SendPosition(Client sender, bool fromServer = false) { }



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
