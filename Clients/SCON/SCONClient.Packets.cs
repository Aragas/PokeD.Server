using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private void HandleLogListRequest(LogListRequestPacket packet)
        {
            var list = FileSystemWrapper.LogFolder.GetFilesAsync().Result;

            var strings = new List<string>();
            foreach (var file in list)
                strings.Add(file.Name);
            
            SendPacket(new LogListResponsePacket { LogFileList = strings.ToArray() });
        }

        private void HandleLogFileRequest(LogFileRequestPacket packet)
        {
            if (FileSystemWrapper.LogFolder.CheckExistsAsync(packet.LogFilename).Result == ExistenceCheckResult.FileExists)
            {
                var logText = FileSystemWrapper.LogFolder.GetFileAsync(packet.LogFilename).Result.ReadAllTextAsync().Result;

                SendPacket(new LogFileResponsePacket { LogFile = logText });
            }
        }

        private void HandleCrashLogListRequest(CrashLogListRequestPacket packet)
        {
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
            if (FileSystemWrapper.LogFolder.CheckExistsAsync("Crash").Result == ExistenceCheckResult.FolderExists)
                if (FileSystemWrapper.LogFolder.GetFolderAsync("Crash").Result.CheckExistsAsync(packet.CrashLogFilename).Result == ExistenceCheckResult.FileExists)
                {
                    var crashLogText = FileSystemWrapper.LogFolder.GetFileAsync(packet.CrashLogFilename).Result.ReadAllTextAsync().Result;

                    SendPacket(new CrashLogFileResponsePacket {CrashLogFile = crashLogText});
                }
        }
    }
}