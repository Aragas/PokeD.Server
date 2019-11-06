using System.IO;
using System.Net.Sockets;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace PokeD.Server.IO
{
    internal partial class BouncyCastleAesStream : Stream
    {
        private class EncryptorDecryptor
        {
            private BufferedBlockCipher DecryptCipher { get; }
            private BufferedBlockCipher EncryptCipher { get; }


            public EncryptorDecryptor(byte[] key)
            {
                EncryptCipher = new BufferedBlockCipher(new CfbBlockCipher(new AesEngine(), 8));
                EncryptCipher.Init(true, new ParametersWithIV(new KeyParameter(key), key, 0, 16));

                DecryptCipher = new BufferedBlockCipher(new CfbBlockCipher(new AesEngine(), 8));
                DecryptCipher.Init(false, new ParametersWithIV(new KeyParameter(key), key, 0, 16));
            }


            public byte[] Decrypt(byte[] buffer, int offset, int count) => DecryptCipher.ProcessBytes(buffer, offset, count);
            public byte[] Encrypt(byte[] buffer, int offset, int count) => EncryptCipher.ProcessBytes(buffer, offset, count);
        }

        private Stream Stream { get; }

        private EncryptorDecryptor ED { get; }


        public BouncyCastleAesStream(Socket client, byte[] key)
        {
            Stream = new NetworkStream(client);
            ED = new EncryptorDecryptor(key);
        }
        public BouncyCastleAesStream(Stream stream, byte[] key)
        {
            Stream = stream;
            ED = new EncryptorDecryptor(key);
        }
    }
}