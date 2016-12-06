using System.Linq;

using Aragas.Network.Data;

using PokeD.Core.Data.P3D;
using PokeD.Core.Extensions;
using PokeD.Core.Packets.P3D.Battle;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Client;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.P3D.Trade;
using PokeD.Server.Chat;
using PokeD.Server.Extensions;

namespace PokeD.Server.Clients.P3D
{
    public partial class P3DPlayer
    {
        private bool PokemonsValid(string pokemonData) => Module.ValidatePokemons && new DataItems(pokemonData).DataItemsToMonsters().All(pokemon => pokemon.IsValid);
        private bool PokemonValid(string pokemonData) => Module.ValidatePokemons && new DataItems(pokemonData).ToMonster().IsValid;

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
                                GameJoltId = packet.GameJoltId;
                                break;

                            case 3:
                                DecimalSeparator = packet.DecimalSeparator;
                                break;

                            case 4:
                                Nickname = packet.Name;
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

        private bool FirstGameData { get; set; } = false;
        private void HandleGameData(GameDataPacket packet)
        {
            ParseGameData(packet);
            Module.SendPosition(this);

            if(IsInitialized)
                Module.ClientUpdate(this);

            if (!Moving)
                Module.SendPacketToAll(packet);

            var dataPacket = GetDataPacket();
            dataPacket.Origin = Id;
            SendPacket(dataPacket);


            // We assume that if we get a GameData, it's a client that wanna play
            if (FirstGameData)
                return;
            FirstGameData = true;

            if (!Module.SetClientId(this))
                return;

            SendPacket(new IdPacket { Origin = -1, PlayerId = Id });
            SendPacket(new WorldDataPacket { Origin = -1, DataItems = Module.World.GenerateDataItems() });

            if (!IsGameJoltPlayer)
            {
                SendServerMessage("Please use /login %PASSWORD% for logging in or registering");
                SendServerMessage("Please note that chat data  isn't sended secure to server");
                SendServerMessage("So it can be seen via traffic sniffing");
                SendServerMessage("Don't use your regular passwords");
                SendServerMessage("On server it's stored fully secure via SHA-512");
            }
            else
                Initialize();
        }


        private void HandleChatMessage(ChatMessageGlobalPacket packet)
        {
            var message = new ChatMessage(this, packet.Message);
            if (packet.Message.StartsWith("/"))
            {
                // Do not show login command
                if (!packet.Message.ToLower().StartsWith("/login"))
                    SendChatMessage(message);

                if (Module.ExecuteClientCommand(this, packet.Message))
                    return;

                if (ExecuteCommand(packet.Message))
                    return;

                SendServerMessage("Invalid command!");
            }
            else if(IsInitialized)
                Module.SendChatMessage(message);
        }
        private void HandlePrivateMessage(ChatMessagePrivatePacket packet)
        {
            var destClient = Module.GetClient(packet.DestinationPlayerName);
            if (destClient != null)
                destClient.SendPrivateMessage(new ChatMessage(this, packet.Message));
            else
                SendServerMessage($"The player with the name \"{packet.DestinationPlayerName}\" doesn't exist.");
        }


        private void HandleGameStateMessage(GameStateMessagePacket packet)
        {
            var playerName = Module.GetClientName(packet.Origin);

            if (!string.IsNullOrEmpty(playerName))
                SendServerMessage($"The player {playerName} {packet.EventMessage}");
        }


        private void HandleTradeRequest(TradeRequestPacket packet)
        {
            var destClient = Module.GetClient(packet.DestinationPlayerId);
            if (destClient is P3DPlayer)
            {
                // XNOR
                if (IsGameJoltPlayer == ((P3DPlayer) destClient).IsGameJoltPlayer)
                    destClient.SendPacket(new TradeRequestPacket { Origin = packet.Origin });
                else
                {
                    SendServerMessage($"Can not start trade with {destClient.Name}! Online-Offline trade disabled.");
                    Module.SendTradeCancel(this, destClient);
                }
            }
            else
                destClient.SendPacket(new TradeRequestPacket { Origin = packet.Origin });
        }

        private void HandleTradeJoin(TradeJoinPacket packet) => Module.GetClient(packet.DestinationPlayerId)?.SendPacket(new TradeJoinPacket {Origin = packet.Origin});
        private void HandleTradeQuit(TradeQuitPacket packet) => Module.SendTradeCancel(this, Module.GetClient(packet.DestinationPlayerId));
        private void HandleTradeOffer(TradeOfferPacket packet)
        {
            var destClient = Module.GetClient(packet.DestinationPlayerId);

            if (PokemonValid(packet.TradeData))
                Module.SendTradeRequest(this, packet.DataItems.ToMonster(), destClient);
            else
            {
                SendServerMessage("Your Pokemon is not valid!");
                Module.SendTradeCancel(this, destClient);
            }
        }
        private void HandleTradeStart(TradeStartPacket packet) => Module.SendTradeConfirm(this, Module.GetClient(packet.DestinationPlayerId));


        private void HandleBattleClientData(BattleClientDataPacket packet)
        {
            Module.GetClient(packet.DestinationPlayerId)?.SendPacket(new BattleClientDataPacket { Origin = packet.Origin, DataItems = new DataItems(packet.BattleData) });
        }
        private void HandleBattleHostData(BattleHostDataPacket packet)
        {
            Module.GetClient(packet.DestinationPlayerId)?.SendPacket(new BattleHostDataPacket { Origin = packet.Origin, DataItems = new DataItems(packet.BattleData) });
        }
        private void HandleBattleJoin(BattleJoinPacket packet)
        {
            Module.GetClient(packet.DestinationPlayerId)?.SendPacket(new BattleJoinPacket { Origin = packet.Origin });
        }
        private void HandleBattleOffer(BattleOfferPacket packet)
        {
            if (PokemonsValid(packet.BattleData))
                Module.GetClient(packet.DestinationPlayerId)?.SendPacket(new BattleOfferPacket { Origin = packet.Origin, DataItems = new DataItems(packet.BattleData) });
            else
            {
                SendServerMessage($"One of your Pokemon is not valid!");
                SendPacket(new BattleQuitPacket { Origin = packet.DestinationPlayerId });
            }
        }
        private void HandleBattlePokemonData(BattlePokemonDataPacket packet)
        {
            Module.GetClient(packet.DestinationPlayerId)?.SendPacket(new BattlePokemonDataPacket { Origin = packet.Origin, DataItems = new DataItems(packet.BattleData) });
        }
        private void HandleBattleQuit(BattleQuitPacket packet)
        {
            Module.GetClient(packet.DestinationPlayerId)?.SendPacket(new BattleQuitPacket { Origin = packet.Origin });
        }
        private void HandleBattleRequest(BattleRequestPacket packet)
        {
            Module.GetClient(packet.DestinationPlayerId)?.SendPacket(new BattleRequestPacket { Origin = packet.Origin });
        }
        private void HandleBattleStart(BattleStartPacket packet)
        {
            Module.GetClient(packet.DestinationPlayerId)?.SendPacket(new BattleStartPacket { Origin = packet.Origin });
        }


        private void HandleServerDataRequest(ServerDataRequestPacket packet)
        {
            var clients = Module.GetAllClients().ToList();
            var spacket = new ServerInfoDataPacket
            {
                Origin = Id,

                CurrentPlayers = Module.GetAllClients().Count(),
                MaxPlayers = Module.MaxPlayers,
                PlayerNames = clients.Any() ? clients.ClientInfos().Select(client => client.Name).ToArray() : new string[0],

                ServerName = Module.ServerName,
                ServerMessage = Module.ServerMessage,
            };
            SendPacket(spacket);

            Kick();
        }
    }
}