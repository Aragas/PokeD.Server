using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.IO;
using PokeD.Core.Wrappers;

using PokeD.Server.Exceptions;

namespace PokeD.Server.IO
{
    public sealed class PlayerStream : IPokeStream
    {
        public bool Connected { get { return _tcp != null && _tcp.Connected; } }
        public int DataAvailable { get { return _tcp != null ? _tcp.DataAvailable : 0; } }

        public bool EncryptionEnabled { get { return false; } }
        public uint CompressionThreshold { get { return 0; } }



        private readonly INetworkTCPClient _tcp;

        private byte[] _buffer;
        private Encoding _encoding = Encoding.UTF8;

        public PlayerStream(INetworkTCPClient tcp)
        {
            _tcp = tcp;
        }


        public void InitializeEncryption(byte[] key)
        {
            throw new NotImplementedException();
        }

        public void SetCompression(uint threshold)
        {
            throw new NotImplementedException();
        }


        public void Connect(string ip, ushort port)
        {
            _tcp.Connect(ip, port);
        }

        public void Disconnect()
        {
            _tcp.Disconnect();
        }

        public Task ConnectAsync(string ip, ushort port)
        {
            return _tcp.ConnectAsync(ip, port);
        }

        public bool DisconnectAsync()
        {
            return _tcp.DisconnectAsync();
        }


        #region Vars

        // -- String

        public void WriteString(string value, int length = 0)
        {
            var lengthBytes = GetVarIntBytes(value.Length);
            var final = new byte[value.Length + lengthBytes.Length];

            Buffer.BlockCopy(lengthBytes, 0, final, 0, lengthBytes.Length);
            Buffer.BlockCopy(_encoding.GetBytes(value), 0, final, lengthBytes.Length, value.Length);

            WriteByteArray(final);
        }

        // -- VarInt

        public void WriteVarInt(VarInt value)
        {
            WriteByteArray(GetVarIntBytes(value));
        }

        // BUG: Is broken?
        public static byte[] GetVarIntBytes(int _value)
        {
            uint value = (uint) _value;

            var bytes = new List<byte>();
            while (true)
            {
                if ((value & 0xFFFFFF80u) == 0)
                {
                    bytes.Add((byte) value);
                    break;
                }
                bytes.Add((byte) (value & 0x7F | 0x80));
                value >>= 7;
            }

            return bytes.ToArray();
        }

        // -- Boolean

        public void WriteBoolean(bool value)
        {
            WriteByte(Convert.ToByte(value));
        }

        // -- SByte & Byte

        public void WriteSByte(sbyte value)
        {
            WriteByte(unchecked((byte) value));
        }

        public void WriteByte(byte value)
        {
            if (_buffer != null)
            {
                var tempBuff = new byte[_buffer.Length + 1];

                Buffer.BlockCopy(_buffer, 0, tempBuff, 0, _buffer.Length);
                tempBuff[_buffer.Length] = value;

                _buffer = tempBuff;
            }
            else
                _buffer = new byte[] {value};
        }

        // -- Short & UShort

        public void WriteShort(short value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            WriteByteArray(bytes);
        }

        public void WriteUShort(ushort value)
        {
            WriteByteArray(new byte[]
            {
                (byte) ((value & 0xFF00) >> 8),
                (byte) (value & 0xFF)
            });
        }

        // -- Int & UInt

        public void WriteInt(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            WriteByteArray(bytes);
        }

        public void WriteUInt(uint value)
        {
            WriteByteArray(new[]
            {
                (byte) ((value & 0xFF000000) >> 24),
                (byte) ((value & 0xFF0000) >> 16),
                (byte) ((value & 0xFF00) >> 8),
                (byte) (value & 0xFF)
            });
        }

        // -- Long & ULong

        public void WriteLong(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            WriteByteArray(bytes);
        }

        public void WriteULong(ulong value)
        {
            WriteByteArray(new[]
            {
                (byte) ((value & 0xFF00000000000000) >> 56),
                (byte) ((value & 0xFF000000000000) >> 48),
                (byte) ((value & 0xFF0000000000) >> 40),
                (byte) ((value & 0xFF00000000) >> 32),
                (byte) ((value & 0xFF000000) >> 24),
                (byte) ((value & 0xFF0000) >> 16),
                (byte) ((value & 0xFF00) >> 8),
                (byte) (value & 0xFF)
            });
        }

        // -- BigInt & UBigInt

        public void WriteBigInteger(BigInteger value)
        {
            var bytes = value.ToByteArray();
            Array.Reverse(bytes);

            WriteByteArray(bytes);
        }

        public void WriteUBigInteger(BigInteger value)
        {
            throw new NotImplementedException();
        }

        // -- Float

        public void WriteFloat(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            WriteByteArray(bytes);
        }

        // -- Double

        public void WriteDouble(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            WriteByteArray(bytes);
        }


        // -- StringArray

        public void WriteStringArray(string[] value)
        {
            var length = value.Length;

            for (var i = 0; i < length; i++)
                WriteString(value[i]);
        }

        // -- VarIntArray

        public void WriteVarIntArray(int[] value)
        {
            var length = value.Length;

            for (var i = 0; i < length; i++)
                WriteVarInt(value[i]);
        }

        // -- IntArray

        public void WriteIntArray(int[] value)
        {
            var length = value.Length;

            for (var i = 0; i < length; i++)
                WriteInt(value[i]);
        }

        // -- ByteArray

        public void WriteByteArray(byte[] value)
        {
            if (_buffer != null)
            {
                var tempLength = _buffer.Length + value.Length;
                var tempBuff = new byte[tempLength];

                Buffer.BlockCopy(_buffer, 0, tempBuff, 0, _buffer.Length);
                Buffer.BlockCopy(value, 0, tempBuff, _buffer.Length, value.Length);

                _buffer = tempBuff;
            }
            else
                _buffer = value;
        }

        #endregion Vars


        // -- Read methods

        public byte ReadByte()
        {
            var buffer = new byte[1];

            Receive(buffer, 0, buffer.Length);

            return buffer[0];
        }

        public char ReadChar()
        {
            return _encoding.GetChars(new []{ ReadByte() })[0];
            //return BitConverter.ToChar(new[] { ReadByte(), ReadByte() }, 0);
            //return Convert.ToChar(ReadByte());
        }

        public VarInt ReadVarInt1()
        {
            var result = 0;
            var length = 0;

            while (true)
            {
                var current = ReadByte();
                result |= (current & 0x7F) << length++*7;

                if (length > 6)
                    throw new ServerException("Reading error: VarInt too long.");

                if ((current & 0x80) != 0x80)
                    break;
            }

            return result;
        }

        public VarInt ReadVarInt()
        {
            uint result = 0;
            int length = 0;

            while (true)
            {
                var current = ReadByte();
                result |= (current & 0x7Fu) << length++*7;

                if (length > 5)
                    throw new ServerException("Reading error: VarInt may not be longer than 28 bits.");

                if ((current & 0x80) != 128)
                    break;
            }
            return (int) result;
        }

        public byte[] ReadByteArray(int value)
        {
            var result = new byte[value];
            if (value == 0) return result;
            int n = value;
            while (true)
            {
                n -= Receive(result, value - n, n);
                if (n == 0)
                    break;
            }
            return result;
        }

        public string ReadLine()
        {
            return _tcp.ReadLine();
        }

        // -- Read methods


        private void Send(byte[] buffer, int offset, int count)
        {
            _tcp.Send(buffer, offset, count);
        }

        private int Receive(byte[] buffer, int offset, int count)
        {
            return _tcp.Receive(buffer, offset, count);
        }

        public void SendPacket(ref IPacket packet)
        {
            var str = CreateData(ref packet);
            var array = Encoding.UTF8.GetBytes(str + "\r\n");
            _tcp.Send(array, 0, array.Length);
        }

        public Task SendAsync(byte[] buffer, int offset, int count)
        {
            return _tcp.SendAsync(buffer, offset, count);
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            return _tcp.ReceiveAsync(buffer, offset, count);
        }

        public Task SendPacketAsync(IPacket packet)
        {
            var str = CreateData(ref packet);
            var array = Encoding.UTF8.GetBytes(str + "\r\n");
            return _tcp.SendAsync(array, 0, array.Length);
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
            for (int i = 0; i < dataItems.Count - 1; i++)
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
                _tcp.DisconnectAsync();
                _tcp.Dispose();
            }

            _buffer = null;
        }
    }
}
