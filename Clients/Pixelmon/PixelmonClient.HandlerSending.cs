/*
using MineLib.Core.Client;
using MineLib.Core.Data.Structs;
using MineLib.Core.Events.SendingEvents;

using PokeD.Core.Data.PokeD.Monster;

namespace PokeD.Server.Clients.Pixelmon
{
    public partial class PixelmonClient : MineLibClient
    {
        #region InnerSending

        public void ConnectToServer(string serverHost, ushort port, string username) { ProtocolHandler.FireEvent(new ConnectToServer(serverHost, port, username)); }

        public void CreatePokemon(Monster monster)
        {
            string command = $"/pokegive {Username} {monster.StaticData.Name} ab:{monster.Ability.Name} ba:Ultra ge:{monster.Gender} gr:Ordinary l:{monster.Level} n:";
        }

        public void KeepAlive(int value) { ProtocolHandler.FireEvent(new KeepAliveEvent(value)); }

        public void SendClientInfo() { ProtocolHandler.FireEvent(new SendClientInfoEvent()); }

        public void Respawn() { ProtocolHandler.FireEvent(new RespawnEvent()); }

        public void PlayerMoved(IPlaverMovedData data) { ProtocolHandler.FireEvent(new PlayerMovedEvent(data)); }

        public void PlayerMoved(PlaverMovedMode mode, IPlaverMovedData data) { ProtocolHandler.FireEvent(new PlayerMovedEvent(mode, data)); }

        public void PlayerSetRemoveBlock(PlayerSetRemoveBlockMode mode, IPlayerSetRemoveBlockData data) { ProtocolHandler.FireEvent(new PlayerSetRemoveBlockEvent(mode, data)); }

        public void PlayerSetRemoveBlock(IPlayerSetRemoveBlockData data) { ProtocolHandler.FireEvent(new PlayerSetRemoveBlockEvent(data)); }

        public void SendMessage(string message) { ProtocolHandler.FireEvent(new SendMessageEvent(message)); }

        public void PlayerHeldItem(short slot) { ProtocolHandler.FireEvent(new PlayerHeldItemEvent(slot)); }

        #endregion InnerSending
    }
}
*/