using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Status;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient
    {
        AuthorizationStatus AuthorizationStatus = AuthorizationStatus.RemoteClientEnabled;
        bool CompressionEnabled => CompressionTreshold > 0;
        uint CompressionTreshold => Stream.CompressionThreshold;

        private void HandleAuthorizationRequest(AuthorizationRequestPacket packet)
        {
            SendPacket(new AuthorizationResponsePacket { AuthorizationStatus = AuthorizationStatus });

            if (AuthorizationStatus == AuthorizationStatus.RemoteClientEnabled)
                SendPacket(new AuthorizationCompletePacket());
            else
                SendPacket(new AuthorizationDisconnectPacket {Reason = "Remote Client not enabled!"});
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="packet"></param>
        private void HandleEncryptionRequest(EncryptionRequestPacket packet)
        {
            if ((AuthorizationStatus & AuthorizationStatus.EncryprionEnabled) != AuthorizationStatus.EncryprionEnabled)
                SendPacket(new AuthorizationDisconnectPacket {Reason = "Encryption not enabled!"});
            else
            {
                SendPacket(new EncryptionResponsePacket());

                var request = (EncryptionRequestPacket)packet;
                var sharedKey = PKCS1Signature.CreateSecretKey();

                //var hash = ;

                var pkcs = new PKCS1Signature(request.PublicKey);
                var signedSecret = pkcs.SignData(sharedKey);
                var signedVerify = pkcs.SignData(request.VerificationToken);

                SendPacket(new EncryptionResponsePacket
                {
                    SharedSecret = signedSecret,
                    VerificationToken = signedVerify
                });

                Stream.InitializeEncryption(sharedKey);
            }
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