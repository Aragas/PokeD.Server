using System.Linq;

using Aragas.Core.Data;

using PokeD.Core.Data.P3D;
using PokeD.Core.Packets.P3D.Battle;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Client;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.P3D.Trade;

using PokeD.Server.Extensions;

namespace PokeD.Server.Clients.P3D
{
    public partial class P3DPlayer
    {
        private static bool PokemonsValid(string pokemonData) => new DataItems(pokemonData).DataItemsToMonsters().All(pokemon => pokemon.IsValid());
        private static bool PokemonValid(string pokemonData) => new DataItems(pokemonData).ToMonster().IsValid();

        bool FirstGameData { get; set; } = false;
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
                    Logger.Log(LogType.Error, $"P3D Reading Error: ParseGameData DataItems < 14. Packet DataItems {packet.DataItems}.");
            }
            else
                Logger.Log(LogType.Error, $"P3D Reading Error: ParseGameData DataItems is null.");
        }
        private void HandleGameData(GameDataPacket packet)
        {
            ParseGameData(packet);
            Module.SendPosition(this);

            if(IsInitialized)
                Module.Server.DatabasePlayerSave(this);

            if(!Moving)
                Module.P3DPlayerSendToAllClients(packet, packet.Origin);

            var dataPacket = GetDataPacket();
            dataPacket.Origin = ID;
            SendPacket(dataPacket);

            // We assume that if we get a GameData, it's a client that wanna play
            if (FirstGameData)
                return;
            FirstGameData = true;

            if (!IsGameJoltPlayer)
            {
                Module.PreAdd(this);

                SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = "Please use /login %PASSWORD% for logging in or registering" });
                SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = "Please note that chat data  isn't sended secure to server" });
                SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = "So it can be seen via traffic sniffing" });
                SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = "Don't use your regular passwords" });
                SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = "On server it's stored fully secure via SHA-512" });
            }
            else
            {
                PasswordHash = GameJoltID.ToString();
                Module.PreAdd(this);
                Initialize();
            }
        }


        private void HandleChatMessage(ChatMessageGlobalPacket packet)
        {
            if (packet.Message.StartsWith("/"))
            {
                // Do not show login command
                if(!packet.Message.ToLower().StartsWith("/login"))
                    SendPacket(new ChatMessageGlobalPacket { Origin = ID, Message = packet.Message });

                if (Module.Server.ProcessCommand(this, packet.Message)) return;
                if (ExecuteCommand(packet.Message)) return;
                SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = "Invalid command!" });
            }
            else if(IsInitialized)
                Module.SendGlobalMessage(this, packet.Message);
        }
        private void HandlePrivateMessage(ChatMessagePrivatePacket packet)
        {
            var destClient = Module.Server.GetClient(packet.DestinationPlayerName);
            if (destClient != null)
            {
                Module.P3DPlayerSendToClient(this, new ChatMessagePrivatePacket { DataItems = packet.DataItems }, packet.Origin);
                Module.SendPrivateMessage(this, destClient, packet.Message);
            }
            else
                SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = $"The player with the name \"{packet.DestinationPlayerName}\" doesn't exist." });
        }


        private void HandleGameStateMessage(GameStateMessagePacket packet)
        {
            var playerName = Module.Server.GetClientName(packet.Origin);

            if (!string.IsNullOrEmpty(playerName))
            {
                var message = $"The player {playerName} {packet.EventMessage}";

                Module.SendServerMessage(this, message);
            }
        }


        private void HandleTradeRequest(TradeRequestPacket packet)
        {
            var destClient = Module.Server.GetClient(packet.DestinationPlayerID);
            if (destClient is P3DPlayer)
            {
                // XNOR
                if (IsGameJoltPlayer == ((P3DPlayer) destClient).IsGameJoltPlayer)
                    Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new TradeRequestPacket(), packet.Origin);
                else
                { 
                    SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = $"Can not start trade with {destClient.Name}! Online-Offline trade disabled." });
                    Module.SendTradeCancel(this, destClient);
                }
            }
            else
                Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new TradeRequestPacket(), packet.Origin);
        }
        private void HandleTradeJoin(TradeJoinPacket packet) { Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new TradeJoinPacket(), packet.Origin); }
        private void HandleTradeQuit(TradeQuitPacket packet) { Module.SendTradeCancel(this, Module.Server.GetClient(packet.DestinationPlayerID)); }
        private void HandleTradeOffer(TradeOfferPacket packet)
        {
            var destClient = Module.Server.GetClient(packet.DestinationPlayerID);

            if (PokemonValid(packet.TradeData))
                Module.SendTradeRequest(this, packet.DataItems.ToMonster(), destClient);
            else
            {
                SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = $"Your Pokemon is not valid!" });
                Module.SendTradeCancel(this, destClient);
            }
        }
        private void HandleTradeStart(TradeStartPacket packet) { Module.SendTradeConfirm(this, Module.Server.GetClient(packet.DestinationPlayerID)); }


        private void HandleBattleClientData(BattleClientDataPacket packet)
        {
            Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleClientDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }
        private void HandleBattleHostData(BattleHostDataPacket packet)
        {
            Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleHostDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }
        private void HandleBattleJoin(BattleJoinPacket packet)
        {
            Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleJoinPacket() , packet.Origin);
        }
        private void HandleBattleOffer(BattleOfferPacket packet)
        {
            if (PokemonsValid(packet.BattleData))
                Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleOfferPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
            else
            {
                SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = $"One of your Pokemon is not valid!" });
                SendPacket(new BattleQuitPacket { Origin = packet.DestinationPlayerID });
            }
        }
        private void HandleBattlePokemonData(BattlePokemonDataPacket packet)
        {
            Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattlePokemonDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }
        private void HandleBattleQuit(BattleQuitPacket packet)
        {
            Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleQuitPacket(), packet.Origin);
        }
        private void HandleBattleRequest(BattleRequestPacket packet)
        {
            Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleRequestPacket(), packet.Origin);
        }
        private void HandleBattleStart(BattleStartPacket packet)
        {
            Module.P3DPlayerSendToClient(packet.DestinationPlayerID, new BattleStartPacket(), packet.Origin);
        }


        private void HandleServerDataRequest(ServerDataRequestPacket packet)
        {
            var spacket = new ServerInfoDataPacket();
            spacket.CurrentPlayers = Module.Server.AllClients().Count();
            spacket.MaxPlayers = Module.MaxPlayers;
            spacket.ServerName = Module.ServerName;
            spacket.ServerMessage = Module.ServerMessage;
            if (Module.Server.AllClients().Any())
                spacket.PlayerNames = Module.Server.GetAllClientsInfo().Select(client => client.Name).ToArray();

            spacket.Origin = ID;
            SendPacket(spacket);

            Module.RemoveClient(this);
        }
    }
}
