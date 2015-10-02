using System;
using System.Linq;

using Newtonsoft.Json;

using PokeD.Core;
using PokeD.Core.Data;
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


        [JsonIgnore]
        public bool IsMoving { get; private set; }
        Vector3 LastPosition { get; set; }

        private void HandleJoiningGameRequest(JoiningGameRequestPacket packet)
        {
            SendPacket(new JoiningGameResponsePacket { EncryptionEnabled = EncryptionEnabled }, -1);

            if (EncryptionEnabled)
                SendEncryptionRequest();
        }

        private void HandleEncryptionResponse(EncryptionResponsePacket packet)
        {
            if (Authorized)
                return;


            var pkcs = new PKCS1Signer(_server.RSAKeyPair);

            var decryptedToken = pkcs.DeSignData(packet.VerificationToken);
            for (int i = 0; i < VerificationToken.Length; i++)
                if (decryptedToken[i] != VerificationToken[i])
                {
                    SendPacket(new KickedPacket { Reason = "Unable to authenticate." }, -1);
                    return;
                }
            
            Array.Clear(VerificationToken, 0, VerificationToken.Length);

            var sharedKey = pkcs.DeSignData(packet.SharedSecret);

            Stream.InitializeEncryption(sharedKey);

            Authorized = true;
        }

        private void ParseGameData(GameDataPacket packet)
        {
            if (packet.DataItems == null)
            {
                Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: ParseGameData DataItems is null.");
                return;
            }

            var strArray = packet.DataItems.ToArray();
            if (strArray.Length < 14)
            {
                Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: ParseGameData DataItems < 14. Packet DataItems {packet.DataItems}.");
                return;
            }

            for (var index = 0; index < strArray.Length; index++)
            {
                var dataItem = strArray[index];

                if (string.IsNullOrEmpty(dataItem))
                    continue;

                switch (index)
                {
                    case 0:
                        GameMode = packet.GameMode;
                        break;

                    case 1:
                        IsGameJoltPlayer = packet.IsGameJoltPlayer;
                        break;

                    case 2:
                        GameJoltID = packet.GameJoltID;
                        break;

                    case 3:
                        DecimalSeparator = packet.DecimalSeparator;
                        break;

                    case 4:
                        Name = packet.Name;
                        break;

                    case 5:
                        LevelFile = packet.LevelFile;
                        break;

                    case 6:
                        if (packet.GetPokemonPosition(DecimalSeparator) != Vector3.Zero)
                        {
                            LastPosition = Position;

                            Position = packet.GetPosition(DecimalSeparator);

                            IsMoving = LastPosition != Position;
                        }
                        break;

                    case 7:
                        Facing = packet.Facing;
                        break;

                    case 8:
                        Moving = packet.Moving;
                        break;

                    case 9:
                        Skin = packet.Skin;
                        break;

                    case 10:
                        BusyType = packet.BusyType;
                        break;

                    case 11:
                        PokemonVisible = packet.PokemonVisible;
                        break;

                    case 12:
                        if (packet.GetPokemonPosition(DecimalSeparator) != Vector3.Zero)
                            PokemonPosition = packet.GetPokemonPosition(DecimalSeparator);
                        break;

                    case 13:
                        PokemonSkin = packet.PokemonSkin;
                        break;

                    case 14:
                        PokemonFacing = packet.PokemonFacing;
                        break;
                }
            }
        }
        private void HandleGameData(GameDataPacket packet)
        {
            /*
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
            */

            ParseGameData(packet);

            if (!Initialized)
            {
                _server.AddPlayer(this);
                Initialized = true;
            }
        }


        private void HandleChatMessage(ChatMessageGlobalPacket packet)
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
                _server.SendToClient(packet.Origin, new ChatMessageGlobalPacket { Message = $"The player with the name \"{packet.DestinationPlayerName}\" doesn't exist." }, -1);
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
                spacket.PlayerNames = _server.GetAllClientsInfo().Select(client => client.Name).ToArray();

            SendPacket(spacket, ID);

            _server.RemovePlayer(this);
        }
    }
}
