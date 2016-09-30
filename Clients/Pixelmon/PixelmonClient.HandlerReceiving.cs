/*
using System;
using System.Collections.Generic;

using MineLib.Core.Client;
using MineLib.Core.Events;
using MineLib.Core.Events.ReceiveEvents;

namespace PokeD.Server.Clients.Pixelmon
{
    public partial class PixelmonClient : MineLibClient
    {
        private Dictionary<Type, List<Action<ReceiveEvent>>> ReceiveHandlers { get; } = new Dictionary<Type, List<Action<ReceiveEvent>>>();
        private static Action<ReceiveEvent> Transform<TReceiveEvent>(Action<TReceiveEvent> action) where TReceiveEvent : ReceiveEvent => receiveEvent => action((TReceiveEvent) receiveEvent);


        public override void RegisterReceiveEvent<TReceiveEvent>(Action<TReceiveEvent> func)
        {
            var receiveType = typeof(TReceiveEvent);

            if (ReceiveHandlers.ContainsKey(receiveType))
                ReceiveHandlers[receiveType].Add(Transform(func));
            else
                ReceiveHandlers.Add(receiveType, new List<Action<ReceiveEvent>> { Transform(func) });
        }
        public override void DeregisterReceiveEvent<TReceiveEvent>(Action<TReceiveEvent> func)
        {
            var receiveType = typeof(TReceiveEvent);

            if (ReceiveHandlers.ContainsKey(receiveType))
                ReceiveHandlers[receiveType].Remove(Transform(func));
        }

        public override void DoReceiveEvent<TReceiveEvent>(TReceiveEvent args)
        {
            var receiveType = args.GetType();

            if (ReceiveHandlers.ContainsKey(receiveType))
                foreach (var func in ReceiveHandlers[receiveType])
                    func(args);
        }

        private void RegisterSupportedReceiveEvents()
        {
            RegisterReceiveEvent<ChatMessageEvent>(OnChatMessage);

            RegisterReceiveEvent<PlayerPositionEvent>(OnPlayerPosition);
            RegisterReceiveEvent<PlayerLookEvent>(OnPlayerLook);
            RegisterReceiveEvent<HeldItemChangeEvent>(OnHeldItemChange);
            RegisterReceiveEvent<SpawnPointEvent>(OnSpawnPoint);
            RegisterReceiveEvent<UpdateHealthEvent>(OnUpdateHealth);
            RegisterReceiveEvent<RespawnEvent>(OnRespawn);
            RegisterReceiveEvent<ActionEvent>(OnAction);
            RegisterReceiveEvent<SetExperienceEvent>(OnSetExperience);

            RegisterReceiveEvent<TimeUpdateEvent>(OnTimeUpdate);

        }


        #region InnerReceiving

        private void OnChatMessage(ChatMessageEvent receiveEvent)
        {

        }


        private void OnPlayerPosition(PlayerPositionEvent receiveEvent)
        {
        }

        private void OnPlayerLook(PlayerLookEvent receiveEvent)
        {
        }

        private void OnHeldItemChange(HeldItemChangeEvent receiveEvent)
        {
        }

        private void OnSpawnPoint(SpawnPointEvent receiveEvent)
        {
        }

        private void OnUpdateHealth(UpdateHealthEvent receiveEvent)
        {
        }

        private void OnRespawn(RespawnEvent receiveEvent)
        {
        }

        private void OnAction(ActionEvent receiveEvent)
        {
        }

        private void OnSetExperience(SetExperienceEvent receiveEvent)
        {
        }

        private void OnTimeUpdate(TimeUpdateEvent receiveEvent)
        {
        }

        #endregion InnerReceiving
    }
}
*/