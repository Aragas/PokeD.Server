using System;

using Aragas.Core.Data;
using Aragas.Core.Interfaces;

using PokeD.Core.Packets;
using PokeD.Core.Packets.Shared;
using PokeD.Server.Data;

namespace PokeD.Server.Clients
{
    public interface IClient : IUpdatable, IDisposable
    {
        int ID { get; set; }
        string Name { get; }
        Prefix Prefix { get; }
        bool IsGameJoltPlayer { get; }
        ulong GameJoltID { get; }
        Vector3 Position { get; }
        string LevelFile { get; }

        string IP { get; }
        DateTime ConnectionTime { get; }
        bool UseCustomWorld { get; }
        bool ChatReceiving { get; }
        bool IsMoving { get; }


        GameDataPacket GetDataPacket();

        void SendPacket(P3DPacket packet, int originID);
    }
}
