using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Newtonsoft.Json;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.IO;
using PokeD.Core.Packets;
using PokeD.Core.Packets.Battle;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Client;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;
using PokeD.Core.Wrappers;

using PokeD.Server.IO;

namespace PokeD.Server.Data
{
    public partial class Player : IUpdatable, IDisposable
    {

        #region Game Values

        [JsonIgnore]
        public int ID { get; set; }

        [JsonIgnore]
        public bool Initialized { get; set; }

        [JsonIgnore]
        public string GameMode { get; set; }
        [JsonIgnore]
        public bool IsGameJoltPlayer { get; set; }
        [JsonIgnore]
        public long GameJoltId { get; set; }
        [JsonIgnore]
        public char DecimalSeparator { get; set; }
        [JsonIgnore]
        public string Name { get; set; }
        [JsonIgnore]
        public string LevelFile { get; set; }
        [JsonIgnore]
        public Vector3 Position { get; set; }
        [JsonIgnore]
        public int Facing { get; set; }
        [JsonIgnore]
        public bool Moving { get; set; }
        [JsonIgnore]
        public string Skin { get; set; }
        [JsonIgnore]
        public string BusyType { get; set; }
        [JsonIgnore]
        public bool PokemonVisible { get; set; }
        [JsonIgnore]
        public Vector3 PokemonPosition { get; set; }
        [JsonIgnore]
        public string PokemonSkin { get; set; }
        [JsonIgnore]
        public int PokemonFacing { get; set; }

        [JsonIgnore]
        public DateTime LastMessage { get; set; }
        [JsonIgnore]
        public DateTime LastPing { get; set; }


        #endregion Game Values

        IPokeStream Stream { get; set; }


        readonly Server _server;

#if DEBUG
        // -- Debug -- //
        List<IPacket> _received = new List<IPacket>();
        List<IPacket> _sended = new List<IPacket>();
        // -- Debug -- //
#endif


        public Player(INetworkTCPClient client, Server server)
        {
            Stream = new PlayerStream(client);
            _server = server;

            MovingUpdateRate = 60;
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {
            if (!Stream.Connected)
                _server.RemovePlayer(this);

            if (Stream.Connected && Stream.DataAvailable > 0)
            {
                var data = Stream.ReadLine();

                LastMessage = DateTime.UtcNow;
                HandleData(data);
            }


            // Stuff that is done every 1 second
            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            if (_server.CustomWorldEnabled && UseCustomWorld)
            {
                CustomWorld.Update();
                SendPacket(new WorldDataPacket { DataItems = CustomWorld.GenerateDataItems() }, -1);
            }

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }


        private void HandleData(string data)
        {
            if (string.IsNullOrEmpty(data) || !IPacket.DataIsValid(data))
                return;

            int id;
            if (IPacket.TryParseID(data, out id))
            {
                var packet = PlayerResponse.Packets[id]().ParseData(data);

                if (packet != null)
                {
                    HandlePacket(packet);
#if DEBUG
                    _received.Add(packet);
#endif
                }
            }
        }

        private void HandlePacket(IPacket packet)
        {
            switch ((PlayerPacketTypes) packet.ID)
            {
                case PlayerPacketTypes.Unknown:
                    break;

                case PlayerPacketTypes.GameData:
                    HandleGameData((GameDataPacket) packet);
                    break;

                case PlayerPacketTypes.PrivateMessage:
                    HandlePrivateMessage((ChatMessagePrivatePacket) packet);
                    break;

                case PlayerPacketTypes.ChatMessage:
                    HandleChatMessage((ChatMessagePacket) packet);
                    break;

                case PlayerPacketTypes.Ping:
                    LastPing = DateTime.UtcNow;
                    break;

                case PlayerPacketTypes.GameStateMessage:
                    HandleGameStateMessage((GameStateMessagePacket) packet);
                    break;

                case PlayerPacketTypes.TradeRequest:
                    HandleTradeRequest((TradeRequestPacket) packet);
                    break;

                case PlayerPacketTypes.TradeJoin:
                    HandleTradeJoin((TradeJoinPacket) packet);
                    break;

                case PlayerPacketTypes.TradeQuit:
                    HandleTradeQuit((TradeQuitPacket) packet);
                    break;

                case PlayerPacketTypes.TradeOffer:
                    HandleTradeOffer((TradeOfferPacket) packet);
                    break;

                case PlayerPacketTypes.TradeStart:
                    HandleTradeStart((TradeStartPacket) packet);
                    break;

                case PlayerPacketTypes.BattleRequest:
                    HandleBattleRequest((BattleRequestPacket) packet);
                    break;

                case PlayerPacketTypes.BattleJoin:
                    HandleBattleJoin((BattleJoinPacket) packet);
                    break;

                case PlayerPacketTypes.BattleQuit:
                    HandleBattleQuit((BattleQuitPacket) packet);
                    break;

                case PlayerPacketTypes.BattleOffer:
                    HandleBattleOffer((BattleOfferPacket) packet);
                    break;

                case PlayerPacketTypes.BattleStart:
                    HandleBattleStart((BattleStartPacket) packet);
                    break;

                case PlayerPacketTypes.BattleClientData:
                    HandleBattleClientData((BattleClientDataPacket) packet);
                    break;

                case PlayerPacketTypes.BattleHostData:
                    HandleBattleHostData((BattleHostDataPacket) packet);
                    break;

                case PlayerPacketTypes.BattlePokemonData:
                    HandleBattlePokemonData((BattlePokemonDataPacket) packet);
                    break;

                case PlayerPacketTypes.ServerDataRequest:
                    HandleServerDataRequest((ServerDataRequestPacket) packet);
                    break;
            }
        }

        
        public void SendPacket(IPacket packet, int originID)
        {
            if (Stream.Connected)
            {
                packet.ProtocolVersion = _server.ProtocolVersion;
                packet.Origin = originID;

                Stream.SendPacket(ref packet);

#if DEBUG
                _sended.Add(packet);
#endif
            }
        }


        /// <summary>
        /// Call it only from 16 ms thread.
        /// </summary>
        /// <param name="players"></param>
        public void SendGameDataPlayers(Player[] players)
        {
            foreach (var player in players.Where(player => player.ID != ID))
            {
                var data = GenerateDataItems();

                var pos = Positions.Dequeue();
                Position += pos;
                //PokemonPosition += pos;

                //Position = Positions.Dequeue();


                data[6] = Position.ToPokeString(DecimalSeparator);
                //data[12] = PokemonPosition.ToPokeString();

                player.SendPacket(new GameDataPacket {DataItems = data}, ID);
                //player.SendPacket(new GameDataPacket { DataItems = GenerateDataItems() }, ID);
            }
        }

        public DataItems GenerateDataItems()
        {
            var list = new List<string>
            {
                GameMode,
                IsGameJoltPlayer ? "1" : "0",
                GameJoltId.ToString(),
                DecimalSeparator.ToString(),
                Name,
                LevelFile,
                Position.ToPokeString(DecimalSeparator),
                Facing.ToString(),
                Moving ? "1" : "0",
                Skin,
                BusyType,
                PokemonVisible ? "1" : "0",
                PokemonPosition.ToPokeString(DecimalSeparator),
                PokemonSkin,
                PokemonFacing.ToString()
            };
            return new DataItems(list);
        }

        public DataItems GenerateDataItems(char separator)
        {
            var list = new List<string>
            {
                GameMode,
                IsGameJoltPlayer ? "1" : "0",
                GameJoltId.ToString(),
                DecimalSeparator.ToString(),
                Name,
                LevelFile,
                Position.ToPokeString(separator),
                Facing.ToString(),
                Moving ? "1" : "0",
                Skin,
                BusyType,
                PokemonVisible ? "1" : "0",
                PokemonPosition.ToPokeString(separator),
                PokemonSkin,
                PokemonFacing.ToString()
            };
            return new DataItems(list);
        }


        public void Dispose()
        {
            if(Stream != null)
                Stream.Dispose();

            _server.RemovePlayer(this);
        }
    }
}