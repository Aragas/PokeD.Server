using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using Aragas.Core.Data;
using Aragas.Core.Packets;
using Aragas.Core.Wrappers;

using PokeD.Core.Data.P3D;
using PokeD.Core.Extensions;
using PokeD.Core.Packets;
using PokeD.Core.Packets.P3D.Battle;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.P3D.Trade;

using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients.NPC
{

    // Call Lua funcs, but put default variables first. So lua can override
    public partial class NPCPlayer : Client, INPC
    {
        CultureInfo CultureInfo => CultureInfo.InvariantCulture;

        #region Game Values

        public override int ID { get; set; }

        private char DecimalSeparator { get; set; } = '.';
        public string PureName { get; set; } = string.Empty;
        public override string Name { get { return Prefix != Prefix.NONE ? $"[{Prefix}] {PureName}" : PureName; } protected set { PureName = value; } }
        public override Prefix Prefix { get; protected set; } = Prefix.NPC;
        public override string PasswordHash { get; set; } = string.Empty;

        public override string LevelFile { get; protected set; } = string.Empty;
        public override Vector3 Position { get { return _position; } protected set { _position = value; Module.SendPosition(this); } }
        private Vector3 _position;
        public int Facing { get; set; }
        public bool Moving { get; set; }
        public string Skin { get; set; } = string.Empty;
        public string BusyType { get; set; } = string.Empty;
        public bool PokemonVisible { get; set; }
        public Vector3 PokemonPosition { get; set; }
        public string PokemonSkin { get; set; } = string.Empty;
        public int PokemonFacing { get; set; }

        #endregion Game Values

        #region Other Values

        public override string IP => "NPC";

        public override DateTime ConnectionTime { get; } = DateTime.MinValue;
        public override CultureInfo Language => new CultureInfo("ru-RU");

        #endregion Other Values


        IServerModule Module { get; }

        LuaScript Lua { get; }
        LuaTable Hook => LuaWrapper.ToLuaTable(Lua["hook"]);

        
#if DEBUG
        List<P3DPacket> Received { get; } =  new List<P3DPacket>();
#endif


        public NPCPlayer(string name, LuaScript luaScript, IServerModule module)
        {
            Module = module;

            Name = name;
            Lua = luaScript;


            Lua["NPC"] = this; // Sandbox it, a lot of vars opened
            Lua.ReloadFile();
        }

        Stopwatch UpdateWatch { get; } = Stopwatch.StartNew();
        public override void Update()
        {
            Hook.CallFunction("Call", "Update");

            // Stuff that is done every 1 second
            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            Hook.CallFunction("Call", "Update2");

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }
        //public void BattleUpdate(BattleDataTable battleData)
        //{
        //    Hook.CallFunction("Call", "BattleUpdate", battleData);
        //}

        public Client[] GetLocalPlayers() => Module.Server.AllClients().Where(client => client != this && client.LevelFile == LevelFile).ToArray();
        //public Client[] GetLocalPlayers() => Module.Server.AllClients().Where(client => client.LevelFile == LevelFile).ToArray();


        public override void SendPacket(Packet packet)
        {
            var protoOrigin = packet as P3DPacket;
            if (protoOrigin == null)
                throw new Exception($"Wrong packet type, {packet.GetType().FullName}");

#if DEBUG
            Received.Add(protoOrigin);
#endif

            HandlePacket(protoOrigin);
        }

        private void HandlePacket(P3DPacket packet)
        {
            switch ((P3DPacketTypes) (int) packet.ID)
            {
                case P3DPacketTypes.ChatMessagePrivate:
                    HandlePrivateMessage((ChatMessagePrivatePacket) packet);
                    break;

                case P3DPacketTypes.ChatMessageGlobal:
                    HandleChatMessage((ChatMessageGlobalPacket) packet);
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
            }
        }

        public override void LoadFromDB(Player data)
        {
            if (ID == 0)
                ID = data.Id;

            Prefix = data.Prefix;
        }
        
        private DataItems GenerateDataItems()
        {
            return new DataItems(
                "NPC",  // Gamemode
                "1",    // IsGameJolt
                "0",    // GameJolt ID
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
        public override GameDataPacket GetDataPacket() { return new GameDataPacket { DataItems = GenerateDataItems() }; }
        
        public override void Dispose()
        {

        }


        public void Move(int x, int y, int z)
        {

        }

        public void SayPlayerPM(Client client, string message) { Module.SendPrivateMessage(this, client, message); }
        public void SayGlobal(string message) { Module.SendGlobalMessage(this, message); }
        public void SayGlobal(object message) {  }
    }
}