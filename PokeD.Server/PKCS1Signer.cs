using System;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace PokeD.Server
{
    public sealed class PKCS1Signer
    {
        private AsymmetricCipherKeyPair RSAKeyPair { get; }
        private bool OnlyPublic { get; }


        /// <summary>
        /// Can Sign and DeSign
        /// </summary>
        /// <param name="rsaKeyPair"></param>
        public PKCS1Signer(AsymmetricCipherKeyPair rsaKeyPair)
        {
            RSAKeyPair = rsaKeyPair;
            OnlyPublic = false;
        }
        /// <summary>
        /// Can only Sign.
        /// </summary>
        /// <param name="rsaPublicKey"></param>
        public PKCS1Signer(AsymmetricKeyParameter rsaPublicKey)
        {
            RSAKeyPair = new AsymmetricCipherKeyPair(rsaPublicKey, new RsaKeyParameters(true, BigInteger.One, BigInteger.One));
            OnlyPublic = true;
        }
        /// <summary>
        /// Can only Sign.
        /// </summary>
        /// <param name="rsaPublicKey"></param>
        public PKCS1Signer(byte[] rsaPublicKey) : this(PublicKeyFactory.CreateKey(rsaPublicKey)) { }


        public byte[] SignData(byte[] data)
        {
            var eng = new Pkcs1Encoding(new RsaEngine());
            eng.Init(true, RSAKeyPair.Public);
            return eng.ProcessBlock(data, 0, data.Length);
        }
        public byte[] DeSignData(byte[] data)
        {
            if (OnlyPublic)
                throw new NotSupportedException("Private key not available");

            var eng = new Pkcs1Encoding(new RsaEngine());
            eng.Init(false, RSAKeyPair.Private);
            return eng.ProcessBlock(data, 0, data.Length);
        }
    }
}