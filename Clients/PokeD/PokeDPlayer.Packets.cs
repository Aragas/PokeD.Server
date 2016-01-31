using System;

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Prng;

using PokeD.Core;
using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Data.PokeD.Trainer;
using PokeD.Core.Extensions;
using PokeD.Core.Packets.PokeD.Authorization;
using PokeD.Core.Packets.PokeD.Battle;
using PokeD.Core.Packets.PokeD.Chat;
using PokeD.Core.Packets.PokeD.Overworld.Map;
using PokeD.Core.Packets.PokeD.Overworld;
using PokeD.Core.Packets.PokeD.Trade;


namespace PokeD.Server.Clients.PokeD
{
    partial class PokeDPlayer
    {
        AuthorizationStatus AuthorizationStatus => Module.EncryptionEnabled ? AuthorizationStatus.EncryprionEnabled : 0;
        byte[] VerificationToken { get; set; }

        private void HandleAuthorizationRequest(AuthorizationRequestPacket packet)
        {
            if (IsInitialized)
                return;

            PlayerRef = new Trainer(packet.Name);

            SendPacket(new AuthorizationResponsePacket { AuthorizationStatus = AuthorizationStatus });

            if (AuthorizationStatus.HasFlag(AuthorizationStatus.EncryprionEnabled))
            {
                var publicKey = Module.Server.RSAKeyPair.PublicKeyToByteArray();

                VerificationToken = new byte[4];
                var drg = new DigestRandomGenerator(new Sha512Digest());
                drg.NextBytes(VerificationToken);

                SendPacket(new EncryptionRequestPacket {PublicKey = publicKey, VerificationToken = VerificationToken});
            }
            else
                Module.PreAdd(this); //Initialize();
        }
        private void HandleEncryptionResponse(EncryptionResponsePacket packet)
        {
            if (IsInitialized)
                return;

            if (AuthorizationStatus.HasFlag(AuthorizationStatus.EncryprionEnabled))
            {
                var pkcs = new PKCS1Signer(Module.Server.RSAKeyPair);

                var decryptedToken = pkcs.DeSignData(packet.VerificationToken);
                for (int i = 0; i < VerificationToken.Length; i++)
                    if (decryptedToken[i] != VerificationToken[i])
                    {
                        SendPacket(new AuthorizationDisconnectPacket { Reason = "Unable to authenticate." });
                        return;
                    }
                Array.Clear(VerificationToken, 0, VerificationToken.Length);

                var sharedKey = pkcs.DeSignData(packet.SharedSecret);

                Stream.InitializeEncryption(sharedKey);
                Module.PreAdd(this);
                //Initialize();
            }
            else
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Encryption not enabled!" });
        }


        private void HandlePosition(PositionPacket packet) { }
        private void HandleTrainerInfo(TrainerInfoPacket packet) { }
        private void HandleTileSetRequest(TileSetRequestPacket packet) { Module.PokeDTileSetRequest(this, packet.TileSetNames); }

        private void HandleChatServerMessage(ChatServerMessagePacket packet) { }
        private void HandleChatGlobalMessage(ChatGlobalMessagePacket packet)
        {
            if (packet.Message.StartsWith("/"))
            {
                if (!packet.Message.ToLower().StartsWith("/login"))
                    SendPacket(new ChatGlobalMessagePacket { Message = packet.Message }, ID);

                ExecuteCommand(packet.Message);
            }
            else if (IsInitialized)
            {
                Logger.LogChatMessage(Name, packet.Message);
                Module.SendGlobalMessage(this, packet.Message);
            }
        }
        private void HandleChatPrivateMessage(ChatPrivateMessagePacket packet)
        {
            var destClient = Module.Server.GetClient(packet.PlayerID);
            if (destClient != null)
            {
                Module.SendPrivateMessage(this, destClient, packet.Message);
                Module.PokeDPlayerSendToClient(this, new ChatPrivateMessagePacket { Message = packet.Message });
            }
            else
                SendPacket(new ChatGlobalMessagePacket { Message = $"The player with the name \"{destClient.Name}\" doesn't exist." }, -1);
        }


        private void HandleBattleRequest(BattleRequestPacket packet)
        {
            if(Battle == null)
                Battle = Module.CreateBattle(packet.PlayerIDs, packet.Message);
            else
                SendPacket(new BattleCancelledPacket {Reason = "You are already in battle!"});
        }
        private void HandleBattleAccept(BattleAcceptPacket packet) { if(packet.IsAccepted) Battle.AcceptBattle(this); else Battle.CancelBattle(this); }

        private void HandleBattleAttack(BattleAttackPacket packet) { Battle.HandlePacket(this, packet); }
        private void HandleBattleItem(BattleItemPacket packet) { Battle.HandlePacket(this, packet); }
        private void HandleBattleSwitch(BattleSwitchPacket packet) { Battle.HandlePacket(this, packet); }
        private void HandleBattleFlee(BattleFleePacket packet) { Battle.HandlePacket(this, packet); }


        private void HandleTradeOffer(TradeOfferPacket packet)
        {
            Module.SendTradeRequest(this, new Monster(packet.MonsterData), Module.Server.GetClient(packet.DestinationID));
        }
        private void HandleTradeAccept(TradeAcceptPacket packet)
        {
            Module.SendTradeConfirm(this, Module.Server.GetClient(packet.DestinationID));
        }
        private void HandleTradeRefuse(TradeRefusePacket packet)
        {
            Module.SendTradeCancel(this, Module.Server.GetClient(packet.DestinationID));
        }
    }
}
