using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Aragas.Core.Data;
using Aragas.Core.Interfaces;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Data.PokeD.Monster.Data;
using PokeD.Core.Packets;
using PokeD.Core.Packets.PokeD.Battle;

using PokeD.Server.Clients;
using PokeD.Server.Clients.PokeD;

namespace PokeD.Server
{
    public class BattleTrainer
    {
        public IClient Client { get; }
        public bool HasAccepted { get; set; }

        public PokeDPacket LastCommand { get; set; }



        public BattleTrainer(IClient client) { Client = client; }
    }
    public class BattleInstance : IUpdatable, IDisposable
    {
        //public int BattleID { get; set; }

        private List<BattleTrainer> Trainers { get; }
        private BattleTrainer HostTrainer => Trainers.ElementAt(0);
        private List<BattleTrainer> OtherTrainers => Trainers.Skip(1).ToList();

        private string Message { get; }

        public BattleInstance(IEnumerable<IClient> players, string message)
        {
            Trainers = new List<BattleTrainer>(players.Select(client => new BattleTrainer(client)));
            Message = message;

            SendOffers();
        }
        private void SendOffers()
        {
            var playerIDs = Trainers.Select(c => (VarInt) c.Client.ID).ToArray();

            foreach (var client in Trainers)
                client.Client.SendPacket(new BattleOfferPacket { PlayerIDs = playerIDs, Message = Message }, 0);
        }

        Stopwatch UpdateWatch { get; } = Stopwatch.StartNew();
        public void Update()
        {
            // Stuff that is done every 0.5 second
            if (UpdateWatch.ElapsedMilliseconds < 500)
                return;

            UpdateWatch.Reset();
            UpdateWatch.Start();

            if (Trainers.All(trainer => trainer.LastCommand != null))
                DoRound();
        }
        private void DoRound()
        {



            foreach (var trainer in Trainers)
                trainer.LastCommand = null;
        }

        public void AcceptBattle(IClient player)
        {
            foreach (var trainer in Trainers.Where(trainer => trainer.Client.ID == player.ID))
                trainer.HasAccepted = true;
        }
        public void CancelBattle(IClient player)
        {
            foreach (var trainer in Trainers)
                trainer.Client.SendPacket(new BattleCancelledPacket { Reason = $"Player {player.Name} has denied the battle request!" }, 0);
        }

        public void HandlePacket(IClient player, PokeDPacket packet)
        {
            foreach (var trainer in Trainers.Where(trainer => trainer.Client.ID == player.ID))
            {
                if (!trainer.HasAccepted)
                    CancelBattle(player);

                trainer.LastCommand = packet;
                //trainer.DoneTurn = true;
            }
        }

        public void Dispose()
        {
            
        }
    }

    public class ModulePokeD : IServerModule
    {
        const string FileName = "ModulePokeD.json";

        #region Settings

        [JsonProperty("Port")]
        public ushort Port { get; private set; } = 15130;

        [JsonProperty("EncryptionEnabled")]
        public bool EncryptionEnabled { get; private set; } = true;


        #endregion Settings

        [JsonIgnore]
        public Server Server { get; }

        ITCPListener Listener { get; set; }


        [JsonIgnore]
        public ClientList Clients { get; } = new ClientList();
        [JsonIgnore]
        public bool ClientsVisible { get; } = true;
        List<BattleInstance> Battles { get; } = new List<BattleInstance>();


        public ModulePokeD(Server server)
        {
            Server = server;
        }


        public void Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load SCON settings!");

            Logger.Log(LogType.Info, $"Starting SCON.");

        }
        public void Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save SCON settings!");

            Logger.Log(LogType.Info, $"Stopping SCON.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped SCON.");
        }


        public void StartListen()
        {
            Listener = TCPListenerWrapper.CreateTCPListener(Port);
            Listener.Start();
        }
        public void CheckListener()
        {
            if (Listener != null && Listener.AvailableClients)
                if (Listener.AvailableClients)
                    AddClient(new PokeDPlayer(Listener.AcceptTCPClient(), this));
        }


        public void AddClient(IClient client)
        {
            if (!Server.LoadDBPlayer(client))
            {
                RemoveClient(client, "Wrong password!");
                return;
            }

            Clients.Add(client);

            Server.ClientConnected(this, client);
        }
        public void RemoveClient(IClient client, string reason = "")
        {
            Server.UpdateDBPlayer(client, true);

            //Clients.Remove(client);
            //
            //Server.ClientDisconnected(this, client);
        }

        bool b = false;
        public void Update()
        {
            if (!b)
            {
                var client = new PokeDPlayer(null, this);
                Server.PeekDBID(client);
                Server.LoadDBPlayer(client);
                Clients.Add(client);

                b = true;
            }

            for (var i = 0; i < Battles.Count; i++)
                Battles[i]?.Update();

            for (var i = 0; i < Clients.Count; i++)
                Clients[i]?.Update();
        }


        public void OtherConnected(IClient client) { }
        public void OtherDisconnected(IClient client) { }

        public void SendServerMessage(string message) { }
        public void SendPrivateMessage(IClient sender, IClient destClient, string message) { }
        public void SendGlobalMessage(IClient sender, string message) { }

        public void SendTradeRequest(IClient sender, Monster monster, IClient destClient)
        {
            var monstIns = new MonsterInstanceData(151, null, MonsterGender.Genderless, false, 11);
            monstIns.EV = new MonsterStats(255, 255, 255, 255, 255, 255);
            monstIns.IV = new MonsterStats(30, 30, 30, 30, 30, 30);
            monstIns.CurrentHP = 100;
            monstIns.Abilities = new[] { (short)30 };
            monstIns.Experience = 1059860;
            monstIns.CatchInfo = new MonsterCatchInfo()
            {
                Nickname = "TestShit",
                PokeballID = 1,
                TrainerID = 12345,
                Location = "at Aragas's Computer",
                TrainerName = "Aragas",
                Method = "As a gift from nilllzz"
            };
            monstIns.Moves = new MonsterMoves(
                new Move(1, 1),
                new Move(2, 1),
                new Move(3, 1),
                new Move(4, 1));
            var monst = new Monster(monstIns);

            Server.ClientTradeOffer(this, destClient, monst, sender);
        }
        public void SendTradeConfirm(IClient sender, IClient destClient) { }
        public void SendTradeCancel(IClient sender, IClient destClient) { }

        public BattleInstance CreateBattle(VarInt[] playerIDs, string message)
        {
            var battle = new BattleInstance(playerIDs.Select(playerID => Server.GetClient(playerID)), message);
            Battles.Add(battle);
            return battle;
        }


        public void Dispose()
        {
            for (int i = 0; i < Battles.Count; i++)
                Battles[i].Dispose();
            Battles.Clear();

            for (int i = 0; i < Clients.Count; i++)
                Clients[i].Dispose();
            Clients.Clear();
        }
    }
}
