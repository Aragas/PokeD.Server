using System;

using Aragas.Core.Data;
using Aragas.Core.Interfaces;
using Aragas.Core.Packets;

using PokeD.Core.Packets.P3D.Shared;

using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients
{
    public interface IClient : IUpdatable, IDisposable
    {
        int ID { get; set; }
        string Name { get; }
        Prefix Prefix { get; }
        bool IsGameJoltPlayer { get; }
        long GameJoltID { get; }
        bool Moving { get; }
        Vector3 Position { get; }
        string LevelFile { get; }

        string IP { get; }
        DateTime ConnectionTime { get; }
        bool UseCustomWorld { get; }
        bool ChatReceiving { get; }


        GameDataPacket GetDataPacket();

        void SendPacket(ProtobufPacket packet, int originID);

        void LoadFromDB(Player data);
    }
}
