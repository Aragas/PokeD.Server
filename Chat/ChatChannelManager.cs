using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PCLExt.AppDomain;

namespace PokeD.Server.Chat
{
    public class ChatChannelManager
    {
        private Server Server { get; }
        public List<ChatChannel> ChatChannels { get; } = new List<ChatChannel>();

        public ChatChannelManager(Server server, bool autoLoad = true)
        {
            Server = server;

            if(autoLoad)
                AutoLoadChatChannels();
        }
        private void AutoLoadChatChannels()
        {
            var types = AppDomain.GetAssembly(typeof(ChatChannelManager)).DefinedTypes
                .Where(typeInfo => typeof(ChatChannel).GetTypeInfo().IsAssignableFrom(typeInfo))
                .Where(typeInfo => !typeInfo.IsDefined(typeof(ChatChannelDisableAutoLoadAttribute), true))
                .Where(typeInfo => !typeInfo.IsAbstract);
                
            foreach (var chatChannel in types.Where(type => type != typeof(ChatChannelLua).GetTypeInfo()).Select(type => (ChatChannel) Activator.CreateInstance(type.AsType())))
                ChatChannels.Add(chatChannel);

            ChatChannels.AddRange(ChatChannelLuaLoader.LoadChatChannels());
        }


        public ChatChannel FindByName(string name) => ChatChannels.FirstOrDefault(chatChannel => chatChannel.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        public ChatChannel FindByAlias(string alias) => ChatChannels.FirstOrDefault(chatChannel => chatChannel.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
    }
}