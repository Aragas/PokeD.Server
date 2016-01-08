using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Aragas.Core.Extensions;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data.P3D;
using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Data.PokeD.Monster.Data;
using PokeD.Core.Packets;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Trade;

using PokeD.Server.Clients;
using PokeD.Server.Clients.P3D;

namespace PokeD.Server
{
    public enum MuteStatus
    {
        Completed,
        PlayerNotFound,
        MutedYourself,
        IsNotMuted
    }

    public class ModuleP3D : IServerModule
    {
        const string FileName = "ModuleP3D.json";

        #region Settings

        [JsonProperty("Port")]
        public ushort Port { get; private set; } = 15124;

        [JsonProperty("ServerName", NullValueHandling = NullValueHandling.Ignore)]
        public string ServerName { get; private set; } = "Put name here";

        [JsonProperty("ServerMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string ServerMessage { get; private set; } = "Put description here";
        
        [JsonProperty("MaxPlayers")]
        public int MaxPlayers { get; private set; } = 1000;

        [JsonProperty("EncryptionEnabled")]
        public bool EncryptionEnabled { get; private set; } = true;

        [JsonProperty("MoveCorrectionEnabled")]
        public bool MoveCorrectionEnabled { get; private set; } = true;

        [JsonProperty("MutedPlayers")]
        public Dictionary<int, List<int>> MutedPlayers { get; } = new Dictionary<int, List<int>>();

        #endregion Settings

        [JsonIgnore]
        public Server Server { get; }

        bool IsDisposing { get; set; }

        ITCPListener Listener { get; set; }

        IThread PlayerWatcherThread { get; set; }
        IThread PlayerCorrectionThread { get; set; }


        [JsonIgnore]
        public ClientList Clients { get; } = new ClientList();
        List<IClient> PlayersJoining { get; } = new List<IClient>();
        List<IClient> PlayersToAdd { get; } = new List<IClient>();
        List<IClient> PlayersToRemove { get; } = new List<IClient>();


        ConcurrentQueue<PlayerPacketP3DOrigin> PacketsToPlayer { get; set; } = new ConcurrentQueue<PlayerPacketP3DOrigin>();
        ConcurrentQueue<PacketP3DOrigin> PacketsToAllPlayers { get; set; } = new ConcurrentQueue<PacketP3DOrigin>();

        ConcurrentDictionary<string, IClient[]> NearPlayers { get; } = new ConcurrentDictionary<string, IClient[]>();


        public ModuleP3D(Server server) { Server = server; }


        public void Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load SCON settings!");

            Logger.Log(LogType.Info, $"Starting SCON.");
            
            if (MoveCorrectionEnabled)
            {
                PlayerWatcherThread = ThreadWrapper.CreateThread(PlayerWatcherCycle);
                PlayerWatcherThread.Name = "PlayerWatcherThread";
                PlayerWatcherThread.IsBackground = true;
                PlayerWatcherThread.Start();

                PlayerCorrectionThread = ThreadWrapper.CreateThread(PlayerCorrectionCycle);
                PlayerCorrectionThread.Name = "PlayerCorrectionThread";
                PlayerCorrectionThread.IsBackground = true;
                PlayerCorrectionThread.Start();
            }
        }
        public void Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save SCON settings!");

            Logger.Log(LogType.Info, $"Stopping SCON.");

            if (PlayerWatcherThread.IsRunning)
                PlayerWatcherThread.Abort();

            if (PlayerCorrectionThread.IsRunning)
                PlayerCorrectionThread.Abort();

            Dispose();

            Logger.Log(LogType.Info, $"Stopped SCON.");
        }


        public static long PlayerWatcherThreadTime { get; private set; }
        private void PlayerWatcherCycle()
        {
            var watch = Stopwatch.StartNew();
            while (!IsDisposing)
            {
                var players = new List<IClient>(Clients.GetConcreteTypeEnumerator<P3DPlayer>());
                //var players = new List<IClient>(Clients.GetConcreteTypeEnumerator<P3DPlayer, ProtobufPlayer>());

                foreach (var player in players.Where(player => player.LevelFile != null && !NearPlayers.ContainsKey(player.LevelFile)))
                    NearPlayers.TryAdd(player.LevelFile, null);

                foreach (var level in NearPlayers.Keys)
                {
                    var playerList = new List<IClient>();
                    foreach (var player in players.Where(player => level == player.LevelFile))
                        playerList.Add(player);

                    var array = playerList.ToArray();
                    NearPlayers.AddOrUpdate(level, array, (s, players1) => players1 = array);
                }



                if (watch.ElapsedMilliseconds < 400)
                {
                    PlayerWatcherThreadTime = watch.ElapsedMilliseconds;

                    var time = (int)(400 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    ThreadWrapper.Sleep(time);
                }
                watch.Reset();
                watch.Start();
            }
        }

        public static long PlayerCorrectionThreadTime { get; private set; }
        private void PlayerCorrectionCycle()
        {
            var watch = Stopwatch.StartNew();
            while (!IsDisposing)
            {
                foreach (var nearPlayers in NearPlayers.Where(nearPlayers => nearPlayers.Value != null))
                    foreach (var player in nearPlayers.Value.Where(player => player.Moving))
                        foreach (var playerToSend in nearPlayers.Value.Where(playerToSend => player != playerToSend))
                            playerToSend.SendPacket(player.GetDataPacket(), player.ID);

                /*
                for (int i = 0; i < Players.Count; i++)
                {
                    var player1 = Players[i];
                    for (int j = 0; j < Players.Count; j++)
                    {
                        var player2 = Players[j];

                        if(player1 != player2)
                            if (player1.IsMoving)
                                SendToClient(player2, player1.GetDataPacket(), player1.ID);
                        //var nearPlayer = Players[i];
                        //if (nearPlayer.IsMoving)
                        //    SendToAllClients(nearPlayer.GetDataPacket(), nearPlayer.ID);
                    }
                    //if (nearPlayer.IsMoving)
                    //    SendToAllClients(nearPlayer.GetDataPacket(), nearPlayer.ID);
                }
                */


                if (watch.ElapsedMilliseconds < 5)
                {
                    PlayerCorrectionThreadTime = watch.ElapsedMilliseconds;

                    var time = (int)(5 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    ThreadWrapper.Sleep(time);
                }
                watch.Reset();
                watch.Start();
            }
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
                    PlayersJoining.Add(new P3DPlayer(Listener.AcceptTCPClient(), this));
        }


        public void AddClient(IClient client)
        {
            if (IsGameJoltIDUsed(client))
            {
                RemoveClient(client, "You are already on server!");
                return;
            }

            Server.LoadDBPlayer(client);

            P3DPlayerSendToClient(client, new IDPacket { PlayerID = client.ID }, -1);
            P3DPlayerSendToClient(client, new WorldDataPacket { DataItems = Server.World.GenerateDataItems() }, -1);

            // Send to player his ID
            P3DPlayerSendToClient(client, new CreatePlayerPacket { PlayerID = client.ID }, -1);
            // Send to player all Players ID
            var clients = Server.AllClients();
            for (var i = 0; i < clients.Count; i++)
            {
                P3DPlayerSendToClient(client, new CreatePlayerPacket { PlayerID = clients[i].ID }, -1);
                P3DPlayerSendToClient(client, clients[i].GetDataPacket(), clients[i].ID);
            }
            // Send to Players player ID
            P3DPlayerSendToAllClients(new CreatePlayerPacket { PlayerID = client.ID }, -1);
            P3DPlayerSendToAllClients(client.GetDataPacket(), client.ID);


            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);
        }
        public void RemoveClient(IClient client, string reason = "")
        {
            Server.UpdateDBPlayer(client);

            if (!string.IsNullOrEmpty(reason))
                client.SendPacket(new KickedPacket { Reason = reason }, -1);

            PlayersToRemove.Add(client);
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {
            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Clients.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);

                if (playerToAdd.ID != 0)
                {
                    Logger.Log(LogType.Server, $"The player {playerToAdd.Name} joined the game from IP {playerToAdd.IP}.");
                    P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {playerToAdd.Name} joined the game!" });

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
                    P3DPlayerSendToAllClients(new DestroyPlayerPacket { PlayerID = playerToRemove.ID });

                    Logger.Log(LogType.Server, $"The player {playerToRemove.Name} disconnected, playtime was {DateTime.Now - playerToRemove.ConnectionTime:hh\\:mm\\:ss}.");
                    P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {playerToRemove.Name} disconnected!" });

                    Server.ClientDisconnected(this, playerToRemove);
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

            PlayerPacketP3DOrigin packetToPlayer;
            while (!IsDisposing && PacketsToPlayer.TryDequeue(out packetToPlayer))
                packetToPlayer.Player.SendPacket(packetToPlayer.Packet, packetToPlayer.OriginID);

            PacketP3DOrigin packetToAllPlayers;
            while (!IsDisposing && PacketsToAllPlayers.TryDequeue(out packetToAllPlayers))
                for (var i = 0; i < Clients.Count; i++)
                    Clients[i].SendPacket(packetToAllPlayers.Packet, packetToAllPlayers.OriginID);

            #endregion Packet Sending



            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            for (var i = 0; i < Clients.Count; i++)
            {
                var player = Clients[i];
                if (player == null) continue;

                if (!player.UseCustomWorld)
                    P3DPlayerSendToClient(player, new WorldDataPacket { DataItems = Server.World.GenerateDataItems() }, -1);
            }

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }


        public void OtherConnected(IClient client)
        {
            P3DPlayerSendToAllClients(new CreatePlayerPacket { PlayerID = client.ID }, -1);
            P3DPlayerSendToAllClients(client.GetDataPacket(), client.ID);
        }
        public void OtherDisconnected(IClient client)
        {
            P3DPlayerSendToAllClients(new DestroyPlayerPacket { PlayerID = client.ID });
        }


        public void SendServerMessage(string message)
        {
            for (var i = 0; i < Clients.Count; i++)
            {
                var player = Clients[i];
                if (player.ChatReceiving)
                    player.SendPacket(new ChatMessageGlobalPacket { Message = message }, -1);
            }

            Server.ClientServerMessage(this, message);
        }
        public void SendPrivateMessage(IClient sender, IClient destClient, string message)
        {
            if (destClient.GetType() == typeof (P3DPlayer))
                P3DPlayerSendToClient(destClient, new ChatMessagePrivatePacket { DataItems = new DataItems(message) }, sender.ID);
            else
                Server.ClientPrivateMessage(this, sender, destClient, message);
        }
        public void SendGlobalMessage(IClient sender, string message)
        {
            P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = message }, sender.ID);

            Server.ClientGlobalMessage(this, sender, message);
        }

        public void SendTradeRequest(IClient sender, Monster monster, IClient destClient)
        {
            if (destClient.GetType() == typeof (P3DPlayer))
            {
                P3DPlayerSendToClient(destClient, new TradeRequestPacket(), sender.ID);

                var data = MonsterToDataItems(monster);
                P3DPlayerSendToClient(destClient, new TradeOfferPacket() { DataItems = data }, sender.ID);
            }
            else
                Server.ClientTradeOffer(this, sender, monster, destClient);
        }
        public void SendTradeConfirm(IClient sender, IClient destClient)
        {
            if (destClient.GetType() == typeof(P3DPlayer))
                P3DPlayerSendToClient(destClient, new TradeStartPacket(), sender.ID);
            else
                Server.ClientTradeConfirm(this, sender, destClient);
        }
        public void SendTradeCancel(IClient sender, IClient destClient)
        {
            if (destClient.GetType() == typeof(P3DPlayer))
                P3DPlayerSendToClient(destClient, new TradeQuitPacket(), sender.ID);
            else
                Server.ClientTradeCancel(this, sender, destClient);
        }


        public void P3DPlayerSendToClient(int destinationID, P3DPacket packet, int originID)
        {
            var player = Server.GetClient(destinationID);
            if (player != null)
                P3DPlayerSendToClient(player, packet, originID);
        }
        public void P3DPlayerSendToClient(IClient player, P3DPacket packet, int originID)
        {
            if (packet is TradeRequestPacket)
            {
                var client = Server.GetClient(originID);
                if(player.GetType() != typeof(P3DPlayer))
                    P3DPlayerSendToClient(client, new TradeJoinPacket(), player.ID);
            }
            if (packet is TradeStartPacket)
            {
                var client = Server.GetClient(originID);
                if (player.GetType() != typeof(P3DPlayer))
                    P3DPlayerSendToClient(client, new TradeStartPacket(), player.ID);
            }

            PacketsToPlayer.Enqueue(new PlayerPacketP3DOrigin(player, ref packet, originID));
        }
        public void P3DPlayerSendToAllClients(P3DPacket packet, int originID = -1)
        {
            if (originID != -1 && (packet is ChatMessageGlobalPacket || packet is ChatMessagePrivatePacket))
                if (MutedPlayers.ContainsKey(originID) && MutedPlayers[originID].Count > 0)
                {
                    for (var i = 0; i < Clients.Count; i++)
                    {
                        var player = Clients[i];
                        if (!MutedPlayers[originID].Contains(player.ID))
                            PacketsToPlayer.Enqueue(new PlayerPacketP3DOrigin(player, ref packet, originID));
                    }

                    return;
                }

            PacketsToAllPlayers.Enqueue(new PacketP3DOrigin(ref packet, originID));
        }

        private bool IsGameJoltIDUsed(IClient client)
        {
            for (int i = 0; i < Clients.Count; i++)
            {
                var player = Clients[i];
                if (player.IsGameJoltPlayer && client.GameJoltID == player.GameJoltID)
                    return true;
            }

            return false;
        }

        public MuteStatus MutePlayer(int id, string muteName)
        {
            if (!MutedPlayers.ContainsKey(id))
                MutedPlayers.Add(id, new List<int>());

            var muteID = Server.GetClientID(muteName);
            if (id == muteID)
                return MuteStatus.MutedYourself;

            if (muteID != -1)
            {
                MutedPlayers[id].Add(muteID);
                return MuteStatus.Completed;
            }

            return MuteStatus.PlayerNotFound;
        }
        public MuteStatus UnMutePlayer(int id, string muteName)
        {
            if (!MutedPlayers.ContainsKey(id))
                return MuteStatus.IsNotMuted;

            var muteID = Server.GetClientID(muteName);
            if (id == muteID)
                return MuteStatus.MutedYourself;

            if (muteID != -1)
            {
                MutedPlayers[id].Remove(muteID);
                return MuteStatus.Completed;
            }

            return MuteStatus.PlayerNotFound;
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
                Clients[i].SendPacket(new ServerClosePacket { Reason = "Closing server!" }, -1);
                Clients[i].Dispose();
            }
            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                PlayersToAdd[i].SendPacket(new ServerClosePacket { Reason = "Closing server!" }, -1);
                PlayersToAdd[i].Dispose();
            }

            // Do not dispose PlayersToRemove!


            Clients.Clear();
            PlayersJoining.Clear();
            PlayersToAdd.Clear();
            PlayersToRemove.Clear();

            PacketsToPlayer = null;
            PacketsToAllPlayers = null;

            NearPlayers.Clear();

            MutedPlayers.Clear();
        }


        public static Monster DataItemsToMonster(DataItems data)
        {
            var dict = DataItemsToDictionary(data);

            var id = short.Parse(dict["Pokemon"]);
            var name = string.IsNullOrEmpty(dict["NickName"]) ? string.Empty : dict["NickName"];
            var gender = (MonsterGender)int.Parse(dict["Gender"]);
            var isShiny = int.Parse(dict["isShiny"]) != 0;
            var nature = byte.Parse(dict["Nature"]);

            var dat = new MonsterInstanceData(id, name, gender, isShiny, nature)
            {
                Experience = int.Parse(dict["Experience"]),
                Abilities = new[] {short.Parse(dict["Ability"])},
                Friendship = byte.Parse(dict["Friendship"]),
                CatchInfo = new MonsterCatchInfo()
                {
                    PokeballID  = byte.Parse(dict["CatchBall"]),
                    Method      = dict["CatchMethod"],
                    Location    = dict["CatchLocation"],
                    TrainerName = dict["CatchTrainer"],
                    TrainerID   = (short) int.Parse(dict["OT"]).BitsGet(0, 16) == short.MaxValue
                                ? (short) int.Parse(dict["OT"]).BitsGet(16, 32)
                                : (short) int.Parse(dict["OT"]).BitsGet(0, 16)
                }
            };

            dat.HeldItem = short.Parse(dict["Item"]);

            var move0 = dict["Attack1"].Split(',');
            var move1 = dict["Attack2"].Split(',');
            var move2 = dict["Attack3"].Split(',');
            var move3 = dict["Attack4"].Split(',');
            dat.Moves = new MonsterMoves(
                new Move(int.Parse(move0[0]), int.Parse(move0[2]) - int.Parse(move0[1])),
                new Move(int.Parse(move1[0]), int.Parse(move1[2]) - int.Parse(move1[1])),
                new Move(int.Parse(move2[0]), int.Parse(move2[2]) - int.Parse(move2[1])),
                new Move(int.Parse(move3[0]), int.Parse(move3[2]) - int.Parse(move3[1])));

            dat.CurrentHP = short.Parse(dict["HP"]);

            var ev = dict["EVs"].Split(',');
            var ev0 = (short) (short.Parse(ev[0]) > 1 ? short.Parse(ev[0]) - 1 : short.Parse(ev[0]));
            var ev1 = (short) (short.Parse(ev[1]) > 1 ? short.Parse(ev[1]) - 1 : short.Parse(ev[1]));
            var ev2 = (short) (short.Parse(ev[2]) > 1 ? short.Parse(ev[2]) - 1 : short.Parse(ev[2]));
            var ev3 = (short) (short.Parse(ev[3]) > 1 ? short.Parse(ev[3]) - 1 : short.Parse(ev[3]));
            var ev4 = (short) (short.Parse(ev[4]) > 1 ? short.Parse(ev[4]) - 1 : short.Parse(ev[4]));
            var ev5 = (short) (short.Parse(ev[5]) > 1 ? short.Parse(ev[5]) - 1 : short.Parse(ev[5]));
            dat.EV = new MonsterStats(ev0, ev1, ev2, ev3, ev4, ev5);

            var iv = dict["IVs"].Split(',');
            var iv0 = (short) (short.Parse(iv[0]) > 1 ? short.Parse(iv[0]) - 1 : short.Parse(iv[0]));
            var iv1 = (short) (short.Parse(iv[1]) > 1 ? short.Parse(iv[1]) - 1 : short.Parse(iv[1]));
            var iv2 = (short) (short.Parse(iv[2]) > 1 ? short.Parse(iv[2]) - 1 : short.Parse(iv[2]));
            var iv3 = (short) (short.Parse(iv[3]) > 1 ? short.Parse(iv[3]) - 1 : short.Parse(iv[3]));
            var iv4 = (short) (short.Parse(iv[4]) > 1 ? short.Parse(iv[4]) - 1 : short.Parse(iv[4]));
            var iv5 = (short) (short.Parse(iv[5]) > 1 ? short.Parse(iv[5]) - 1 : short.Parse(iv[5]));
            dat.IV = new MonsterStats(iv0, iv1, iv2, iv3, iv4, iv5);

            return new Monster(dat);
        }
        public static DataItems MonsterToDataItems(Monster monster)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("Pokemon", $"[{monster.ID}]");
            dict.Add("Experience", $"[{monster.Experience}]");
            dict.Add("Gender", $"[{((int)monster.Gender)}]");
            dict.Add("EggSteps", $"[{monster.EggSteps}]");
            dict.Add("Item", $"[{monster.HeldItem}]");
            dict.Add("ItemData", $"[]");
            dict.Add("NickName", $"[{monster.DisplayName}]");
            dict.Add("Level", $"[{(monster.Level > 0 ? monster.Level - 1 : monster.Level)}]");
            dict.Add("OT", $"[{(monster.CatchInfo.TrainerID < 0 ? (short)-monster.CatchInfo.TrainerID : monster.CatchInfo.TrainerID)}]");
            dict.Add("Ability", $"[{string.Join(",", monster.Abilities.Select(ability => ability.ToString()).ToArray())}]");
            dict.Add("Status", $"[]"); // TODO
            dict.Add("Nature", $"[{monster.Nature}]");
            dict.Add("CatchLocation", $"[{monster.CatchInfo.Location}]");
            dict.Add("CatchTrainer", $"[{monster.CatchInfo.TrainerName}]");
            dict.Add("CatchBall", $"[{monster.CatchInfo.PokeballID}]");
            dict.Add("CatchMethod", $"[{monster.CatchInfo.Method}]");
            dict.Add("Friendship", $"[{monster.Friendship}]");
            dict.Add("isShiny", $"[{(monster.IsShiny ? 1 : 0)}]");
            dict.Add("Attack1", $"[{monster.Moves.Move_0.ID}, 10, 10]");
            dict.Add("Attack2", $"[{monster.Moves.Move_1.ID}, 10, 10]");
            dict.Add("Attack3", $"[{monster.Moves.Move_2.ID}, 10, 10]");
            dict.Add("Attack4", $"[{monster.Moves.Move_3.ID}, 10, 10]");
            dict.Add("HP", $"[{monster.CurrentHP}]");
            dict.Add("EVs", $"[{monster.InstanceData.EV.HP},{monster.InstanceData.EV.Attack},{monster.InstanceData.EV.Defense},{monster.InstanceData.EV.SpecialAttack},{monster.InstanceData.EV.SpecialDefense},{monster.InstanceData.EV.Speed}]");
            dict.Add("IVs", $"[{monster.InstanceData.IV.HP},{monster.InstanceData.IV.Attack},{monster.InstanceData.IV.Defense},{monster.InstanceData.IV.SpecialAttack},{monster.InstanceData.IV.SpecialDefense},{monster.InstanceData.IV.Speed}]");
            dict.Add("AdditionalData", $"[]");
            dict.Add("IDValue", $"[PokeD00Conv]");

            return DictionaryToDataItems(dict);
        }
        private static Dictionary<string, string> DataItemsToDictionary(DataItems data)
        {
            var dict  = new Dictionary<string, string>();
            var str = data.ToString();
            str = str.Replace("{", "");
            //str = str.Replace("}", ",");
            var array = str.Split('}');
            foreach (var s in array.Reverse().Skip(1))
            {
                var v = s.Split('"');
                dict.Add(v[1], v[2].Replace("[", "").Replace("]", ""));
            }

            return dict;
        }
        private static DataItems DictionaryToDataItems(Dictionary<string, string> dict)
        {
            //var builder = new StringBuilder("1*");
            var builder = new StringBuilder();

            foreach (var s in dict)
                builder.Append($"{{\"{s.Key}\"{s.Value}}}");
            
            return new DataItems(builder.ToString());
        }
        

        private class PlayerPacketP3DOrigin
        {
            public readonly IClient Player;
            public readonly P3DPacket Packet;
            public readonly int OriginID;

            public PlayerPacketP3DOrigin(IClient player, ref P3DPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
            public PlayerPacketP3DOrigin(IClient player, P3DPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
        } 
        private class PacketP3DOrigin
        {
            public readonly P3DPacket Packet;
            public readonly int OriginID;

            public PacketP3DOrigin(ref P3DPacket packet, int origin)
            {
                Packet = packet;
                OriginID = origin;
            }
        }
    }
}
