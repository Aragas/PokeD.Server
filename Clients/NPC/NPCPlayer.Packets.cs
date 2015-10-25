using PokeD.Core.Packets.Battle;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;

namespace PokeD.Server.Clients.NPC
{
    public partial class NPCPlayer
    {       
        private void HandleChatMessage(ChatMessageGlobalPacket packet)
        {
            if (packet.Message.StartsWith("/"))
            {
            }
            else
            {
            }
        }
        private void HandlePrivateMessage(ChatMessagePrivatePacket packet)
        {
            var destinationPlayerID = _server.GetClientID(packet.DestinationPlayerName);
            if (destinationPlayerID != -1)
            {
            }
            else
            {
            }
        }


        private void HandleGameStateMessage(GameStateMessagePacket packet)
        {
            var playerName = _server.GetClientName(packet.Origin);
            if (!string.IsNullOrEmpty(playerName))
            {
            }
        }


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
