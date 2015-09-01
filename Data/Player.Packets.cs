using System;
using System.Collections.Generic;

using Newtonsoft.Json;

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
        [JsonIgnore] public bool IsMovingNew { get { return (Position.X % 1) != 0 || (Position.Z % 1) != 0; } }

        [JsonIgnore] public bool IsMoving { get { return Positions.Count > 0; } }
        [JsonIgnore] Queue<Vector3> Positions = new Queue<Vector3>();

        int MovingUpdateRate { get; set; }
        private void DoMoving(Vector3 lastPos, Vector3 newPos)
        {
            var steps = MovingUpdateRate;
            if (Initialized && !IsMoving)
            {
                //*
                var step = (float) 1 / (float) steps;
                var direction = newPos - lastPos;
                if (direction.Distance() > 1)
                    return;

                if (direction.X != 0)
                    for (int i = 0; i < steps; i++)
                        Positions.Enqueue(direction.X > 0 ? new Vector3(step, 0f, 0f) : new Vector3(-step, 0f, 0f));

                else if (direction.Y != 0)
                    for (int i = 0; i < steps; i++)
                        Positions.Enqueue(direction.X > 0 ? new Vector3(0f, step, 0f) : new Vector3(0f, -step, 0f));

                else if (direction.Z != 0)
                    for (int i = 0; i < steps; i++)
                        Positions.Enqueue(direction.X > 0 ? new Vector3(0f, 0f, step) : new Vector3(0f, 0f, -step));
                //*/

                /*
                var step = (float)1 / (float)steps;
                var direction = newPos - lastPos;
                if (direction.Distance() > 1)
                    return;

                if (direction.X != 0)
                    for (int i = 0; i < steps; i++)
                        Positions.Enqueue(direction.X > 0 ? new Vector3((step + step * (i+1)), 0f, 0f) : new Vector3(-(step + step * (i + 1)), 0f, 0f));

                else if (direction.Y != 0)
                    for (int i = 0; i < steps; i++)
                        Positions.Enqueue(direction.X > 0 ? new Vector3(0f, (step + step * (i + 1)), 0f) : new Vector3(0f, -(step + step * (i + 1)), 0f));

                else if (direction.Z != 0)
                    for (int i = 0; i < steps; i++)
                        Positions.Enqueue(direction.X > 0 ? new Vector3(0f, 0f, (step + step * (i + 1))) : new Vector3(0f, 0f, -(step + step * (i + 1))));
                */
            }
        }


        private static bool IsFullPackageData(DataItems dataItems)
        {
            if (string.Equals(dataItems[0], "1", StringComparison.OrdinalIgnoreCase))
                foreach (string str in dataItems.ToList())
                {
                    if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
                        return false;
                    else
                    {
                        var num = 0;
                        do
                        {
                            if ((string.IsNullOrEmpty(dataItems[num]) || string.IsNullOrWhiteSpace(dataItems[num])) && (num != 2))
                                return false;

                            num++;
                        } while (num <= 14);
                    }
                }

            return true;
        }

        private void ParseGameData(GameDataPacket packet)
        {
            string[] strArray = packet.DataItems.ToArray();
            if(strArray.Length < 14)
                return;
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
                            //if(packet.LevelFile != "mainmenu\\mainmenu1.dat")
                            //    LevelFile = packet.LevelFile;
                            LevelFile = packet.LevelFile;
                            break;

                        case 6:
                            if (packet.GetPokemonPosition(DecimalSeparator) != Vector3.Zero)
                            {
                                if (_server.MoveCorrectionEnabled && Position != packet.GetPosition(DecimalSeparator))
                                    DoMoving(Position, packet.GetPosition(DecimalSeparator));
                                
                                Position = packet.GetPosition(DecimalSeparator);
                            }

                            //if (Position != packet.Position)
                            //    DoMoving(Position, packet.Position);
                            
                            //if (packet.Position != new Vector3(float.MaxValue))
                            //{
                            //    if (Position != packet.Position)
                            //    {
                            //        BeforeMove = Position;
                            //        DoMoving(Position, packet.Position);
                            //    }
                            //
                            //    if(BeforeMove != packet.Position)
                            //        Position = packet.Position;
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
                index++;
            }
            while (index <= 14);
        }
        

        private void HandleGameData(GameDataPacket packet)
        {
            if (IsFullPackageData(packet.DataItems))
                ParseGameData(packet);


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
                _server.SendToAllPlayers(packet, packet.Origin);
        }

        private void HandlePrivateMessage(ChatMessagePrivatePacket packet)
        {
            var destinationPlayerID = _server.GetPlayerID(packet.DestinationPlayerName);
            if (destinationPlayerID != -1)
            {
                _server.SendToPlayer(destinationPlayerID, new ChatMessagePrivatePacket { DataItems = packet.DataItems }, packet.Origin);
                _server.SendToPlayer(packet.Origin, new ChatMessagePrivatePacket { DataItems = packet.DataItems }, packet.Origin);
            }
            else
                _server.SendToPlayer(packet.Origin, new ChatMessagePacket { Message = string.Format("The player with the name \"{0}\" doesn't exist.", packet.DestinationPlayerName) }, -1);
        }

        private void HandleGameStateMessage(GameStateMessagePacket packet)
        {
            var playerName = _server.GetPlayerName(packet.Origin);

            if (!string.IsNullOrEmpty(playerName))
                _server.SendToAllPlayers(new ChatMessagePacket { Message = string.Format("The player {0} {1}", playerName, packet.DataItems[0]) });
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
            _server.SendToPlayer(packet.DestinationPlayerID, new TradeOfferPacket { DataItems = packet.DataItems }, packet.Origin);
        }

        private void HandleTradeStart(TradeStartPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new TradeStartPacket(), packet.Origin);
        }


        private void HandleBattleClientData(BattleClientDataPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleClientDataPacket { DataItems = packet.DataItems }, packet.Origin);
        }

        private void HandleBattleHostData(BattleHostDataPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleHostDataPacket { DataItems = packet.DataItems }, packet.Origin);
        }

        private void HandleBattleJoin(BattleJoinPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleJoinPacket() , packet.Origin);
        }

        private void HandleBattleOffer(BattleOfferPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattleOfferPacket { DataItems = packet.DataItems }, packet.Origin);
        }

        private void HandleBattlePokemonData(BattlePokemonDataPacket packet)
        {
            _server.SendToPlayer(packet.DestinationPlayerID, new BattlePokemonDataPacket { DataItems = packet.DataItems }, packet.Origin);
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
                CurrentPlayers = _server.PlayersCount,
                MaxPlayers = _server.MaxPlayers,
                ServerName = _server.ServerName,
                ServerMessage = _server.ServerMessage
            }, ID);

            _server.RemovePlayer(this);
        }
    }
}
