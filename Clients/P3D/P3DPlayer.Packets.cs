using System;
using System.Linq;

using Aragas.Core.Data;

using PokeD.Core.Data;
using PokeD.Core.Packets.Battle;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Client;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;

namespace PokeD.Server.Clients.P3D
{
    public partial class P3DPlayer
    {
        private void ParseGameData(GameDataPacket packet)
        {
            if (packet.DataItems != null)
            {
                var strArray = packet.DataItems.ToArray();
                if (strArray.Length >= 14)
                {
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
                                Position = packet.GetPosition(DecimalSeparator);
                                //if (packet.GetPokemonPosition(DecimalSeparator) != Vector3.Zero)
                                //{
                                //    LastPosition = Position;
                                //
                                //    Position = packet.GetPosition(DecimalSeparator);
                                //
                                //    IsMoving = LastPosition != Position;
                                //}
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
                                //Basic.ServersManager.UpdatePlayerList();
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
                else
                    Logger.Log(LogType.GlobalError, $"P3D Reading Error: ParseGameData DataItems < 14. Packet DataItems {packet.DataItems}.");
            }
            else
                Logger.Log(LogType.GlobalError, $"P3D Reading Error: ParseGameData DataItems is null.");
        }
        private void HandleGameData(GameDataPacket packet)
        {
            ParseGameData(packet);
            _server.UpdateDBPlayer(this);

            if(!Moving)
                _server.SendToAllClients(packet, packet.Origin);

            // if GameJoltID == 0, initialize in login
            if (!IsInitialized)// && GameJoltID != 0)
                Initialize();

            SendPacket(GetDataPacket(), ID);
        }


        private void HandleChatMessage(ChatMessageGlobalPacket packet)
        {
            if (packet.Message.StartsWith("/"))
            {
                SendPacket(new ChatMessageGlobalPacket { Message = packet.Message }, ID);
                ExecuteCommand(packet.Message);
            }
            else
            {
                Logger.LogChatMessage(Name, packet.Message);
                _server.SendToAllClients(new ChatMessageGlobalPacket { Message = packet.Message }, packet.Origin);
            }
        }
        private void HandlePrivateMessage(ChatMessagePrivatePacket packet)
        {
            var destinationPlayerID = _server.GetClientID(packet.DestinationPlayerName);
            if (destinationPlayerID != -1)
            {
                _server.SendToClient(destinationPlayerID, new ChatMessagePrivatePacket { DataItems = new DataItems(packet.Message) }, packet.Origin);
                _server.SendToClient(packet.Origin, new ChatMessagePrivatePacket { DataItems = packet.DataItems }, packet.Origin);
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
            if(IsGameJoltPlayer == _server.GetClient(packet.DestinationPlayerID).IsGameJoltPlayer)
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
            _server.SendToClient(packet.DestinationPlayerID, new TradeOfferPacket { DataItems = new DataItems(packet.TradeData) }, packet.Origin);
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

            _server.SendToClient(packet.DestinationPlayerID, new BattleClientDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }
        private void HandleBattleHostData(BattleHostDataPacket packet)
        {
            BattleOpponentID = packet.DestinationPlayerID;
            BattleLastPacket = DateTime.UtcNow;
            Battling = true;

            _server.SendToClient(packet.DestinationPlayerID, new BattleHostDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }
        private void HandleBattleJoin(BattleJoinPacket packet)
        {
            _server.SendToClient(packet.DestinationPlayerID, new BattleJoinPacket() , packet.Origin);
        }
        private void HandleBattleOffer(BattleOfferPacket packet)
        {
            _server.SendToClient(packet.DestinationPlayerID, new BattleOfferPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }
        private void HandleBattlePokemonData(BattlePokemonDataPacket packet)
        {
            BattleLastPacket = DateTime.UtcNow;

            _server.SendToClient(packet.DestinationPlayerID, new BattlePokemonDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
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
