using System;

using PokeD.Core;
using PokeD.Core.Packets.Battle;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Client;
using PokeD.Core.Packets.Encryption;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;

namespace PokeD.Server.Clients.Protobuf
{
    partial class ProtobufPlayer
    {
        byte[] VerificationToken { get; set; }
        bool Authorized { get; set; }


        private void HandleEncryptionResponse(EncryptionResponsePacket packet)
        {
            if (Authorized)
                return;


            var pkcs = new PKCS1Signer(_server.RSAKeyPair);

            var decryptedToken = pkcs.DeSignData(packet.VerificationToken);
            for (int i = 0; i < VerificationToken.Length; i++)
            {
                if (decryptedToken[i] != VerificationToken[i])
                {
                    SendPacket(new KickedPacket { Reason = "Unable to authenticate." }, -1);
                    return;
                }
            }
            Array.Clear(VerificationToken, 0, VerificationToken.Length);

            var sharedKey = pkcs.DeSignData(packet.SharedSecret);

            Stream.InitializeEncryption(sharedKey);

            Authorized = true;
        }


        private void HandleGameData(GameDataPacket packet)
        {
            if (_server.EncryptionEnabled && !Authorized)
            {
                SendPacket(new KickedPacket { Reason = "You haven't enabled encryption!" }, -1);
                _server.RemovePlayer(this);
                return;
            }

            /*
            try { GameMode = packet.GameMode; }
            catch (Exception) { }

            try { IsGameJoltPlayer = packet.IsGameJoltPlayer; }
            catch (Exception) { }

            try { GameJoltId = packet.GameJoltId; }
            catch (Exception) { }

            try { GameJoltId = packet.GameJoltId; }
            catch (Exception) { }

            try { DecimalSeparator = packet.DecimalSeparator; }
            catch (Exception) { }

            try { Name = packet.Name; }
            catch (Exception) { }

            try { LevelFile = packet.LevelFile; }
            catch (Exception) { }

            try { Position = packet.GetPosition(DecimalSeparator); }
            catch (Exception) { }

            try { Facing = packet.Facing; }
            catch (Exception) { }

            try { Moving = packet.Moving; }
            catch (Exception) { }

            try { Skin = packet.Skin; }
            catch (Exception) { }

            try { BusyType = packet.BusyType; }
            catch (Exception) { }

            try { PokemonVisible = packet.PokemonVisible; }
            catch (Exception) { }

            try { PokemonPosition = packet.GetPokemonPosition(DecimalSeparator); }
            catch (Exception) { }

            try { PokemonSkin = packet.PokemonSkin; }
            catch (Exception) { }

            try { PokemonFacing = packet.PokemonFacing; }
            catch (Exception) { }
            */

            try
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
            }
            catch (Exception) { }

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
                spacket.PlayerNames = _server.GetAllClientsNames();

            SendPacket(spacket, ID);

            _server.RemovePlayer(this);
        }
    }
}
