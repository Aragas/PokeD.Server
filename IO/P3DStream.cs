using System;
using System.Globalization;
using System.Numerics;
using System.Text;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.IO;
using PokeD.Core.Wrappers;

namespace PokeD.Server.IO
{
    public sealed class P3DStream : IPokeStream
    {
        public bool Connected => _tcp != null && _tcp.Connected;
        public int DataAvailable => _tcp?.DataAvailable ?? 0;

        public bool EncryptionEnabled => false;
        public uint CompressionThreshold => 0;


        private readonly INetworkTCPClient _tcp;


        public P3DStream(INetworkTCPClient tcp)
        {
            _tcp = tcp;
        }


        public void InitializeEncryption(byte[] key)
        {
            throw new NotSupportedException();
        }

        public void SetCompression(uint threshold)
        {
            throw new NotSupportedException();
        }


        public void Connect(string ip, ushort port)
        {
            _tcp.Connect(ip, port);
        }

        public void Disconnect()
        {
            _tcp.Disconnect();
        }


        #region Vars

        // -- String

        public void WriteString(string value, int length = 0)
        {
            throw new NotSupportedException();
        }

        // -- VarInt

        public void WriteVarInt(VarInt value)
        {
            throw new NotSupportedException();
        }

        // -- Boolean

        public void WriteBoolean(bool value)
        {
            throw new NotSupportedException();
        }

        // -- SByte & Byte

        public void WriteSByte(sbyte value)
        {
            throw new NotSupportedException();
        }

        public void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }

        // -- Short & UShort

        public void WriteShort(short value)
        {
            throw new NotSupportedException();
        }

        public void WriteUShort(ushort value)
        {
            throw new NotSupportedException();
        }

        // -- Int & UInt

        public void WriteInt(int value)
        {
            throw new NotSupportedException();
        }

        public void WriteUInt(uint value)
        {
            throw new NotSupportedException();
        }

        // -- Long & ULong

        public void WriteLong(long value)
        {
            throw new NotSupportedException();
        }

        public void WriteULong(ulong value)
        {
            throw new NotSupportedException();
        }

        // -- BigInt & UBigInt

        public void WriteBigInteger(BigInteger value)
        {
            throw new NotSupportedException();
        }

        public void WriteUBigInteger(BigInteger value)
        {
            throw new NotSupportedException();
        }

        // -- Float

        public void WriteFloat(float value)
        {
            throw new NotSupportedException();
        }

        // -- Double

        public void WriteDouble(double value)
        {
            throw new NotSupportedException();
        }


        // -- StringArray

        public void WriteStringArray(string[] value)
        {
            throw new NotSupportedException();
        }

        // -- VarIntArray

        public void WriteVarIntArray(int[] value)
        {
            throw new NotSupportedException();
        }

        // -- IntArray

        public void WriteIntArray(int[] value)
        {
            throw new NotSupportedException();
        }

        // -- ByteArray

        public void WriteByteArray(byte[] value)
        {
            throw new NotSupportedException();
        }

        #endregion Vars


        // -- Read methods

        public byte ReadByte()
        {
            throw new NotSupportedException();
        }

        public VarInt ReadVarInt()
        {
            throw new NotSupportedException();
        }

        public byte[] ReadByteArray(int value)
        {
            throw new NotSupportedException();
        }

        public string ReadLine()
        {
            return _tcp.ReadLine();
        }

        // -- Read methods


        public void SendPacket(ref IPacket packet)
        {
            var str = CreateData(ref packet);
            var array = Encoding.UTF8.GetBytes(str + "\r\n");
            _tcp.Send(array, 0, array.Length);
        }


        private static string CreateData(ref IPacket packet)
        {
            var dataItems = packet.DataItems.ToList();

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(packet.ProtocolVersion.ToString(CultureInfo.InvariantCulture));
            stringBuilder.Append("|");
            stringBuilder.Append(packet.ID.ToString());
            stringBuilder.Append("|");
            stringBuilder.Append(packet.Origin.ToString());
            stringBuilder.Append("|");
            stringBuilder.Append(dataItems.Count.ToString());
            stringBuilder.Append("|0|");

            var num = 0;
            for (var i = 0; i < dataItems.Count - 1; i++)
            {
                num += dataItems[i].Length;
                stringBuilder.Append(num);
                stringBuilder.Append("|");
            }

            foreach (var dataItem in dataItems)
                stringBuilder.Append(dataItem);
            
            return stringBuilder.ToString();
        }


        public void Dispose()
        {
            if (_tcp != null)
            {
                _tcp.Disconnect();
                _tcp.Dispose();
            }
        }
    }
}
