using PokeD.Core.Interfaces;

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
            public IClient Player { get; }
            public IPacket Packet { get; }
            public int OriginID { get; }

            public PlayerPacket(IClient player, ref IPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
        }
        
        private struct OriginPacket
        {
            public IPacket Packet { get; }
            public int OriginID { get; }

            public OriginPacket(ref IPacket packet, int origin)
            {
                Packet = packet;
                OriginID = origin;
            }
        }
    }
}
