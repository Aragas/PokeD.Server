using System;
using System.Linq;
using System.Reflection;

using Aragas.TupleEventSystem;

using PCLExt.Config;
using PCLExt.Config.Extensions;

using PokeD.Core;
using PokeD.Core.Services;

namespace PokeD.Server.Services
{
    public abstract class BaseServerService : IService, IStartable, IStoppable
    {
        [ConfigIgnore]
        public IServiceContainer Services { get; }

        protected virtual string ServiceName => GetType().Name;
        protected virtual IConfigFile ServiceConfigFile { get; }
        protected ConfigType ConfigType { get; }

        private bool IsDisposed { get; set; }

        protected BaseServerService(IServiceContainer services, ConfigType configType) { Services = services; ConfigType = configType; }

        public virtual bool Start()
        {
            if (!FileSystemExtensions.LoadConfig(ServiceConfigFile, this))
            {
                Logger.Log(LogType.Warning, $"Failed to load {ServiceName} settings!");
                return false;
            }

            return true;
        }
        public virtual bool Stop()
        {
            if (!FileSystemExtensions.SaveConfig(ServiceConfigFile, this))
            {
                Logger.Log(LogType.Warning, $"Failed to save {ServiceName} settings!");
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // Dispose every event to check if somthing is leaking.
                    var eventFields = GetType().GetTypeInfo().DeclaredFields
                        .Where(fieldInfo =>
                        {
                            var typeInfo = fieldInfo.FieldType.GetTypeInfo();
                            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(BaseEventHandler<>);
                        });
                    foreach (var fieldInfo in eventFields)
                        (fieldInfo.GetValue(this) as IDisposable)?.Dispose();

                    var eventProperties = GetType().GetTypeInfo().DeclaredProperties
                        .Where(propertyInfo =>
                        {
                            var typeInfo = propertyInfo.PropertyType.GetTypeInfo();
                            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(BaseEventHandler<>);
                        });
                    foreach (var propertyInfo in eventProperties)
                        (propertyInfo.GetValue(this) as IDisposable)?.Dispose();
                }


                IsDisposed = true;
            }
        }
        ~BaseServerService()
        {
            Dispose(false);
        }
    }
}