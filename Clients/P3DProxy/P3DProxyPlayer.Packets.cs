using PokeD.Core.Packets.P3D.Battle;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.P3D.Trade;

namespace PokeD.Server.Clients.P3DProxy
{
    public partial class P3DProxyPlayer
    {
        private void HandleID(IDPacket packet)
        {
            ID = packet.PlayerID;
        }
        private void HandleGameData(GameDataPacket packet)
        {
            if(packet.Origin == ID)
                return;

            Module.AddOrUpdateClient(packet.Origin, packet);
        }

        private void HandleCreatePlayer(CreatePlayerPacket packet) { }
        private void HandleDestroyPlayer(DestroyPlayerPacket packet)
        {
            Module.RemoveClient(packet.PlayerID);
        }


        private void HandleServerMessage(ServerMessagePacket packet)
        {
            Module.SendServerMessage(packet.Message);
        }
        private void HandleChatMessage(ChatMessageGlobalPacket packet)
        {
            var client = Module.GetDummy(packet.Origin);
            if(client != null)
                Module.SendGlobalMessage(client, packet.Message);
        }
        private void HandlePrivateMessage(ChatMessagePrivatePacket packet) { }


        private void HandleTradeRequest(TradeRequestPacket packet)
        {
        }
        private void HandleTradeJoin(TradeJoinPacket packet)
        {
        }
        private void HandleTradeQuit(TradeQuitPacket packet)
        {
        }
        private void HandleTradeOffer(TradeOfferPacket packet)
        {
        }
        private void HandleTradeStart(TradeStartPacket packet)
        {
        }


        private void HandleBattleClientData(BattleClientDataPacket packet)
        {
        }
        private void HandleBattleHostData(BattleHostDataPacket packet)
        {
        }
        private void HandleBattleJoin(BattleJoinPacket packet)
        {
        }
        private void HandleBattleOffer(BattleOfferPacket packet)
        {
        }
        private void HandleBattlePokemonData(BattlePokemonDataPacket packet)
        {
        }
        private void HandleBattleQuit(BattleQuitPacket packet)
        {
        }
        private void HandleBattleRequest(BattleRequestPacket packet)
        {
        }
        private void HandleBattleStart(BattleStartPacket packet)
        {
        }
    }
}
