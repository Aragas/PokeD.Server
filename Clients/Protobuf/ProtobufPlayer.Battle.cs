/*
using System;

using PokeD.Core.Packets.P3D.Battle;

namespace PokeD.Server.Clients.Protobuf
{
    public partial class ProtobufPlayer
    {
        public int BattleTurnTime { get; set; } = 20;
        bool Battling { get; set; }
        int BattleOpponentID { get; set; }
        DateTime BattleLastPacket { get; set; }


        private void BattleUpdate()
        {
            if (!Battling)
                return;

            //if(DateTime.Now - BattleLastPacket > TimeSpan.FromSeconds(BattleTurnTime))
            //    _server.SendToClient(BattleOpponentID, new BattleQuitPacket(), ID);
        }
    }
}
*/