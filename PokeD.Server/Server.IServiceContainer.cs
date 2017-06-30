using PokeD.Core.Services;

namespace PokeD.Server
{
    public partial class Server : IServiceContainer
    {
        private ServiceContainer Services { get; } = new ServiceContainer();

        public T GetService<T>() where T : class, IService => Services.GetService<T>();
        public void AddService<T>(T component) where T : class, IService => Services.AddService(component);
        public void RemoveService<T>() where T : class, IService => Services.RemoveService<T>();
    }
}