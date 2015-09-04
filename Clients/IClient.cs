using System;
using Org.BouncyCastle.Utilities.Net;
using PokeD.Core.Data;
using PokeD.Core.Interfaces;

namespace PokeD.Server.Clients
{
    public interface IClient : IUpdatable, IDisposable
    {
        int ID { get; }
        string Name { get; }
        string IP { get; }
        DateTime ConnectionTime { get; }
        bool UseCustomWorld { get; }
        bool IsGameJoltPlayer { get; }
        DataItems GenerateDataItems();
        void SendPacket(IPacket packet, int originID);
    }
}
