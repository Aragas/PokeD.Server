using System;
using System.Globalization;
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
        string PasswordHash { get; set; }
        Vector3 Position { get; }
        string LevelFile { get; }

        string IP { get; }
        DateTime ConnectionTime { get; }
        CultureInfo Language { get; }


        GameDataPacket GetDataPacket();

        void SendPacket(ProtobufPacket packet, int originID = 0);

        void LoadFromDB(Player data);
    }
}
