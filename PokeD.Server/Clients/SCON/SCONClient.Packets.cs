/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Aragas.Network.IO;

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Prng;

using PCLExt.FileStorage;

using PokeD.Core.Data.SCON;
using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;
using PokeD.Core.Packets.SCON.Logs;
using PokeD.Core.Packets.SCON.Script;
using PokeD.Core.Packets.SCON.Status;
using PokeD.Server.Extensions;
using PokeD.Server.IO;
using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient
    {
        private AuthorizationStatus AuthorizationStatus { get; }

        private byte[] VerificationToken { get; set; }
        private bool Authorized { get; set; }

        private void HandleAuthorizationRequest(AuthorizationRequestPacket packet)
        {
            if (Authorized)
                return;

            SendPacket(new AuthorizationResponsePacket { AuthorizationStatus = AuthorizationStatus });

            if (AuthorizationStatus.HasFlag(AuthorizationStatus.EncryprionEnabled))
            {
                var publicKey = Module.Security.RSAKeyPair.PublicKeyToByteArray();

                VerificationToken = new byte[4];
                var drg = new DigestRandomGenerator(new Sha512Digest());
                drg.NextBytes(VerificationToken);

                SendPacket(new EncryptionRequestPacket { PublicKey = publicKey, VerificationToken = VerificationToken });
            }
        }
        private void HandleEncryptionResponse(EncryptionResponsePacket packet)
        {
            if (Authorized)
                return;

            if (AuthorizationStatus.HasFlag(AuthorizationStatus.EncryprionEnabled))
            {
                var pkcs = new PKCS1Signer(Module.Security.RSAKeyPair);

                var decryptedToken = pkcs.DeSignData(packet.VerificationToken);
                for (var i = 0; i < VerificationToken.Length; i++)
                    if (decryptedToken[i] != VerificationToken[i])
                    {
                        SendPacket(new AuthorizationDisconnectPacket { Reason = "Unable to authenticate." });
                        return;
                    }
                Array.Clear(VerificationToken, 0, VerificationToken.Length);

                var sharedKey = pkcs.DeSignData(packet.SharedSecret);

                Stream = new ProtobufTransmission<SCONPacket>(Socket, new BouncyCastleAesStream(Socket, sharedKey));
            }
            else
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Encryption not enabled!" });
        }
        private void HandleAuthorizationPassword(AuthorizationPasswordPacket packet)
        {
            if (Authorized)
                return;

            if (Module.SCONPassword.Hash == packet.PasswordHash)
            {
                Authorized = true;
                SendPacket(new AuthorizationCompletePacket());

                Join();
                IsInitialized = true;
            }
            else
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Password not correct!" });
        }

        private void HandleExecuteCommand(ExecuteCommandPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            Module.ExecuteClientCommand(this, packet.Command);
        }

        private void HandleChatReceivePacket(ChatReceivePacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            ChatEnabled = packet.Enabled;
        }

        private void HandlePlayerInfoListRequest(PlayerInfoListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            SendPacket(new PlayerInfoListResponsePacket { PlayerInfos = Module.AllClientsSelect(clients => clients.ClientInfos().ToList()).ToArray() });
        }

        private void HandleLogListRequest(LogListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            var list = new LogsFolder().GetFiles();

            var logs = new List<Log>();
            foreach (var file in list)
                logs.Add(new Log { LogFileName = file.Name });

            SendPacket(new LogListResponsePacket { Logs = logs.ToArray() });
        }
        private void HandleLogFileRequest(LogFileRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            if (new LogsFolder().CheckExists(packet.LogFilename) == ExistenceCheckResult.FileExists)
                using (var reader = new StreamReader(new LogsFolder().GetFile(packet.LogFilename).Open(PCLExt.FileStorage.FileAccess.Read)))
                {
                    var logText = reader.ReadToEnd();
                    SendPacket(new LogFileResponsePacket { LogFilename = packet.LogFilename, LogFile = logText });
                }
        }

        private void HandleCrashLogListRequest(CrashLogListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket {Reason = "Not authorized!"});
                return;
            }

            var list = new CrashLogsFolder().GetFiles();

            var crashLogs = new List<Log>();
            foreach (var file in list)
                crashLogs.Add(new Log { LogFileName = file.Name });

            SendPacket(new CrashLogListResponsePacket { CrashLogs = crashLogs.ToArray() });
        }
        private void HandleCrashLogFileRequest(CrashLogFileRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            if (new CrashLogsFolder().CheckExists(packet.CrashLogFilename) == ExistenceCheckResult.FileExists)
                using (var reader = new StreamReader(new CrashLogsFolder().GetFile(packet.CrashLogFilename).Open(PCLExt.FileStorage.FileAccess.Read)))
                {
                    var logText = reader.ReadToEnd();
                    SendPacket(new CrashLogFileResponsePacket { CrashLogFilename = packet.CrashLogFilename, CrashLogFile = logText });
                }
        }

        private void HandlePlayerDatabaseListRequest(PlayerDatabaseListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            SendPacket(new PlayerDatabaseListResponsePacket { PlayerDatabases = new PlayerDatabase[0] });
        }

        private void HandleBanListRequest(BanListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            SendPacket(new BanListResponsePacket { Bans = new Ban[0] });
        }

        private void HandleUploadLuaToServer(UploadScriptToServerPacket packet)
        {

        }
        private void HandleReloadNPCs(ReloadScriptPacket packet)
        {
            //Module.Server.ReloadNPCs();
        }
    }
}
*/