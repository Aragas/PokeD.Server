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
        bool IsGameJoltPlayer { get; }
        long GameJoltId { get; }
        Vector3 Position { get; }
        string LevelFile { get; }

        string IP { get; }
        DateTime ConnectionTime { get; }
        bool UseCustomWorld { get; }
        bool ChatReceiving { get; }


        GameDataPacket GetDataPacket();

        void SendPacket(ProtobufPacket packet, int originID);
        void SendPacket(P3DPacket packet, int originID);

        void Disconnect();
    }
}
