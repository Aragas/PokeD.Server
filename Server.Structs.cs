using PokeD.Core.Packets;

using PokeD.Server.Clients;

namespace PokeD.Server
{
    public enum MuteStatus
    {
        Completed,
        PlayerNotFound,
        MutedYourself,
        IsNotMuted
    }

    public partial class Server
    {
        private struct PlayerPacket
        {
            public readonly IClient Player;
            public readonly P3DPacket Packet;
            public readonly int OriginID;

            public PlayerPacket(IClient player, ref P3DPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
        }
        
        private struct OriginPacket
        {
            public readonly P3DPacket Packet;
            public readonly int OriginID;

            public OriginPacket(ref P3DPacket packet, int origin)
            {
                Packet = packet;
                OriginID = origin;
            }
        }
    }
}
