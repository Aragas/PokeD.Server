/*
using System.Linq;

using MineLib.Core.Client;
using MineLib.Core.Loader;

namespace PokeD.Server.Clients.Pixelmon
{
    public partial class PixelmonClient : MineLibClient
    {
        public override string Name { get; }


        public PixelmonClient(string login, string password) : base(login)
        {
            RegisterSupportedReceiveEvents();

            var modules = AssemblyParser.GetAssemblyInfos("Protocol*.dll");
            var module = modules.Any() ? modules.First() : new AssemblyInfo("NONE");

            ProtocolHandler = new ModularProtocolHandler(this, module, login, password);
        }


        public override void Connect(ServerInfo serverInfo) { ProtocolHandler.Connect(serverInfo); }
        public override void Disconnect() { ProtocolHandler.Disconnect(); }


        public override void Dispose()
        {
            ProtocolHandler?.Dispose();
        }
    }
}
*/