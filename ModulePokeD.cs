using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Aragas.Core.Data;
using Aragas.Core.Interfaces;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Packets;
using PokeD.Core.Packets.PokeD.Authorization;
using PokeD.Core.Packets.PokeD.Battle;
using PokeD.Core.Packets.PokeD.Chat;
using PokeD.Core.Packets.PokeD.Overworld;
using PokeD.Core.Packets.PokeD.Trade;

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
        bool IsDisposing { get; set; }

        ITCPListener Listener { get; set; }


        [JsonIgnore]
        public ClientList Clients { get; } = new ClientList();
        [JsonIgnore]
        public bool ClientsVisible { get; } = true;
        List<IClient> PlayersJoining { get; } = new List<IClient>();
        List<IClient> PlayersToAdd { get; } = new List<IClient>();
        List<IClient> PlayersToRemove { get; } = new List<IClient>();

        ConcurrentQueue<PlayerPacketPokeD> PacketsToPlayer { get; set; } = new ConcurrentQueue<PlayerPacketPokeD>();
        ConcurrentQueue<PokeDPacket> PacketsToAllPlayers { get; set; } = new ConcurrentQueue<PokeDPacket>();

        List<BattleInstance> Battles { get; } = new List<BattleInstance>();


        public ModulePokeD(Server server) { Server = server; }


        public void Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load PokeD settings!");

            Logger.Log(LogType.Info, $"Starting PokeD.");
        }
        public void Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save PokeD settings!");

            Logger.Log(LogType.Info, $"Stopping PokeD.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped PokeD.");
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

            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);

            PokeDPlayerSendToClient(client, new AuthorizationCompletePacket { PlayerID = client.ID });
            //PokeDPlayerSendToClient(client, new PlayerList { PlayerID = client.ID });
        }
        public void RemoveClient(IClient client, string reason = "")
        {
            if (!string.IsNullOrEmpty(reason))
                client.SendPacket(new DisconnectPacket { Reason = reason });

            PlayersToRemove.Add(client);
        }

        bool b = false;
        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {
            /*
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
            */

            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Clients.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);

                if (playerToAdd.ID != 0)
                {
                    Logger.Log(LogType.Server, $"The player {playerToAdd.Name} joined the game from IP {playerToAdd.IP}.");
                    PokeDPlayerSendToAllClients(new ChatGlobalMessagePacket() { Message = $"Player {playerToAdd.Name} joined the game!" });

                    Server.ClientConnected(this, playerToAdd);
                }
            }

            for (var i = 0; i < PlayersToRemove.Count; i++)
            {
                var playerToRemove = PlayersToRemove[i];

                Clients.Remove(playerToRemove);
                PlayersJoining.Remove(playerToRemove);
                PlayersToRemove.Remove(playerToRemove);

                if (playerToRemove.ID != 0)
                {
                    //PokeDPlayerSendToAllClients(new DestroyPlayerPacket { PlayerID = playerToRemove.ID });
                    //
                    //Logger.Log(LogType.Server, $"The player {playerToRemove.Name} disconnected, playtime was {DateTime.Now - playerToRemove.ConnectionTime:hh\\:mm\\:ss}.");
                    //PokeDPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {playerToRemove.Name} disconnected!" });
                    //
                    //Server.ClientDisconnected(this, playerToRemove);
                }

                playerToRemove.Dispose();
            }

            #endregion Player Filtration


            #region Player Updating

            // Update actual players
            for (var i = 0; i < Clients.Count; i++)
                Clients[i]?.Update();

            // Update joining players
            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i]?.Update();

            #endregion Player Updating


            #region Packet Sending

            PlayerPacketPokeD packetToPlayer;
            while (!IsDisposing && PacketsToPlayer.TryDequeue(out packetToPlayer))
                packetToPlayer.Player.SendPacket(packetToPlayer.Packet);

            PokeDPacket packetToAllPlayers;
            while (!IsDisposing && PacketsToAllPlayers.TryDequeue(out packetToAllPlayers))
                for (var i = 0; i < Clients.Count; i++)
                    Clients[i].SendPacket(packetToAllPlayers);

            #endregion Packet Sending



            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            //for (var i = 0; i < Clients.Count; i++)
            //{
            //    var player = Clients[i];
            //    if (player == null) continue;
            //
            //    //PokeDPlayerSendToClient(player, new WorldDataPacket { DataItems = Server.World.GenerateDataItems() }, -1);
            //}

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }


        public void OtherConnected(IClient client) { }
        public void OtherDisconnected(IClient client) { }

        public void SendServerMessage(string message)
        {
            PokeDPlayerSendToAllClients(new ChatServerMessagePacket() { Message = message });

            Server.ClientServerMessage(this, message);
        }
        public void SendPrivateMessage(IClient sender, IClient destClient, string message)
        {
            if (destClient is PokeDPlayer)
                PokeDPlayerSendToClient(destClient, new ChatPrivateMessagePacket() { Message = message });
            else
                Server.ClientPrivateMessage(this, sender, destClient, message);
        }
        public void SendGlobalMessage(IClient sender, string message)
        {
            PokeDPlayerSendToAllClients(new ChatGlobalMessagePacket() { Message = message });

            Server.ClientGlobalMessage(this, sender, message);
        }

        public void SendTradeRequest(IClient sender, Monster monster, IClient destClient)
        {
            if (destClient is PokeDPlayer)
                PokeDPlayerSendToClient(destClient, new TradeOfferPacket() { DestinationID = -1, MonsterData = monster.InstanceData });
            else
                Server.ClientTradeOffer(this, sender, monster, destClient);

            /*
            var monstIns = new MonsterInstanceData(151, MonsterGender.Genderless, false, new short[] {30}, 11)
            {
                EV = new MonsterStats(255, 255, 255, 255, 255, 255),
                IV = new MonsterStats(30, 30, 30, 30, 30, 30),

                CurrentHP = 100,
                Experience = 1059860,

                CatchInfo = new MonsterCatchInfo()
                {
                    Nickname = "TestShit",
                    PokeballID = 1,
                    TrainerID = 12345,
                    Location = "at Aragas's Computer",
                    TrainerName = "Aragas",
                    Method = "As a gift from nilllzz"
                },

                Moves = new MonsterMoves(
                    new Move(1, 1),
                    new Move(2, 1),
                    new Move(3, 1),
                    new Move(4, 1))
            };
            var monst = new Monster(monstIns);

            Server.ClientTradeOffer(this, destClient, monst, sender);
            */
        }
        public void SendTradeConfirm(IClient sender, IClient destClient)
        {
            if (destClient is PokeDPlayer)
                PokeDPlayerSendToClient(destClient, new TradeAcceptPacket() { DestinationID = -1 });
            else
                Server.ClientTradeConfirm(this, sender, destClient);
        }
        public void SendTradeCancel(IClient sender, IClient destClient)
        {
            if (destClient is PokeDPlayer)
                PokeDPlayerSendToClient(destClient, new TradeRefusePacket() { DestinationID = -1 });
            else
                Server.ClientTradeCancel(this, sender, destClient);
        }


        public void PokeDPlayerSendToClient(int destinationID, PokeDPacket packet)
        {
            var player = Server.GetClient(destinationID);
            if (player != null)
                PokeDPlayerSendToClient(player, packet);
        }
        public void PokeDPlayerSendToClient(IClient player, PokeDPacket packet)
        {
            //player.SendPacket(packet);

            PacketsToPlayer.Enqueue(new PlayerPacketPokeD(player, ref packet));
        }
        public void PokeDPlayerSendToAllClients(PokeDPacket packet)
        {
            //for (int i = 0; i < Clients.Count; i++)
            //    Clients[i].SendPacket(packet);

            PacketsToAllPlayers.Enqueue(packet);
        }


        public BattleInstance CreateBattle(VarInt[] playerIDs, string message)
        {
            var battle = new BattleInstance(playerIDs.Select(playerID => Server.GetClient(playerID)), message);
            Battles.Add(battle);
            return battle;
        }


        public void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;


            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Dispose();

            for (var i = 0; i < Clients.Count; i++)
            {
                Clients[i].SendPacket(new DisconnectPacket() { Reason = "Closing server!" }, -1);
                Clients[i].Dispose();
            }
            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                PlayersToAdd[i].SendPacket(new DisconnectPacket() { Reason = "Closing server!" }, -1);
                PlayersToAdd[i].Dispose();
            }

            // Do not dispose PlayersToRemove!

            for (int i = 0; i < Battles.Count; i++)
                Battles[i].Dispose();


            Clients.Clear();
            PlayersJoining.Clear();
            PlayersToAdd.Clear();
            PlayersToRemove.Clear();

            PacketsToPlayer = null;
            PacketsToAllPlayers = null;

            Battles.Clear();
        }

        private class PlayerPacketPokeD
        {
            public readonly IClient Player;
            public readonly PokeDPacket Packet;

            public PlayerPacketPokeD(IClient player, ref PokeDPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
            }
            public PlayerPacketPokeD(IClient player, PokeDPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
            }
        }
    }
}
