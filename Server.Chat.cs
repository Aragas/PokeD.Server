using PCLExt.Config;

using PokeD.Server.Chat;
using PokeD.Server.Clients;

namespace PokeD.Server
{
    public partial class Server
    {
        [ConfigIgnore]
        public ChatChannelManager ChatChannelManager { get; }


        private void ChatClientConnected(Client client)
        {
            ChatChannelManager.FindByAlias("global").Subscribe(client);
        }
        private void ChatClientDisconnected(Client client)
        {
            foreach (var chatChannel in ChatChannelManager.ChatChannels)
                chatChannel.UnSubscribe(client);
        }
        private void ChatClientSentMessage(ChatMessage chatMessage)
        {
            foreach (var chatChannel in ChatChannelManager.ChatChannels)
                if(chatChannel.MessageSend(chatMessage))
                    Logger.LogChatMessage(chatMessage.Sender.Name, chatChannel.Name, chatMessage.Message);
        }
    }
}