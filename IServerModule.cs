using System;

using PokeD.Core;
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


        void ClientConnected(Client client);
        void ClientDisconnected(Client client);

        void SendPrivateMessage(Client sender, Client destClient, string message, bool fromServer = false);
        void SendGlobalMessage(Client sender, string message, bool fromServer = false);
        void SendServerMessage(Client sender, string message, bool fromServer = false);

        void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false);
        void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false);
        void SendTradeCancel(Client sender, Client destClient, bool fromServer = false);

        //void SendBattleRequest(Client sender, Client destClient);
        //void SendBattleAccept(Client sender);
        //void SendBattleAttack(Client sender);
        //void SendBattleItem(Client sender);
        //void SendBattleSwitch(Client sender);
        //void SendBattleFlee(Client sender);

        void SendPosition(Client sender, bool fromServer = false);
    }
}