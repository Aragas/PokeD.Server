using PokeD.Core.Data;
using PokeD.Core.Packets.Battle;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Client;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;

namespace PokeD.Server.Data
{
    public partial class Player
    {
        private void HandleGameData(GameDataPacket packet)
        {
            /*
            GameMode = packet.GameMode;
            IsGameJoltPlayer = packet.IsGameJoltPlayer;
            GameJoltId = packet.GameJoltId;
            DecimalSeparator = packet.DecimalSeparator;
            Name = packet.Name;
            LevelFile = packet.LevelFile;
            Position = packet.Position;
            Facing = packet.Facing;
            Moving = packet.Moving;
            Skin = packet.Skin;
            BusyType = packet.BusyType;
            //Basic.ServersManager.UpdatePlayerList();
            PokemonVisible = packet.PokemonVisible;
            PokemonPosition = packet.PokemonPosition;
            PokemonSkin = packet.PokemonSkin;
            PokemonFacing = packet.PokemonFacing;
            */

            ParseGameData(packet);

            if (!Initialized)
            {
                ID = _server.GenerateID();
                SendPacketCustom(new IDPacket { DataItems = new DataItems(ID.ToString()) });
                SendPacketCustom(new WorldDataPacket { DataItems = new DataItems(_server.World.GetWorld().ToArray()) });

                _server.AddPlayer(this);
                Initialized = true;
            }
        }

        private void ParseGameData(GameDataPacket packet)
        {
            string[] strArray = packet.DataItems.ToList().ToArray();
            int index = 0;
            do
            {
                string str = strArray[index];
                if (str != "")
                {
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
                            Name = packet.Name;
                            break;

                        case 5:
                            LevelFile = packet.LevelFile;
                            break;

                        case 6:
                            Position = packet.Position;
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
                            PokemonPosition = packet.PokemonPosition;
                            break;

                        case 13:
                            PokemonSkin = packet.PokemonSkin;
                            break;

                        case 14:
                            PokemonFacing = packet.PokemonFacing;
                            break;
                    }
                }
                index++;
            }
            while (index <= 14);
        }

        private void HandleChatMessage(ChatMessagePacket packet)
        {
            if (!_server.PlayerIsMuted(this))
            {
                if (packet.Message.StartsWith("/"))
                    _server.ExecuteClientCommand(packet.Message);
                else
                    _server.SendToAllPlayers(packet, packet.Origin);
            }
            else
                SendPacketCustom(new ChatMessagePacket { DataItems = new DataItems("You are muted on this server!") });
        }

        private void HandlePrivateMessage(ChatMessagePrivatePacket packet)
        {
            if (!_server.PlayerIsMuted(this))
            {
                var destinationPlayerID = _server.PlayerID(packet.DestinationPlayerName);
                if (destinationPlayerID != -1)
                {
                    _server.SendToPlayer(destinationPlayerID, new ChatMessagePrivatePacket { DataItems = new DataItems(packet.Message) }, packet.Origin);
                    _server.SendToPlayer(packet.Origin, new ChatMessagePrivatePacket { DataItems = packet.DataItems }, packet.Origin);
                }
                else
                    _server.SendToPlayer(packet.Origin, new ChatMessagePacket { DataItems = new DataItems(string.Format("The player with the name \"{0}\" doesn't exist.", packet.DestinationPlayerName)) });
            }
            else
                _server.SendToPlayer(packet.Origin, new ChatMessagePacket { DataItems = new DataItems("You are muted on this server!") });
        }

        private void HandleGameStateMessage(GameStateMessagePacket packet)
        {
            var playerName = _server.GetPlayerName(packet.Origin);

            if (!string.IsNullOrEmpty(playerName) && _server.PlayerIsMuted(packet.Origin))
                _server.SendToAllPlayers(new ChatMessagePacket { DataItems = new DataItems(string.Format("The player {0} {1}", playerName, packet.DataItems[0])) });
        }


        private void HandleTradeRequest(TradeRequestPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new TradeRequestPacket(), packet.Origin);
        }

        private void HandleTradeJoin(TradeJoinPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new TradeJoinPacket(), packet.Origin);
        }

        private void HandleTradeQuit(TradeQuitPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new TradeQuitPacket(), packet.Origin);
        }

        private void HandleTradeOffer(TradeOfferPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new TradeOfferPacket { DataItems = new DataItems(packet.TradeData)}, packet.Origin);
        }

        private void HandleTradeStart(TradeStartPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new TradeStartPacket(), packet.Origin);
        }


        private void HandleBattleClientData(BattleClientDataPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleClientDataPacket { DataItems = new DataItems(packet.BattleData)}, packet.Origin);
        }

        private void HandleBattleHostData(BattleHostDataPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleHostDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }

        private void HandleBattleJoin(BattleJoinPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleJoinPacket() , packet.Origin);
        }

        private void HandleBattleOffer(BattleOfferPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleOfferPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }

        private void HandleBattlePokemonData(BattlePokemonDataPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattlePokemonDataPacket { DataItems = new DataItems(packet.BattleData) }, packet.Origin);
        }

        private void HandleBattleQuit(BattleQuitPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleQuitPacket(), packet.Origin);
        }

        private void HandleBattleRequest(BattleRequestPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleRequestPacket(), packet.Origin);
        }

        private void HandleBattleStart(BattleStartPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleStartPacket(), packet.Origin);
        }


        private void HandleServerDataRequest(ServerDataRequestPacket packet)
        {
            SendPacket(new ServerInfoDataPacket
            {
                CurrentPlayers = _server.Players.Count,
                MaxPlayers = _server.MaxPlayers,
                ServerName = _server.ServerName,
                ServerMessage = _server.ServerMessage
            });

            _server.RemovePlayer(this);
        }
    }
}
