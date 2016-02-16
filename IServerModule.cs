using System;

using Aragas.Core.Interfaces;

using PokeD.Core.Data.PokeD.Monster;

using PokeD.Server.Clients;

namespace PokeD.Server
{
    public interface IServerModule : IUpdatable, IDisposable
    {
        Server Server { get; }
        bool Enabled { get; }
        ushort Port { get; }
        ClientList Clients { get; }
        bool ClientsVisible { get; } 

        bool Start();
        void Stop();

        void StartListen();
        void CheckListener();

        //void AddClient(IClient client);
        //void RemoveClient(IClient client, string reason = "");

        void OtherConnected(IClient client);
        void OtherDisconnected(IClient client);

        void SendPrivateMessage(IClient sender, IClient destClient, string message);
        void SendGlobalMessage(IClient sender, string message);
        void SendServerMessage(IClient sender, string message);

        //void BattleRequest();
        void SendTradeRequest(IClient sender, Monster monster, IClient destClient);
        void SendTradeConfirm(IClient sender, IClient destClient);
        void SendTradeCancel(IClient sender, IClient destClient);

        void SendPosition(IClient sender);
    }
}