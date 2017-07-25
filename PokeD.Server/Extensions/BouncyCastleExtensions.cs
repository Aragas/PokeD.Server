using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.X509;

namespace PokeD.Server.Extensions
{
    public static class BouncyCastleExtensions
    {
        public static byte[] PublicKeyToByteArray(this AsymmetricCipherKeyPair keyPair)
        {
            var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
            return publicKeyInfo.ToAsn1Object().GetDerEncoded();
        }
    }
}