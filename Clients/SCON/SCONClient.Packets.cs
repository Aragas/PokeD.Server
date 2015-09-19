using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;
using PokeD.Core.Packets.SCON.Status;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient
    {
        AuthorizationStatus AuthorizationStatus { get; set; } = AuthorizationStatus.RemoteClientEnabled;

        bool Authorized { get; set; }

        private void HandleAuthorizationRequest(AuthorizationRequestPacket packet)
        {
            if (Authorized)
                return;

            SendPacket(new AuthorizationResponsePacket { AuthorizationStatus = AuthorizationStatus });
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="packet"></param>
        private void HandleEncryptionRequest(EncryptionRequestPacket packet)
        {
            if (Authorized)
                return;

            if (AuthorizationStatus.HasFlag(AuthorizationStatus.RemoteClientEnabled))
            {
                if (AuthorizationStatus.HasFlag(AuthorizationStatus.EncryprionEnabled))
                {
                    SendPacket(new AuthorizationDisconnectPacket { Reason = "Encryption isn't working! Hahaha!!!" });


                }
                else
                    SendPacket(new AuthorizationDisconnectPacket { Reason = "Encryption not enabled!" });
            }
            else
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Remote Client not enabled!" });
        }

        private void HandleAuthorizationPassword(AuthorizationPasswordPacket packet)
        {
            if(Authorized)
                return;

            if (AuthorizationStatus.HasFlag(AuthorizationStatus.RemoteClientEnabled))
            {
                if (_server.SCON_Password == packet.Password)
                {
                    Authorized = true;
                    SendPacket(new AuthorizationCompletePacket());
                }
                else
                    SendPacket(new AuthorizationDisconnectPacket { Reason = "Password not correct!" });
            }
            else
                SendPacket(new AuthorizationDisconnectPacket {Reason = "Remote Client not enabled!"});
        }

        private void HandleExecuteCommand(ExecuteCommandPacket packet)
        {
            if(Authorized)
                _server.ExecuteCommand(packet.Command);
            else
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
        }

        private void HandlePlayerListRequest(PlayerListRequestPacket packet)
        {
            if (Authorized)
                SendPacket(new PlayerListResponsePacket { Players = _server.GetConnectedClientNames() });
            else
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
        }

        private void HandleStartChatReceiving(StartChatReceivingPacket packet)
        {
            ChatReceiving = true;
        }

        private void HandleStopChatReceiving(StopChatReceivingPacket packet)
        {
            ChatReceiving = false;
        }

        private void HandlePlayerLocationRequest(PlayerLocationRequestPacket packet)
        {
            var player = _server.GetClient(packet.Player);

            SendPacket(new PlayerLocationResponsePacket { Player = player.Name, Position = player.Position, LevelFile = player.LevelFile });
        }
    }
}