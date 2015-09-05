using PokeD.Core.Packets.Remote.Authorization;

namespace PokeD.Server.Data
{
    public partial class RemoteClient
    {
        AuthorizationStatus AuthorizationStatus = AuthorizationStatus.RemoteClientEnabled;
        bool CompressionEnabled => CompressionTreshold > 0;
        uint CompressionTreshold => Stream.CompressionThreshold;

        private void HandleAuthorizationRequest(AuthorizationRequestPacket packet)
        {
            SendPacket(new AuthorizationResponsePacket { AuthorizationStatus = AuthorizationStatus });

            if(AuthorizationStatus == AuthorizationStatus.RemoteClientEnabled)
                SendPacket(new AuthorizationCompletePacket());
            else
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Remote Client not enabled!"});
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

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="packet"></param>
        private void HandleCompressionRequest(CompressionRequestPacket packet)
        {
            if ((AuthorizationStatus & AuthorizationStatus.CompressionEnabled) != AuthorizationStatus.CompressionEnabled)
                SendPacket(new AuthorizationDisconnectPacket {Reason = "Compression not enabled!"});
            else
            {
                if (packet.Threshold > 0)
                {
                    if (packet.Threshold <= CompressionTreshold)
                    {
                        SendPacket(new CompressionResponsePacket {Threshold = packet.Threshold});

                        Stream.SetCompression(packet.Threshold);
                    }
                    else
                        SendPacket(new AuthorizationDisconnectPacket {Reason = "Compression threshold too big!"});
                }
                else
                    SendPacket(new CompressionResponsePacket {Threshold = packet.Threshold});
            }
        }
    }
}