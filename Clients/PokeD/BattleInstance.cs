using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Aragas.Core.Data;
using Aragas.Core.Interfaces;
using PokeD.Core.Data.PokeD.Battle;
using PokeD.Core.Packets;

namespace PokeD.Server.Clients.PokeD
{
    public class BattleInstance : IUpdatable, IDisposable
    {
        public int BattleID { get; set; }

        private IBattleInfo Trainers { get; }

        private string Message { get; }

        public BattleInstance(IBattleInfo players, string message)
        {
            Trainers = players;
            Message = message;

            SendOffers();
        }
        private void SendOffers()
        {
            //var playerIDs = Trainers.Select(c => (VarInt)c.Client.ID).ToArray();
            //
            //foreach (var client in Trainers)
            //    client.Client.SendPacket(new BattleOfferPacket { PlayerIDs = playerIDs, Message = Message });
        }

        Stopwatch UpdateWatch { get; } = Stopwatch.StartNew();
        public void Update()
        {
            // Stuff that is done every 0.5 second
            if (UpdateWatch.ElapsedMilliseconds < 500)
                return;

            UpdateWatch.Reset();
            UpdateWatch.Start();

            //if (Trainers.All(trainer => trainer.LastCommand != null))
            //    DoRound();
        }
        private void DoRound()
        {



            //foreach (var trainer in Trainers)
            //    trainer.LastCommand = null;
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

        public void Dispose()
        {

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
