using PokeD.Core.Packets.PokeD.Battle;
using PokeD.Core.Packets.PokeD.Overworld;

namespace PokeD.Server.Clients.PokeD
{
    partial class PokeDPlayer
    {
        byte[] VerificationToken { get; set; }
        bool Authorized { get; set; }

        private void HandlePosition(PositionPacket packet)
        {
        }
        private void HandleTrainerInfo(TrainerInfoPacket packet)
        {
        }

        private void HandleBattleRequest(BattleRequestPacket packet)
        {
            if(Battle == null)
                Battle = Module.CreateBattle(packet.PlayerIDs, packet.Message);
            else
                SendPacket(new BattleCancelledPacket {Reason = "You are already in battle!"});
        }
        private void HandleBattleAccept(BattleAcceptPacket packet) { if(packet.IsAccepted) Battle.AcceptBattle(this); else Battle.CancelBattle(this); }

        private void HandleBattleAttack(BattleAttackPacket packet) { Battle.HandlePacket(this, packet); }
        private void HandleBattleItem(BattleItemPacket packet) { Battle.HandlePacket(this, packet); }
        private void HandleBattleSwitch(BattleSwitchPacket packet) { Battle.HandlePacket(this, packet); }
        private void HandleBattleFlee(BattleFleePacket packet) { Battle.HandlePacket(this, packet); }
    }
}
