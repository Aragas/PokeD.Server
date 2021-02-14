using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Server.Services
{
    public sealed class SecurityService : IHostedService
    {
        private const int RsaKeySize = 1024;

        public AsymmetricCipherKeyPair? RSAKeyPair { get; private set; }

        private readonly ILogger _logger;

        public SecurityService(ILogger<ChatChannelManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static AsymmetricCipherKeyPair GenerateKeyPair()
        {
            var secureRandom = new SecureRandom(new DigestRandomGenerator(new Sha512Digest()));
            var keyGenerationParameters = new KeyGenerationParameters(secureRandom, RsaKeySize);

            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            return keyPairGenerator.GenerateKeyPair();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting Security...");

            _logger.LogDebug("Generating RSA key pair...");
            RSAKeyPair = GenerateKeyPair();
            _logger.LogDebug("Generated RSA key pair.");

            _logger.LogDebug("Started Security.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Stopping Security...");
            RSAKeyPair = null;
            _logger.LogDebug("Stopped Security.");

            return Task.CompletedTask;
        }
    }
}