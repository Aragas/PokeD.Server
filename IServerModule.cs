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

        //void AddClient(Client client);
        //void RemoveClient(Client client, string reason = "");

        void OtherConnected(Client client);
        void OtherDisconnected(Client client);

        void SendPrivateMessage(Client sender, Client destClient, string message);
        void SendGlobalMessage(Client sender, string message);
        void SendServerMessage(Client sender, string message);

        //void BattleRequest();
        void SendTradeRequest(Client sender, Monster monster, Client destClient);
        void SendTradeConfirm(Client sender, Client destClient);
        void SendTradeCancel(Client sender, Client destClient);

        void SendPosition(Client sender);
    }
}