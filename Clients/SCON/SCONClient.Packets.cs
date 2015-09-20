using System.Collections.Generic;

using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

using PCLStorage;

using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;
using PokeD.Core.Packets.SCON.Logs;
using PokeD.Core.Packets.SCON.Status;
using PokeD.Core.Wrappers;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient
    {
        AuthorizationStatus AuthorizationStatus { get; set; } = AuthorizationStatus.RemoteClientEnabled | AuthorizationStatus.EncryprionEnabled;

        byte[] VerificationToken { get; set; }
        bool Authorized { get; set; }

        private void HandleAuthorizationRequest(AuthorizationRequestPacket packet)
        {
            if (Authorized)
                return;

            SendPacket(new AuthorizationResponsePacket { AuthorizationStatus = AuthorizationStatus });

            if (AuthorizationStatus.HasFlag(AuthorizationStatus.EncryprionEnabled))
            {
                var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(_server.RSAKeyPair.Public);
                var publicKey = publicKeyInfo.ToAsn1Object().GetDerEncoded();

                VerificationToken = new byte[4];
                var csp = new SecureRandom();
                csp.NextBytes(VerificationToken);

                SendPacket(new EncryptionRequestPacket { PublicKey = publicKey, VerificationToken = VerificationToken });
            }
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="packet"></param>
        private void HandleEncryptionResponse(EncryptionResponsePacket packet)
        {
            if (Authorized)
                return;

            if (AuthorizationStatus.HasFlag(AuthorizationStatus.RemoteClientEnabled))
            {
                if (AuthorizationStatus.HasFlag(AuthorizationStatus.EncryprionEnabled))
                {                 
                    var pkcs = new PKCS1Signature(_server.RSAKeyPair);

                    var decryptedToken = pkcs.DeSignData(packet.VerificationToken);
                    for (int i = 0; i < decryptedToken.Length; i++)
                        if (decryptedToken[i] != VerificationToken[i])
                        {
                            SendPacket(new AuthorizationDisconnectPacket { Reason = "Unable to authenticate." });
                            return;
                        }
                    
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
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            _server.ExecuteCommand(packet.Command);
        }

        private void HandlePlayerListRequest(PlayerListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            SendPacket(new PlayerListResponsePacket {Players = _server.GetConnectedClientNames()});
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

        private void HandlePlayerLocationRequest(PlayerLocationRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            var player = _server.GetClient(packet.Player);

            SendPacket(new PlayerLocationResponsePacket { Player = player.Name, Position = player.Position, LevelFile = player.LevelFile });
        }

        private void HandleLogListRequest(LogListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            var list = FileSystemWrapper.LogFolder.GetFilesAsync().Result;

            var strings = new List<string>();
            foreach (var file in list)
                strings.Add(file.Name);
            
            SendPacket(new LogListResponsePacket { LogFileList = strings.ToArray() });
        }

        private void HandleLogFileRequest(LogFileRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            if (FileSystemWrapper.LogFolder.CheckExistsAsync(packet.LogFilename).Result == ExistenceCheckResult.FileExists)
            {
                var logText = FileSystemWrapper.LogFolder.GetFileAsync(packet.LogFilename).Result.ReadAllTextAsync().Result;

                SendPacket(new LogFileResponsePacket { LogFile = logText });
            }
        }

        private void HandleCrashLogListRequest(CrashLogListRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket { Reason = "Not authorized!" });
                return;
            }

            if (FileSystemWrapper.LogFolder.CheckExistsAsync("Crash").Result == ExistenceCheckResult.FolderExists)
            {
                var list = FileSystemWrapper.LogFolder.GetFolderAsync("Crash").Result.GetFilesAsync().Result;

                var strings = new List<string>();
                foreach (var file in list)
                    strings.Add(file.Name);

                SendPacket(new CrashLogListResponsePacket {CrashLogFileList = strings.ToArray()});
            }
        }

        private void HandleCrashLogFileRequest(CrashLogFileRequestPacket packet)
        {
            if (!Authorized)
            {
                SendPacket(new AuthorizationDisconnectPacket {Reason = "Not authorized!"});
                return;
            }

            if (FileSystemWrapper.LogFolder.CheckExistsAsync("Crash").Result == ExistenceCheckResult.FolderExists)
                if (FileSystemWrapper.LogFolder.GetFolderAsync("Crash").Result.CheckExistsAsync(packet.CrashLogFilename).Result == ExistenceCheckResult.FileExists)
                {
                    var crashLogText = FileSystemWrapper.LogFolder.GetFolderAsync("Crash").Result.GetFileAsync(packet.CrashLogFilename).Result.ReadAllTextAsync().Result;

                    SendPacket(new CrashLogFileResponsePacket {CrashLogFile = crashLogText});
                }
        }
    }
}