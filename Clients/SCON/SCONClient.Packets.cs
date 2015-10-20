using System;
using System.Collections.Generic;
using System.IO;
using Aragas.Core.Wrappers;

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Prng;

using PCLStorage;

using PokeD.Core;
using PokeD.Core.Data.Structs;
using PokeD.Core.Extensions;
using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;
using PokeD.Core.Packets.SCON.Logs;
using PokeD.Core.Packets.SCON.Lua;
using PokeD.Core.Packets.SCON.Status;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient
    {
        AuthorizationStatus AuthorizationStatus { get; set; }

        byte[] VerificationToken { get; set; }
        bool Authorized { get; set; }

        private void HandleAuthorizationRequest(AuthorizationRequestPacket packet)
        {
            if (Authorized)
                return;

            SendPacket(new AuthorizationResponsePacket { AuthorizationStatus = AuthorizationStatus });

            if (AuthorizationStatus.HasFlag(AuthorizationStatus.EncryprionEnabled))
            {
                var publicKey = _server.RSAKeyPair.PublicKeyToByteArray();

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

            if (AuthorizationStatus.HasFlag(AuthorizationStatus.RemoteClientEnabled))
            {
                if (AuthorizationStatus.HasFlag(AuthorizationStatus.EncryprionEnabled))
                {                 
                    var pkcs = new PKCS1Signer(_server.RSAKeyPair);

                    var decryptedToken = pkcs.DeSignData(packet.VerificationToken);
                    for (int i = 0; i < VerificationToken.Length; i++)
                        if (decryptedToken[i] != VerificationToken[i])
                        {
                            SendPacket(new AuthorizationDisconnectPacket {Reason = "Unable to authenticate."});
                            return;
                        }
                    Array.Clear(VerificationToken, 0, VerificationToken.Length);

                    var sharedKey = pkcs.DeSignData(packet.SharedSecret);

                    Stream.InitializeEncryption(sharedKey);
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
                if (_server.SCON_Password.Hash == packet.PasswordHash)
                {
                    Authorized = true;
                    SendPacket(new AuthorizationCompletePacket());

                    IsInitialized = true;
                    _server.AddPlayer(this);
                }
                else
                    SendPacket(new AuthorizationDisconnectPacket { Reason = "Password not correct!" });
            }
            else
                SendPacket(new AuthorizationDisconnectPacket {Reason = "Remote Client not enabled!"});
        }

        private void HandleExecuteCommand(ExecuteCommandPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            _server.ExecuteCommand(packet.Command);
        }

        private void HandleStartChatReceiving(StartChatReceivingPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            ChatReceiving = true;
        }
        private void HandleStopChatReceiving(StopChatReceivingPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            ChatReceiving = false;
        }

        private void HandlePlayerInfoListRequest(PlayerInfoListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            SendPacket(new PlayerInfoListResponsePacket { PlayerInfoList = new PlayerInfoList(_server.GetAllClientsInfo()) });
        }

        private void HandleLogListRequest(LogListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            var list = FileSystemWrapper.LogFolder.GetFilesAsync().Result;

            var logs = new List<Log>();
            foreach (var file in list)
                logs.Add(new Log { LogFileName = file.Name });
            
            SendPacket(new LogListResponsePacket { LogList = new LogList(logs.ToArray()) });
        }
        private void HandleLogFileRequest(LogFileRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            if (FileSystemWrapper.LogFolder.CheckExistsAsync(packet.LogFilename).Result == ExistenceCheckResult.FileExists)
                using (var reader = new StreamReader(FileSystemWrapper.LogFolder.GetFileAsync(packet.LogFilename).Result.OpenAsync(FileAccess.Read).Result))
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

            var list = FileSystemWrapper.CrashLogFolder.GetFilesAsync().Result;

            var crashLogs = new List<Log>();
            foreach (var file in list)
                crashLogs.Add(new Log {LogFileName = file.Name});

            SendPacket(new CrashLogListResponsePacket {CrashLogList = new LogList(crashLogs.ToArray())});
        }

        private void HandleCrashLogFileRequest(CrashLogFileRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket {Reason = "Not authorized!"});
                return;
            }

            if (FileSystemWrapper.CrashLogFolder.CheckExistsAsync(packet.CrashLogFilename).Result == ExistenceCheckResult.FileExists)
                using (var reader = new StreamReader(FileSystemWrapper.CrashLogFolder.GetFileAsync(packet.CrashLogFilename).Result.OpenAsync(FileAccess.Read).Result))
                {
                    var logText = reader.ReadToEnd();
                    SendPacket(new CrashLogFileResponsePacket {CrashLogFilename = packet.CrashLogFilename, CrashLogFile = logText});
                }
        }

        private void HandlePlayerDatabaseListRequest(PlayerDatabaseListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            SendPacket(new PlayerDatabaseListResponsePacket { PlayerDatabaseList = new PlayerDatabaseList() });
        }

        private void HandleBanListRequest(BanListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            SendPacket(new BanListResponsePacket { BanList = new BanList() });
        }

        private void HandleUploadLuaToServer(UploadLuaToServerPacket packet)
        {
            
        }

        private void HandleReloadNPCs(ReloadNPCsPacket packet)
        {
            _server.ReloadNPCs();
        }
    }
}