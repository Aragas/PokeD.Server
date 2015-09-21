using System;

using PokeD.Core.Packets.Battle;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Client;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;

namespace PokeD.Server.Clients.Protobuf
{
    partial class ProtobufPlayer
    {
        private void HandleGameData(GameDataPacket packet)
        {
            GameMode = packet.GameMode;
            IsGameJoltPlayer = packet.IsGameJoltPlayer;
            GameJoltId = packet.GameJoltId;
            DecimalSeparator = packet.DecimalSeparator;
            Name = packet.Name;
            LevelFile = packet.LevelFile;
            Position = packet.GetPosition(DecimalSeparator);
            Facing = packet.Facing;
            Moving = packet.Moving;
            Skin = packet.Skin;
            BusyType = packet.BusyType;
            PokemonVisible = packet.PokemonVisible;
            PokemonPosition = packet.GetPokemonPosition(DecimalSeparator);
            PokemonSkin = packet.PokemonSkin;
            PokemonFacing = packet.PokemonFacing;

            if (!Initialized)
            {
                _server.AddPlayer(this);
                Initialized = true;
            }
        }


        private void HandleChatMessage(ChatMessagePacket packet)
        {
            if (packet.Message.StartsWith("/"))
            {
                SendPacket(packet, ID);
                ExecuteCommand(packet.Message);
            }
            else
            {
                Logger.LogChatMessage(Name, packet.Message);
                _server.SendToAllClients(packet, packet.Origin);
            }
        }
        private void HandlePrivateMessage(ChatMessagePrivatePacket packet)
        {
            var destinationPlayerID = _server.GetClientID(packet.DestinationPlayerName);
            if (destinationPlayerID != -1)
            {
                _server.SendToClient(destinationPlayerID, new ChatMessagePrivatePacket { Message = packet.Message }, packet.Origin);
                _server.SendToClient(packet.Origin, new ChatMessagePrivatePacket { Message = packet.Message }, packet.Origin);
            }
            else
                _server.SendToClient(packet.Origin, new ChatMessagePacket { Message = $"The player with the name \"{packet.DestinationPlayerName}\" doesn't exist." }, -1);
        }


        private void HandleGameStateMessage(GameStateMessagePacket packet)
        {
            var playerName = _server.GetClientName(packet.Origin);

            if (!string.IsNullOrEmpty(playerName))
            {
                var message = $"The player {playerName} {packet.EventMessage}";

                Logger.Log(LogType.Server, message);
                _server.SendGlobalChatMessageToAllClients(message);
            }
        }


        private void HandleTradeRequest(TradeRequestPacket packet)
        {
            // XNOR
            if (IsGameJoltPlayer == _server.GetClient(packet.DestinationPlayerID).IsGameJoltPlayer)
                _server.SendToClient(packet.DestinationPlayerID, new TradeRequestPacket(), packet.Origin);
        }
        private void HandleTradeJoin(TradeJoinPacket packet)
        {
            _server.SendToClient(packet.DestinationPlayerID, new TradeJoinPacket(), packet.Origin);
        }
        private void HandleTradeQuit(TradeQuitPacket packet)
        {
            _server.SendToClient(packet.DestinationPlayerID, new TradeQuitPacket(), packet.Origin);
        }
        private void HandleTradeOffer(TradeOfferPacket packet)
        {
            _server.SendToClient(packet.DestinationPlayerID, new TradeOfferPacket { TradeData = packet.TradeData }, packet.Origin);
        }
        private void HandleTradeStart(TradeStartPacket packet)
        {
            _server.SendToClient(packet.DestinationPlayerID, new TradeStartPacket(), packet.Origin);
        }


        private void HandleBattleClientData(BattleClientDataPacket packet)
        {
            BattleOpponentID = packet.DestinationPlayerID;
            BattleLastPacket = DateTime.UtcNow;
            Battling = true;

            _server.SendToClient(packet.DestinationPlayerID, new BattleClientDataPacket { BattleData = packet.BattleData }, packet.Origin);
        }
        private void HandleBattleHostData(BattleHostDataPacket packet)
        {
            BattleOpponentID = packet.DestinationPlayerID;
            BattleLastPacket = DateTime.UtcNow;
            Battling = true;

            _server.SendToClient(packet.DestinationPlayerID, new BattleHostDataPacket { BattleData = packet.BattleData }, packet.Origin);
        }
        private void HandleBattleJoin(BattleJoinPacket packet)
        {
            _server.SendToClient(packet.DestinationPlayerID, new BattleJoinPacket(), packet.Origin);
        }
        private void HandleBattleOffer(BattleOfferPacket packet)
        {
            _server.SendToClient(packet.DestinationPlayerID, new BattleOfferPacket { BattleData = packet.BattleData }, packet.Origin);
        }
        private void HandleBattlePokemonData(BattlePokemonDataPacket packet)
        {
            BattleLastPacket = DateTime.UtcNow;

            _server.SendToClient(packet.DestinationPlayerID, new BattlePokemonDataPacket { BattleData = packet.BattleData }, packet.Origin);
        }
        private void HandleBattleQuit(BattleQuitPacket packet)
        {
            Battling = false;

            _server.SendToClient(packet.DestinationPlayerID, new BattleQuitPacket(), packet.Origin);
        }
        private void HandleBattleRequest(BattleRequestPacket packet)
        {
            _server.SendToClient(packet.DestinationPlayerID, new BattleRequestPacket(), packet.Origin);
        }
        private void HandleBattleStart(BattleStartPacket packet)
        {
            _server.SendToClient(packet.DestinationPlayerID, new BattleStartPacket(), packet.Origin);
        }


        private void HandleServerDataRequest(ServerDataRequestPacket packet)
        {
            var spacket = new ServerInfoDataPacket();
            spacket.CurrentPlayers = _server.PlayersCount;
            spacket.MaxPlayers = _server.MaxPlayers;
            spacket.ServerName = _server.ServerName;
            spacket.ServerMessage = _server.ServerMessage;
            if (_server.PlayersCount > 0)
                spacket.PlayerNames = _server.GetClientNames();

            SendPacket(spacket, ID);

            _server.RemovePlayer(this);
        }
    }
}
