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
            public readonly Packet Packet;
            public readonly int OriginID;

            public PlayerPacket(IClient player, ref Packet packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
        }
        
        private struct OriginPacket
        {
            public readonly Packet Packet;
            public readonly int OriginID;

            public OriginPacket(ref Packet packet, int origin)
            {
                Packet = packet;
                OriginID = origin;
            }
        }
    }
}
