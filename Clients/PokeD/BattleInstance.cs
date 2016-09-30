using System;
using System.Diagnostics;
using System.Linq;

using PokeD.Core;
using PokeD.Core.Data.PokeD.Battle;
using PokeD.Core.Packets.PokeD.Battle;
using PokeD.Server.DatabaseData;

namespace PokeD.Server.Clients.PokeD
{
    public class BattleInstance : IUpdatable, IDisposable
    {
        private Server Server { get; }

        public Guid BattleID { get; set; } = Guid.NewGuid();

        public IBattleInfo Trainers { get; }

        public string Message { get; }

        public BattleInstance(Server server, IBattleInfo players, string message)
        {
            Server = server;

            Trainers = players;
            Message = message;

            SendOffers();
        }
        private void SendOffers()
        {
            var playerIDs = Trainers.IDs;

            foreach (var client in Trainers.IDs.Select(clientID => Server.GetClient(clientID)))
                client.SendPacket(new BattleOfferPacket { PlayerIDs = playerIDs.ToArray(), Message = Message});
        }

        Stopwatch UpdateWatch { get; } = Stopwatch.StartNew();
        public void Update()
        {
            // Stuff that is done every 0.5 second
            if (UpdateWatch.ElapsedMilliseconds < 500)
                return;

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }

        public void LoadFromDB(Battle data) { }

        public void EndBattle()
        {


            Server.DatabaseBatteSave(this);
        }

        public void Dispose()
        {

        }


        public void AcceptBattle(Client player)
        {
            //foreach (var trainer in Trainers.Where(trainer => trainer.Client.ID == player.ID))
            //    trainer.HasAccepted = true;
        }
        public void CancelBattle(Client player)
        {
            //foreach (var trainer in Trainers)
            //    trainer.Client.SendPacket(new BattleCancelledPacket { Reason = $"Player {player.Name} has denied the battle request!" });
        }


        public void HandleAttack(Client client, int currentMonster, int targetMonster, int move)
        {
            
        }

        public void HandleBattleItem(Client client, int monster, int item)
        {
            
        }

        public void HandleBattleSwitch(Client client, int currentMonster, int switchMonster)
        {
             
        }

        public void HandleBattleFlee(Client client)
        {
            
        }
    }
}
