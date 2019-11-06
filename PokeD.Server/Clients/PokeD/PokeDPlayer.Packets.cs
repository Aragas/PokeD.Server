using PokeD.Core.Extensions;
using PokeD.Core.Packets.PokeD.Authorization;
using PokeD.Core.Packets.PokeD.Battle;
using PokeD.Core.Packets.PokeD.Chat;
using PokeD.Core.Packets.PokeD.Overworld;
using PokeD.Core.Packets.PokeD.Overworld.Map;
using PokeD.Core.Packets.PokeD.Trade;
using PokeD.Server.Chat;

namespace PokeD.Server.Clients.PokeD
{
    public partial class PokeDPlayer
    {
        private AuthorizationStatus AuthorizationStatus => Module.EncryptionEnabled ? AuthorizationStatus.EncryprionEnabled : 0;
        private byte[] VerificationToken { get; set; }

        private void HandleAuthorizationRequest(AuthorizationRequestPacket packet)
        {
            if (IsInitialized)
                return;

            //PlayerRef = new Trainer(packet.Name);

            SendPacket(new AuthorizationResponsePacket { AuthorizationStatus = AuthorizationStatus });

            if ((AuthorizationStatus & AuthorizationStatus.EncryprionEnabled) != 0)
            {
                /*
                var publicKey = Module.Security.RSAKeyPair.PublicKeyToByteArray();

                VerificationToken = new byte[4];
                var drg = new DigestRandomGenerator(new Sha512Digest());
                drg.NextBytes(VerificationToken);

                SendPacket(new EncryptionRequestPacket {PublicKey = publicKey, VerificationToken = VerificationToken});
                */
            }
            else
            {
                if (!IsInitialized)
                {
                    Join();
                    IsInitialized = true;
                }
            }
        }
        private void HandleEncryptionResponse(EncryptionResponsePacket packet)
        {
            if (IsInitialized)
                return;

            if ((AuthorizationStatus & AuthorizationStatus.EncryprionEnabled) != 0)
            {
                /*
                var pkcs = new PKCS1Signer(Module.Security.RSAKeyPair);

                var decryptedToken = pkcs.DeSignData(packet.VerificationToken);
                for (int i = 0; i < VerificationToken.Length; i++)
                    if (decryptedToken[i] != VerificationToken[i])
                    {
                        SendPacket(new AuthorizationDisconnectPacket { Reason = "Unable to authenticate." });
                        return;
                    }
                Array.Clear(VerificationToken, 0, VerificationToken.Length);

                var sharedKey = pkcs.DeSignData(packet.SharedSecret);

                // TODO
                //Stream.InitializeEncryption(sharedKey);
                Join();
                IsInitialized = true;
                */
            }
            else
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Encryption not enabled!" });
        }


        private void HandlePosition(PositionPacket packet)
        {
            //PlayerRef.Position = packet.Position;

            Module.OnPosition(this);
        }
        private void HandleTrainerInfo(TrainerInfoPacket packet) { }

        private void HandleTileSetRequest(TileSetRequestPacket packet) { Module.PokeDTileSetRequest(this, packet.TileSetNames); }

        private void HandleChatServerMessage(ChatServerMessagePacket packet) { }
        private void HandleChatGlobalMessage(ChatGlobalMessagePacket packet)
        {
            if (packet.Message.StartsWith("/"))
            {
                if (!packet.Message.StartsWith("/login", System.StringComparison.OrdinalIgnoreCase))
                    SendPacket(new ChatGlobalMessagePacket { Message = packet.Message });

                ExecuteCommand(packet.Message);
            }
            else if (IsInitialized)
                Module.OnClientChatMessage(new ChatMessage(this, packet.Message));
        }
        private void HandleChatPrivateMessage(ChatPrivateMessagePacket packet)
        {
            var destClient = Module.GetClient(packet.PlayerID);
            if (destClient != null)
            {
                destClient.SendPrivateMessage(new ChatMessage(this, packet.Message));
                SendPacket(new ChatPrivateMessagePacket { Message = packet.Message });
            }
            else
                SendPacket(new ChatGlobalMessagePacket { Message = "The player doesn't exist." });
        }

        private void HandleBattleRequest(BattleRequestPacket packet){ }
        private void HandleBattleAccept(BattleAcceptPacket packet) { }
        private void HandleBattleAttack(BattleAttackPacket packet) { }
        private void HandleBattleItem(BattleItemPacket packet) { }
        private void HandleBattleSwitch(BattleSwitchPacket packet) { }
        private void HandleBattleFlee(BattleFleePacket packet) { }


        private void HandleTradeOffer(TradeOfferPacket packet) => Module.OnTradeRequest(this, packet.MonsterData.ToDataItems(), Module.GetClient(packet.DestinationID));
        private void HandleTradeAccept(TradeAcceptPacket packet) => Module.OnTradeConfirm(this, Module.GetClient(packet.DestinationID));
        private void HandleTradeRefuse(TradeRefusePacket packet) => Module.OnTradeCancel(this, Module.GetClient(packet.DestinationID));
    }
}