using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PCLExt.Config;

using PokeD.Core.Services;
using PokeD.Server.Chat;

namespace PokeD.Server.Services
{
    public class ChatChannelManagerService : ServerService
    {
        private List<ChatChannel> ChatChannels { get; } = new List<ChatChannel>();

        public ChatChannelManagerService(IServiceContainer services, ConfigType configType) : base(services, configType) { }

        public ChatChannel FindByName(string name) => ChatChannels.FirstOrDefault(chatChannel => chatChannel.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        public ChatChannel FindByAlias(string alias) => ChatChannels.FirstOrDefault(chatChannel => chatChannel.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<ChatChannel> GetChatChannels() => ChatChannels;

        public override bool Start()
        {
            Logger.Log(LogType.Debug, $"Loading ChatChannels...");
            LoadChatChannels();
            Logger.Log(LogType.Debug, $"Loaded ChatChannels.");

            return true;
        }
        public override bool Stop()
        {
            Logger.Log(LogType.Debug, $"Unloading ChatChannels...");
            ChatChannels.Clear();
            Logger.Log(LogType.Debug, $"Unloaded ChatChannels.");
            return true;
        }
        private void LoadChatChannels()
        {
            var types = typeof(ChatChannelManagerService).GetTypeInfo().Assembly.DefinedTypes
                .Where(typeInfo => typeof(ChatChannel).GetTypeInfo().IsAssignableFrom(typeInfo))
                .Where(typeInfo => !typeInfo.IsDefined(typeof(ChatChannelDisableAutoLoadAttribute), true))
                .Where(typeInfo => !typeInfo.IsAbstract);

            foreach (var chatChannel in types.Where(type => type != typeof(ScriptChatChannel).GetTypeInfo()).Select(type => (ChatChannel)Activator.CreateInstance(type.AsType())))
                ChatChannels.Add(chatChannel);

            ChatChannels.AddRange(ScriptChatChannelLoader.LoadChatChannels());
        }

        public override void Dispose()
        {
            ChatChannels.Clear();
        }
    }
}