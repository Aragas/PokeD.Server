using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Aragas.Core.Data;
using Aragas.Core.Packets;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data.P3D;
using PokeD.Core.Extensions;
using PokeD.Core.IO;
using PokeD.Core.Packets;
using PokeD.Core.Packets.P3D.Battle;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Client;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.P3D.Trade;

using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients.P3D
{
    public partial class P3DPlayer : IClient
    {
        CultureInfo CultureInfo => CultureInfo.InvariantCulture;

        #region Game Values

        [JsonIgnore]
        public int ID { get; set; }

        [JsonIgnore]
        public string GameMode { get; private set; }
        [JsonIgnore]
        public bool IsGameJoltPlayer { get; private set; }
        [JsonIgnore]
        public long GameJoltID { get; private set; }
        [JsonIgnore]
        private char DecimalSeparator { get; set; }


        private string _name;
        [JsonIgnore]
        public string Name { get { return Prefix != Prefix.NONE ? $"[{Prefix}] {_name}" : _name; } private set { _name = value; } }

        [JsonIgnore]
        public string LevelFile { get; private set; }
        [JsonIgnore]
        public Vector3 Position { get; private set; }
        [JsonIgnore]
        public int Facing { get; private set; }
        [JsonIgnore]
        public bool Moving { get; private set; }
        [JsonIgnore]
        public string Skin { get; private set; }
        [JsonIgnore]
        public string BusyType { get; private set; }
        [JsonIgnore]
        public bool PokemonVisible { get; private set; }
        [JsonIgnore]
        public Vector3 PokemonPosition { get; private set; }
        [JsonIgnore]
        public string PokemonSkin { get; private set; }
        [JsonIgnore]
        public int PokemonFacing { get; private set; }

        #endregion Game Values

        #region Other Values

        [JsonProperty("Password")]
        public PasswordStorage Password { get; set; }
        bool PasswordCorrect { get; set; }

        [JsonProperty("Prefix")]
        public Prefix Prefix { get; private set; }

        [JsonIgnore]
        public string IP => ClientWrapper.IP;

        [JsonIgnore]
        public DateTime ConnectionTime { get; } = DateTime.Now;

        [JsonIgnore]
        public DateTime LastMessage { get; private set; }
        [JsonIgnore]
        public DateTime LastPing { get; private set; }

        [JsonProperty("ChatReceiving")]
        public bool ChatReceiving => true;

        bool IsInitialized { get; set; }
        bool IsDisposed { get; set; }

        #endregion Other Values

        ITCPClient ClientWrapper { get; }
        P3DStream Stream { get; }


        readonly ModuleP3D _module;


#if DEBUG
        // -- Debug -- //
        List<P3DPacket> Received { get; } =  new List<P3DPacket>();
        List<P3DPacket> Sended { get; } = new List<P3DPacket>();
        // -- Debug -- //
#endif


        public P3DPlayer(ITCPClient clientWrapper, IServerModule server)
        {
            ClientWrapper = clientWrapper;
            Stream = new P3DStream(ClientWrapper);
            _module = (ModuleP3D) server;
        }


        Stopwatch UpdateWatch { get; } = Stopwatch.StartNew();
        public void Update()
        {
            if (Stream.Connected)
            {
                if (Stream.DataAvailable > 0)
                {
                    var data = Stream.ReadLine();
                    LastMessage = DateTime.UtcNow;

                    HandleData(data);
                }



                // Stuff that is done every 1 second
                if (UpdateWatch.ElapsedMilliseconds < 1000)
                    return;

                BattleUpdate();

                if (!Battling && _module.Server.CustomWorldEnabled && UseCustomWorld)
                {
                    CustomWorld.Update();
                    SendPacket(new WorldDataPacket { DataItems = CustomWorld.GenerateDataItems() }, -1);
                }

                UpdateWatch.Reset();
                UpdateWatch.Start();
            }
            else
                _module.RemoveClient(this);
        }

        private void HandleData(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                int id;
                if (P3DPacket.TryParseID(data, out id))
                {
                    if (P3DPacketResponses.Packets.Length > id)
                    {
                        if (P3DPacketResponses.Packets[id] != null)
                        {
                            var packet = P3DPacketResponses.Packets[id]();
                            if (packet.TryParseData(data))
                            {
                                HandlePacket(packet);
#if DEBUG
                                Received.Add(packet);
#endif
                            }
                            else
                                Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packet TryParseData error. Packet ID {id}, Packet Data: {data}.");
                        }
                        else
                            Logger.Log(LogType.GlobalError, $"P3D Reading Error: SCONPacketResponses.Packets[{id}] is null.");
                    }
                    else
                        Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packets Length {P3DPacketResponses.Packets.Length} > Packet ID {id}, Packet Data: {data}.");
                }
                else
                    Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packet TryParseID error. Packet Data: {data}.");
            }
            else
                Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packet Data is null or empty.");
        }
        private void HandlePacket(P3DPacket packet)
        {
            switch ((P3DPacketTypes) (int) packet.ID)
            {
                case P3DPacketTypes.GameData:
                    HandleGameData((GameDataPacket) packet);
                    break;

                case P3DPacketTypes.ChatMessagePrivate:
                    HandlePrivateMessage((ChatMessagePrivatePacket) packet);
                    break;

                case P3DPacketTypes.ChatMessageGlobal:
                    HandleChatMessage((ChatMessageGlobalPacket) packet);
                    break;

                case P3DPacketTypes.Ping:
                    LastPing = DateTime.UtcNow;
                    break;

                case P3DPacketTypes.GameStateMessage:
                    HandleGameStateMessage((GameStateMessagePacket) packet);
                    break;


                case P3DPacketTypes.TradeRequest:
                    HandleTradeRequest((TradeRequestPacket) packet);
                    break;

                case P3DPacketTypes.TradeJoin:
                    HandleTradeJoin((TradeJoinPacket) packet);
                    break;

                case P3DPacketTypes.TradeQuit:
                    HandleTradeQuit((TradeQuitPacket) packet);
                    break;

                case P3DPacketTypes.TradeOffer:
                    HandleTradeOffer((TradeOfferPacket) packet);
                    break;

                case P3DPacketTypes.TradeStart:
                    HandleTradeStart((TradeStartPacket) packet);
                    break;


                case P3DPacketTypes.BattleRequest:
                    HandleBattleRequest((BattleRequestPacket) packet);
                    break;

                case P3DPacketTypes.BattleJoin:
                    HandleBattleJoin((BattleJoinPacket) packet);
                    break;

                case P3DPacketTypes.BattleQuit:
                    HandleBattleQuit((BattleQuitPacket) packet);
                    break;

                case P3DPacketTypes.BattleOffer:
                    HandleBattleOffer((BattleOfferPacket) packet);
                    break;

                case P3DPacketTypes.BattleStart:
                    HandleBattleStart((BattleStartPacket) packet);
                    break;

                case P3DPacketTypes.BattleClientData:
                    HandleBattleClientData((BattleClientDataPacket) packet);
                    break;

                case P3DPacketTypes.BattleHostData:
                    HandleBattleHostData((BattleHostDataPacket) packet);
                    break;

                case P3DPacketTypes.BattlePokemonData:
                    HandleBattlePokemonData((BattlePokemonDataPacket) packet);
                    break;


                case P3DPacketTypes.ServerDataRequest:
                    HandleServerDataRequest((ServerDataRequestPacket) packet);
                    break;
            }
        }


        public void SendPacket(ProtobufPacket packet, int originID)
        {
            var protoOrigin = packet as P3DPacket;
            if(protoOrigin == null)
                throw new Exception($"Wrong packet type, {packet.GetType().FullName}");

            protoOrigin.Origin = originID;

            Stream.SendPacket(ref protoOrigin);

#if DEBUG
            Sended.Add(protoOrigin);
#endif
        }

        public void LoadFromDB(Player data)
        {
            if (ID == 0)
                ID = data.Id;

            Prefix = data.Prefix;

            UseCustomWorld = data.IsUsingCustomWorld;
        }


        private void Initialize()
        {
            if(IsInitialized)
                return;

            _module.AddClient(this);
            IsInitialized = true;
        }

        private DataItems GenerateDataItems()
        {
            return new DataItems(
                GameMode,
                IsGameJoltPlayer ? "1" : "0",
                GameJoltID.ToString(CultureInfo),
                DecimalSeparator.ToString(),
                Name,
                LevelFile,
                Position.ToPokeString(DecimalSeparator, CultureInfo),
                Facing.ToString(CultureInfo),
                Moving ? "1" : "0",
                Skin,
                BusyType,
                PokemonVisible ? "1" : "0",
                PokemonPosition.ToPokeString(DecimalSeparator, CultureInfo),
                PokemonSkin,
                PokemonFacing.ToString(CultureInfo));
        }
        public GameDataPacket GetDataPacket() => new GameDataPacket { DataItems = GenerateDataItems() };


        private void DisconnectAndDispose()
        {
            Stream.Disconnect();
            Stream.Dispose();
        }
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;


            DisconnectAndDispose();
        }
    }
}