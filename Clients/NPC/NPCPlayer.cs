using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using Aragas.Network.Data;
using Aragas.Network.Packets;

using PokeD.Core.Data.P3D;
using PokeD.Core.Extensions;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Server.Chat;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients.NPC
{
    /*
    // Call Lua funcs, but put default variables first. So lua can override
    public partial class NPCPlayer : Client<ModuleNPC>, INPC
    {
        CultureInfo CultureInfo => CultureInfo.InvariantCulture;

        #region Game Values

        public override int ID { get; set; }

        private char DecimalSeparator { get; set; } = '.';
        public override string Nickname { get; protected set; } = string.Empty;
        public override Prefix Prefix { get; protected set; } = Prefix.NPC;
        public override string PasswordHash { get; set; } = string.Empty;

        public override string LevelFile { get; set; } = string.Empty;
        public override Vector3 Position { get { return _position; } set { _position = value; Module.SendPosition(this); } }
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

        public string NPCName { get { return Nickname; } set { Nickname = value; } }

        public override string IP => string.Empty;

        public override DateTime ConnectionTime { get; } = DateTime.MinValue;
        public override CultureInfo Language => new CultureInfo("ru-RU");
        public override PermissionFlags Permissions { get; set; }

        #endregion Other Values


        LuaScript Script { get; }
        LuaTable Hook => Lua.ToLuaTable(Script["hook"]);



        public NPCPlayer(LuaScript luaScript, ModuleNPC module) : base(module)
        {
            Script = luaScript;

            Script["NPC"] = this; // Sandbox it, a lot of vars opened
            Script.ReloadFile();
        }
        public NPCPlayer(string name, LuaScript luaScript, ModuleNPC module) : base(module)
        {
            Nickname = name;
            Script = luaScript;


            Script["NPC"] = this; // Sandbox it, a lot of vars opened
            Script.ReloadFile();
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


        public override bool RegisterOrLogIn(string passwordHash) => false;
        public override bool ChangePassword(string oldPassword, string newPassword) => false;

        public override void SendPacket(Packet packet) { }
        public override void SendChatMessage(ChatMessage chatMessage) { }
        public override void SendServerMessage(string text) { }
        public override void SendPrivateMessage(ChatMessage chatMessage) { }

        public override void LoadFromDB(ClientTable data)
        {
            if (ID == 0)
                ID = data.ID;

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
        public override GameDataPacket GetDataPacket() => new GameDataPacket { DataItems = GenerateDataItems() };

        public override void Dispose()
        {

        }


        public void SetNickname(string nickname) { Nickname = nickname; }

        public Client[] GetLocalPlayers() => Module.GetAllClients().Where(client => client != this && client.LevelFile == LevelFile).ToArray();

        public void Move(int x, int y, int z) { }

        public void SayPrivateMessage(Client client, string message) => client.SendPrivateMessage(new ChatMessage(client, message));
        public void SayGlobalMessage(string message) => Module.SendChatMessage(new ChatMessage(this, message));
    }
    */
}