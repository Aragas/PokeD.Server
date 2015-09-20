using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;

namespace PokeD.Server
{
    public sealed class PKCS1Signature
    {
        // Not really related to this class.
        public static byte[] CreateSecretKey(int length = 16)
        {
            var generator = new CipherKeyGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), length * 8));

            return generator.GenerateKey();
        }


        public AsymmetricCipherKeyPair RSAKeyPair { get; set; }
        public PKCS1Signature(AsymmetricCipherKeyPair rsaKeyPair)
        {
            RSAKeyPair = rsaKeyPair;
        }

        public byte[] SignData(byte[] data)
        {
            var eng = new Pkcs1Encoding(new RsaEngine());
            eng.Init(true, RSAKeyPair.Public);
            return eng.ProcessBlock(data, 0, data.Length);
        }

        public byte[] DeSignData(byte[] data)
        {
            var eng = new Pkcs1Encoding(new RsaEngine());
            eng.Init(false, RSAKeyPair.Private);
            return eng.ProcessBlock(data, 0, data.Length);
        }
    }
}
