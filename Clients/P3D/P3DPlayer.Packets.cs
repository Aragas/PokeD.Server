using System;
using System.Linq;

using Aragas.Core.Data;

using PokeD.Core.Data.P3D;
using PokeD.Core.Packets.P3D.Battle;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Client;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.P3D.Trade;

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
            _module.Server.UpdateDBPlayer(this);

            if(!Moving)
                _module.P3DPlayerSendToAllClients(packet, packet.Origin);

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
                _module.SendGlobalMessage(this, packet.Message);
            }
        }
        private void HandlePrivateMessage(ChatMessagePrivatePacket packet)
        {
            var destClient = _module.Server.GetClient(packet.DestinationPlayerName);
            if (destClient != null)
            {
                _module.SendPrivateMessage(this, destClient, packet.Message);
                _module.SendPrivateMessage(destClient, this, packet.Message);

                //_module.SendToClient(destinationPlayerID, new ChatMessagePrivatePacket { DataItems = new DataItems(packet.Message) }, packet.Origin);
                //_module.SendToClient(packet.Origin, new ChatMessagePrivatePacket { DataItems = packet.DataItems }, packet.Origin);
            }
            else
                SendPacket(new ChatMessageGlobalPacket { Message = $"The player with the name \"{packet.DestinationPlayerName}\" doesn't exist." }, -1);
        }


        private void HandleGameStateMessage(GameStateMessagePacket packet)
        {
            var playerName = _module.Server.GetClientName(packet.Origin);

            if (!string.IsNullOrEmpty(playerName))
            {
                var message = $"The player {playerName} {packet.EventMessage}";

                Logger.Log(LogType.Server, message);
                _module.SendServerMessage(message);
            }
        }


        private void HandleTradeRequest(TradeRequestPacket packet)
        {
            // XNOR
            if (IsGameJoltPlayer == _module.Server.GetClient(packet.DestinationPlayerID).IsGameJoltPlayer)
            {
                _module.P3DPlayerSendToClient(packet.DestinationPlayerID, new TradeRequestPacket(), packet.Origin);
                //_module.SendTradeRequest(this, packet.DataItems, packet.Origin);
            }
        }
        private void HandleTradeJoin(TradeJoinPacket packet)
        {
            _module.P3DPlayerSendToClient(packet.DestinationPlayerID, new TradeJoinPacket(), packet.Origin);
        }
        private void HandleTradeQuit(TradeQuitPacket packet)
        {
            //_module.P3DPlayerSendToClient(packet.DestinationPlayerID, new TradeQuitPacket(), packet.Origin);
            _module.SendTradeCancel(this, _module.Server.GetClient(packet.DestinationPlayerID));
        }
        private void HandleTradeOffer(TradeOfferPacket packet)
        {
            //_module.SendToClient(packet.DestinationPlayerID, new TradeOfferPacket { DataItems = new DataItems(packet.TradeData) }, packet.Origin);
            _module.SendTradeRequest(this, ModuleP3D.DataItemsToMonster(packet.DataItems), _module.Server.GetClient(packet.DestinationPlayerID));
        }
        private void HandleTradeStart(TradeStartPacket packet)
        {
            //_module.P3DPlayerSendToClient(packet.DestinationPlayerID, new TradeStartPacket(), packet.Origin);
            _module.SendTradeConfirm(this, _module.Server.GetClient(packet.DestinationPlayerID));
        }


        private void HandleBattleClientData(BattleClientDataPacket packet)
        {
            BattleOpponentID = packet.DestinationPlayerID;
            BattleLastPacket = DateTime.UtcNow;
            Battling = true;

            _module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleClientDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }
        private void HandleBattleHostData(BattleHostDataPacket packet)
        {
            BattleOpponentID = packet.DestinationPlayerID;
            BattleLastPacket = DateTime.UtcNow;
            Battling = true;

            _module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleHostDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }
        private void HandleBattleJoin(BattleJoinPacket packet)
        {
            _module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleJoinPacket() , packet.Origin);
        }
        private void HandleBattleOffer(BattleOfferPacket packet)
        {
            _module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleOfferPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }
        private void HandleBattlePokemonData(BattlePokemonDataPacket packet)
        {
            BattleLastPacket = DateTime.UtcNow;

            _module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattlePokemonDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }
        private void HandleBattleQuit(BattleQuitPacket packet)
        {
            Battling = false;

            _module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleQuitPacket(), packet.Origin);
        }
        private void HandleBattleRequest(BattleRequestPacket packet)
        {
            _module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleRequestPacket(), packet.Origin);
        }
        private void HandleBattleStart(BattleStartPacket packet)
        {
            _module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleStartPacket(), packet.Origin);
        }


        private void HandleServerDataRequest(ServerDataRequestPacket packet)
        {
            var spacket = new ServerInfoDataPacket();
            spacket.CurrentPlayers = _module.Clients.Count;
            spacket.MaxPlayers = _module.MaxPlayers;
            spacket.ServerName = _module.ServerName;
            spacket.ServerMessage = _module.ServerMessage;
            if (_module.Server.AllClients().Count > 0)
                spacket.PlayerNames = _module.Server.GetAllClientsInfo().Select(client => client.Name).ToArray();

            SendPacket(spacket, ID);

            _module.RemoveClient(this);
        }
    }
}
