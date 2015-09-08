using System;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.Packets;
using PokeD.Core.Packets.Shared;

namespace PokeD.Server.Clients
{
    public interface IClient : IUpdatable, IDisposable
    {
        int ID { get; set; }
        string Name { get; }
        string IP { get; }
        DateTime ConnectionTime { get; }
        bool UseCustomWorld { get; }
        bool IsGameJoltPlayer { get; }
        long GameJoltId { get; }
        GameDataPacket GetDataPacket();
        void SendPacket(Packet packet, int originID);
    }
}
