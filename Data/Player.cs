using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        CancellationTokenSource CancellationTokenSource { get; set; }
        Task CurrentTask { get; set; }


        readonly Server _server;

        // -- Debug -- //
        List<IPacket> _received = new List<IPacket>();
        List<IPacket> _sended = new List<IPacket>();
        // -- Debug -- //


        public Player(INetworkTcpClient client, Server server)
        {
            Stream = new PlayerStream(client);
            _server = server;

            CancellationTokenSource = new CancellationTokenSource();
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {
            if (!Stream.Connected)
                _server.RemovePlayer(this);

            /*
            if ((CurrentTask == null || CurrentTask.IsCompleted) && (Stream.Connected && Stream.DataAvailable > 0))
            {
                CurrentTask = Task.Factory.StartNew(() =>
                {
                    var data = Stream.ReadLine();

                    LastMessage = DateTime.UtcNow;
                    HandleData(Encoding.UTF8.GetBytes(data));
                }, CancellationTokenSource.Token);
            }
            */

            ///*
            if (Stream.Connected && Stream.DataAvailable > 0)
            {
                var data = Stream.ReadLine();

                LastMessage = DateTime.UtcNow;
                HandleData(Encoding.UTF8.GetBytes(data));
            }
            //*/

            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            if (UseCustomWorld)
            {
                CustomWorld.Update();
                SendPacket(new WorldDataPacket { DataItems = CustomWorld.GenerateDataItems() }, -1);
            }

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }


        private void HandleData(byte[] data)
        {
            using (var reader = new StreamReader(new MemoryStream(data)))
            {
                var str = reader.ReadLine();

                if (string.IsNullOrEmpty(str) || !IPacket.DataIsValid(str))
                    return;

                var packet = Response.Packets[IPacket.ParseID(str)]();
                packet.ParseData(str);

                _received.Add(packet);


                HandlePacket(packet);
            }
        }
        
        private void HandlePacket(IPacket packet)
        {
            switch ((PacketTypes) packet.ID)
            {
                case PacketTypes.Unknown:
                    break;

                case PacketTypes.GameData:
                    HandleGameData((GameDataPacket) packet);
                    break;

                case PacketTypes.PrivateMessage:
                    HandlePrivateMessage((ChatMessagePrivatePacket) packet);
                    break;

                case PacketTypes.ChatMessage:
                    HandleChatMessage((ChatMessagePacket) packet);
                    break;

                case PacketTypes.Ping:
                    LastPing = DateTime.UtcNow;
                    break;

                case PacketTypes.GameStateMessage:
                    HandleGameStateMessage((GameStateMessagePacket) packet);
                    break;

                case PacketTypes.TradeRequest:
                    HandleTradeRequest((TradeRequestPacket) packet);
                    break;

                case PacketTypes.TradeJoin:
                    HandleTradeJoin((TradeJoinPacket) packet);
                    break;

                case PacketTypes.TradeQuit:
                    HandleTradeQuit((TradeQuitPacket) packet);
                    break;

                case PacketTypes.TradeOffer:
                    HandleTradeOffer((TradeOfferPacket) packet);
                    break;

                case PacketTypes.TradeStart:
                    HandleTradeStart((TradeStartPacket) packet);
                    break;

                case PacketTypes.BattleRequest:
                    HandleBattleRequest((BattleRequestPacket) packet);
                    break;

                case PacketTypes.BattleJoin:
                    HandleBattleJoin((BattleJoinPacket) packet);
                    break;

                case PacketTypes.BattleQuit:
                    HandleBattleQuit((BattleQuitPacket) packet);
                    break;

                case PacketTypes.BattleOffer:
                    HandleBattleOffer((BattleOfferPacket) packet);
                    break;

                case PacketTypes.BattleStart:
                    HandleBattleStart((BattleStartPacket) packet);
                    break;

                case PacketTypes.BattleClientData:
                    HandleBattleClientData((BattleClientDataPacket) packet);
                    break;

                case PacketTypes.BattleHostData:
                    HandleBattleHostData((BattleHostDataPacket) packet);
                    break;

                case PacketTypes.BattlePokemonData:
                    HandleBattlePokemonData((BattlePokemonDataPacket) packet);
                    break;

                case PacketTypes.ServerDataRequest:
                    HandleServerDataRequest((ServerDataRequestPacket) packet);
                    break;
            }
        }

        
        public void SendPacket(IPacket packet, int originID)
        {
            if (Stream.Connected)
            {
                packet.ProtocolVersion = _server.ProtocolVersion;
                packet.Origin = originID == int.MinValue ? ID : originID;
                _sended.Add(packet);
                Stream.SendPacket(ref packet);
            }
        }

        public void SendGameDataPlayers(Player[] players)
        {
            foreach (var player in players)
            {
                if (player.ID != ID)
                {
                    var data = GenerateDataItems();
                    if (Positions.Count > 0)
                    {
                        Position += Positions.Dequeue();
                        //PokemonPosition += Positions.Dequeue();
                    }

                    data[6] = Position.ToPokeString();
                    //data[12] = PokemonPosition.ToPokeString();

                    player.SendPacket(new GameDataPacket { DataItems = data }, ID);
                }
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
                Position.ToPokeString(),
                Facing.ToString(),
                Moving ? "1" : "0",
                Skin,
                BusyType,
                PokemonVisible ? "1" : "0",
                PokemonPosition.ToPokeString(),
                PokemonSkin,
                PokemonFacing.ToString()
            };
            return new DataItems(list);
        }


        public void Dispose()
        {
            if(Stream != null)
                Stream.Dispose();

            if(CurrentTask != null && !CurrentTask.IsCompleted)
                CancellationTokenSource.Cancel();

            _server.RemovePlayer(this);
        }
    }
}