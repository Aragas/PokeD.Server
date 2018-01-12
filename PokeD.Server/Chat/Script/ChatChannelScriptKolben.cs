using Kolben.Adapters;

using PokeD.Server.Clients;
using PokeD.Server.Storage.Files;

namespace PokeD.Server.Chat
{
    public class ChatChannelScriptKolben : BaseChatChannelScript
    {
        private KolbenFile KolbenFile { get; }

        public override string Name => (string) ScriptOutAdapter.Translate(KolbenFile["Name"]);
        public override string Description => (string) ScriptOutAdapter.Translate(KolbenFile["Description"]) ?? string.Empty;
        public override string Aliases => (string) ScriptOutAdapter.Translate(KolbenFile["Alias"]) ?? string.Empty;

        public ChatChannelScriptKolben(KolbenFile kolbenFile) { KolbenFile = kolbenFile; }

        public override bool MessageSend(ChatMessage chatMessage) => (bool) KolbenFile.CallFunction("MessageSend", chatMessage);

        public override bool Subscribe(Client client) => (bool) KolbenFile.CallFunction("Subscribe", client);

        public override bool UnSubscribe(Client client) => (bool) KolbenFile.CallFunction("UnSubscribe", client);
    }
}