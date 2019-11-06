using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PCLExt.Config;

using PokeD.Core;
using PokeD.Core.Services;
using PokeD.Server.Chat;

namespace PokeD.Server.Services
{
    public class ChatChannelManagerService : BaseServerService
    {
        private List<ChatChannel> ChatChannels { get; } = new List<ChatChannel>();

        private bool IsDisposed { get; set; }

        public ChatChannelManagerService(IServiceContainer services, ConfigType configType) : base(services, configType) { }

        public ChatChannel FindByName(string name) => ChatChannels.Find(chatChannel => chatChannel.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        public ChatChannel FindByAlias(string alias) => ChatChannels.Find(chatChannel => chatChannel.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<ChatChannel> GetChatChannels() => ChatChannels;

        public override bool Start()
        {
            Logger.Log(LogType.Debug, "Loading ChatChannels...");
            LoadChatChannels();
            Logger.Log(LogType.Debug, "Loaded ChatChannels.");

            return true;
        }
        public override bool Stop()
        {
            Logger.Log(LogType.Debug, "Unloading ChatChannels...");
            ChatChannels.Clear();
            Logger.Log(LogType.Debug, "Unloaded ChatChannels.");
            return true;
        }
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

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    ChatChannels.Clear();
                }


                IsDisposed = true;
            }
            base.Dispose(disposing);
        }
    }
}