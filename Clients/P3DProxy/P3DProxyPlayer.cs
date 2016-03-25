using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Aragas.Core.Data;
using Aragas.Core.Packets;
using Aragas.Core.Wrappers;

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

namespace PokeD.Server.Clients.P3DProxy
{
    public sealed partial class P3DProxyPlayer : Client
    {
        CultureInfo CultureInfo => CultureInfo.InvariantCulture;

        #region P3D Values

        public override int ID { get; set; }

        public string GameMode => "Pokemon 3D";
        public bool IsGameJoltPlayer => true;
        public long GameJoltID => 80085;
        private char DecimalSeparator => ',';


        private string _name;
        public override string Name { get { return Prefix != Prefix.NONE ? $"[{Prefix}] {_name}" : _name; } protected set { _name = value; } }


        public override string LevelFile { get { return "None"; } protected set { throw new NotSupportedException(); } }
        public override Vector3 Position { get { return new Vector3(0, 0, 0); } protected set { throw new NotSupportedException(); } }

        public int Facing => 0;
        public bool Moving => false;

        public string Skin => "ss";
        public string BusyType => "ss";

        public bool PokemonVisible => false;
        public Vector3 PokemonPosition => new Vector3(0, 0, 0);
        public string PokemonSkin => "ss";
        public int PokemonFacing => 0;

        #endregion P3D Values

        #region Values

        public override Prefix Prefix { get; protected set; }
        public override string PasswordHash { get; set; }

        public override string IP => Stream.Host;

        public override DateTime ConnectionTime { get; } = DateTime.Now;
        public override CultureInfo Language { get; }

        bool IsInitialized { get; set; }

        #endregion Values

        P3DStream Stream { get; }

        ModuleP3DProxy Module { get; }


#if DEBUG
        // -- Debug -- //
        List<P3DPacket> Received { get; } =  new List<P3DPacket>();
        List<P3DPacket> Sended { get; } = new List<P3DPacket>();
        // -- Debug -- //
#endif


        public P3DProxyPlayer(ITCPClient clientWrapper, ModuleP3DProxy module, string name)
        {
            Stream = new P3DStream(clientWrapper);
            Module = module;

            Name = name;
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public override void Update()
        {
            if (Stream.Connected)
            {
                if (Stream.DataAvailable > 0)
                {
                    var data = Stream.ReadLine();

                    HandleData(data);
                }

                if (UpdateWatch.ElapsedMilliseconds < 10000)
                    return;

                SendPacket(new PingPacket { Origin = ID });

                UpdateWatch.Reset();
                UpdateWatch.Start();
            }
            else
                Module.Dispose();
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
                                Logger.Log(LogType.Error, $"P3D Reading Error: Packet TryParseData error. Packet ID {id}, Packet Data: {data}.");
                        }
                        else
                            Logger.Log(LogType.Error, $"P3D Reading Error: SCONPacketResponses.Packets[{id}] is null.");
                    }
                    else
                        Logger.Log(LogType.Error, $"P3D Reading Error: Packets Length {P3DPacketResponses.Packets.Length} > Packet ID {id}, Packet Data: {data}.");
                }
                else
                    Logger.Log(LogType.Error, $"P3D Reading Error: Packet TryParseID error. Packet Data: {data}.");
            }
            else
                Logger.Log(LogType.Error, $"P3D Reading Error: Packet Data is null or empty.");
        }
        private void HandlePacket(P3DPacket packet)
        {
            switch ((P3DPacketTypes) packet.ID)
            {
                case P3DPacketTypes.ID:
                    HandleID((IDPacket) packet);
                    break;
                case P3DPacketTypes.CreatePlayer:
                    HandleCreatePlayer((CreatePlayerPacket) packet);
                    break;
                case P3DPacketTypes.DestroyPlayer:
                    HandleDestroyPlayer((DestroyPlayerPacket) packet);
                    break;

                    
                case P3DPacketTypes.GameData:
                    HandleGameData((GameDataPacket) packet);
                    break;


                case P3DPacketTypes.ServerMessage:
                    HandleServerMessage((ServerMessagePacket) packet);
                    break;
                case P3DPacketTypes.ChatMessagePrivate:
                    HandlePrivateMessage((ChatMessagePrivatePacket) packet);
                    break;

                case P3DPacketTypes.ChatMessageGlobal:
                    HandleChatMessage((ChatMessageGlobalPacket) packet);
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
            }
        }


        public override void SendPacket(Packet packet)
        {
            var p3dPacket = packet as P3DPacket;
            if (p3dPacket == null)
                throw new Exception($"Wrong packet type, {packet.GetType().FullName}");

            Stream.SendPacket(packet);

#if DEBUG
            Sended.Add(p3dPacket);
#endif
        }
        public override void SendMessage(string text) { }

        public override void LoadFromDB(Player data)
        {
            if (ID == 0)
                ID = data.Id;

            Prefix = data.Prefix;
        }


        private void Initialize()
        {
            if(IsInitialized)
                return;

            //Module.AddClient(this);
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
        public override GameDataPacket GetDataPacket() => new GameDataPacket { DataItems = GenerateDataItems() };


        public override void Dispose()
        {
            Stream.Disconnect();
            Stream.Dispose();
        }
    }
}