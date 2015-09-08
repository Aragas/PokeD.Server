using System;

using PokeD.Core.Packets.Battle;

namespace PokeD.Server.Clients.P3D
{
    public partial class P3DPlayer
    {
        public int BattleTurnTime { get; set; }
        bool Battling { get; set; }
        int BattleOpponentID { get; set; }
        DateTime BattleLastPacket { get; set; }


        private void BattleUpdate()
        {
            if (!Battling)
                return;

            if(DateTime.UtcNow - BattleLastPacket > TimeSpan.FromSeconds(BattleTurnTime))
                _server.SendToClient(BattleOpponentID, new BattleQuitPacket(), ID);
        }
    }
}