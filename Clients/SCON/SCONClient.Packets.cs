using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Status;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient
    {
        AuthorizationStatus AuthorizationStatus { get; set; } = AuthorizationStatus.RemoteClientEnabled;

        private void HandleAuthorizationRequest(AuthorizationRequestPacket packet)
        {
            SendPacket(new AuthorizationResponsePacket { AuthorizationStatus = AuthorizationStatus });
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="packet"></param>
        private void HandleEncryptionRequest(EncryptionRequestPacket packet)
        {
            if ((AuthorizationStatus & AuthorizationStatus.EncryprionEnabled) != AuthorizationStatus.EncryprionEnabled)
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Encryption not enabled!"} );
            else
            {

            }
        }

        private void HandleAuthorizationPassword(AuthorizationPasswordPacket packet)
        {
            if (AuthorizationStatus == AuthorizationStatus.RemoteClientEnabled)
            {
                if (_server.SCON_Password == packet.Password)
                    SendPacket(new AuthorizationCompletePacket());
                else
                    SendPacket(new AuthorizationDisconnectPacket { Reason = "Password not correct!" });
            }
            else
                SendPacket(new AuthorizationDisconnectPacket {Reason = "Remote Client not enabled!"});
        }

        private void HandleExecuteCommand(ExecuteCommandPacket packet)
        {
            _server.ExecuteCommand(packet.Command);
        }

        private void HandlePlayerListRequest(PlayerListRequestPacket packet)
        {
            SendPacket(new PlayerListResponsePacket { Players = _server.GetConnectedClientNames() });
        }
    }
}