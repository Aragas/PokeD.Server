using System;
using System.Collections.Generic;
using System.Globalization;

using Aragas.Network.Data;
using Aragas.Network.IO;
using Aragas.Network.Packets;

using PCLExt.Network;

using PokeD.Core.IO;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.VBA;
using PokeD.Server.Data;
using PokeD.Server.DatabaseData;

namespace PokeD.Server.Clients.VBA
{
    /// <summary>
    /// Visual Boy Advance v. 1.8.0 Server.
    /// </summary>
    public partial class VBAServerClient : Client
    {
        #region P3D Values

        public override int ID { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        public override string Name { get { throw new NotImplementedException(); } protected set { throw new NotImplementedException(); } }

        public override string LevelFile
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        public override Vector3 Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion P3D Values

        #region Values

        public override Prefix Prefix { get { throw new NotImplementedException(); } protected set { throw new NotImplementedException(); } }

        public override string PasswordHash { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public override string IP => Stream.Host;

        public override DateTime ConnectionTime { get; } = DateTime.Now;
        public override CultureInfo Language { get; }

        bool IsInitialized { get; set; }

        bool ChatEnabled { get; set; }

        #endregion Values

        VBAStream Stream { get; }

        ModuleVBA Module { get; }


#if DEBUG
        // -- Debug -- //
        List<StandardPacket> Received { get; } = new List<StandardPacket>();
        List<StandardPacket> Sended { get; } = new List<StandardPacket>();
        // -- Debug -- //
#endif


        public VBAServerClient(ITCPClient clientWrapper, ModuleVBA module)
        {
            Stream = new VBAStream(clientWrapper);
            Module = module;
        }


        public override void Update()
        {
            if (Stream.IsConnected)
            {
                if (!IsInitialized)
                {
                    Stream.SendPacket(new ConnectPacket { Field1 = 0x00, Field2 = 0x01 });
                    IsInitialized = true;
                }

                if (Stream.DataAvailable > 0)
                {
                    var len = Stream.ReadByte();
                    var data = Stream.Receive(len - 1);
                    HandleData(data);
                }
            }
            else
                Module.RemoveClient(this);
        }

        private void HandleData(byte[] data)
        {
            if (data != null)
            {
                using (var reader = new StandardDataReader(data))
                {
                    var id = reader.Read<byte>();

                    Func<VBAPacket> func;
                    if (VBAPacketResponses.TryGetPacketFunc(id, out func))
                    {
                        if (func != null)
                        {
                            var packet = func().ReadPacket(reader);
                            if (packet != null)
                            {
                                HandlePacket(packet);

#if DEBUG
                                Received.Add(packet);
#endif
                            }
                            else
                                Logger.Log(LogType.Error, $"VBA Reading Error: packet is null. Packet ID {id}");
                        }
                        else
                            Logger.Log(LogType.Error, $"VBA Reading Error: VBAPacketResponses.Packets[{id}] is null.");
                    }
                    else
                    {
                        Logger.Log(LogType.Error, $"VBA Reading Error: Packet ID {id} is not correct, Packet Data: {data}. Disconnecting.");
                        Module.RemoveClient(this, $"Packet ID {id} is not correct!");
                    }
                }
            }
            else
                Logger.Log(LogType.Error, $"VBA Reading Error: Packet Data is null.");
        }
        private void HandlePacket(StandardPacket packet)
        {
            switch ((VBAPacketTypes) packet.ID)
            {
                case VBAPacketTypes.Disconnect:
                    Module.RemoveClient(this);
                    break;


                case VBAPacketTypes.E1:
                    break;


                case VBAPacketTypes.E4:
                    break;
            }
        }


        public override GameDataPacket GetDataPacket() { throw new NotSupportedException(); }

        public override void SendPacket(Packet packet)
        {
            Stream.SendPacket(packet);
        }
        public override void SendMessage(string text) { }

        public override void LoadFromDB(Player data) { }


        public override void Dispose()
        {
            Stream.Disconnect();
            Stream.Dispose();
        }
    }
}
