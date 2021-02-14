using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PokeD.Server.Chat;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Server.Services
{
    public sealed class ChatChannelManagerService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private List<ChatChannel> ChatChannels { get; } = new();

        public ChatChannelManagerService(ILogger<ChatChannelManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ChatChannel FindByName(string name) => ChatChannels.Find(chatChannel => chatChannel.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        public ChatChannel FindByAlias(string alias) => ChatChannels.Find(chatChannel => chatChannel.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<ChatChannel> GetChatChannels() => ChatChannels;

        private void LoadChatChannels()
        {
            var chatChannelTypes = typeof(ChatChannelManagerService).GetTypeInfo().Assembly.DefinedTypes
                .Where(typeInfo => typeof(ChatChannel).GetTypeInfo().IsAssignableFrom(typeInfo) &&
                !typeInfo.IsDefined(typeof(ChatChannelDisableAutoLoadAttribute), true) &&
                !typeInfo.IsAbstract);

            foreach (var chatChannel in chatChannelTypes.Where(type => type != typeof(ScriptChatChannel).GetTypeInfo()).Select(type => (ChatChannel) Activator.CreateInstance(type.AsType())))
                ChatChannels.Add(chatChannel);

            var scriptChatChannelLoaderTypes = typeof(ChatChannelManagerService).GetTypeInfo().Assembly.DefinedTypes
                .Where(typeInfo => typeof(ScriptChatChannelLoader).GetTypeInfo().IsAssignableFrom(typeInfo) &&
                !typeInfo.IsDefined(typeof(ChatChannelDisableAutoLoadAttribute), true) &&
                !typeInfo.IsAbstract);

            foreach (var scriptChatChannelLoader in scriptChatChannelLoaderTypes.Where(type => type != typeof(ScriptChatChannelLoader).GetTypeInfo()).Select(type => (ScriptChatChannelLoader) Activator.CreateInstance(type.AsType())))
                ChatChannels.AddRange(scriptChatChannelLoader.LoadChatChannels());
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Loading ChatChannels...");
            LoadChatChannels();
            _logger.LogDebug("Loaded ChatChannels.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Unloading ChatChannels...");
            ChatChannels.Clear();
            _logger.LogDebug("Unloaded ChatChannels.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            ChatChannels.Clear();
        }
    }
}