using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;

using PCLExt.Config;

using PokeD.Core.Services;

namespace PokeD.Server.Services
{
    public class SecurityService : ServerService
    {
        private const int RsaKeySize = 1024;

        [ConfigIgnore]
        public AsymmetricCipherKeyPair RSAKeyPair { get; private set; }

        public SecurityService(IServiceContainer services, ConfigType configType) : base(services, configType) { }


        public override bool Start()
        {
            Logger.Log(LogType.Debug, "Loading Security...");

            Logger.Log(LogType.Debug, "Generating RSA key pair...");
            RSAKeyPair = GenerateKeyPair();
            Logger.Log(LogType.Debug, "Generated RSA key pair.");

            Logger.Log(LogType.Debug, "Loaded Security.");

            return true;
        }
        public override bool Stop()
        {
            Logger.Log(LogType.Debug, "Loading Security...");
            RSAKeyPair = null;
            Logger.Log(LogType.Debug, "Loaded Security.");

            return true;
        }
        private static AsymmetricCipherKeyPair GenerateKeyPair()
        {
            var secureRandom = new SecureRandom(new DigestRandomGenerator(new Sha512Digest()));
            var keyGenerationParameters = new KeyGenerationParameters(secureRandom, RsaKeySize);

            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            return keyPairGenerator.GenerateKeyPair();
        }

        public override void Dispose()
        {

        }
    }
}