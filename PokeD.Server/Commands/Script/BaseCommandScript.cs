using Microsoft.Extensions.DependencyInjection;

using PokeD.Server.Clients;
using PokeD.Server.Services;

using System;
using System.Collections.Generic;

namespace PokeD.Server.Commands
{
    public abstract class BaseCommandScript
    {
        protected static PermissionFlags ParsePermissionFlags(string permissionFlags)
        {
            var permissions = permissionFlags.Split(' ');
            var flags = new List<PermissionFlags>();
            foreach (var permission in permissions)
            {
                if (Enum.TryParse(permission, out PermissionFlags flag))
                    flags.Add(flag);
            }

            var value = PermissionFlags.None;
            foreach (var flag in flags)
                value |= flag;
            return value;
        }

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract IEnumerable<string> Aliases { get; }
        public abstract PermissionFlags Permission { get; }

        protected WorldService World { get; }

        protected BaseCommandScript(IServiceProvider serviceProvider)
        {
            World = serviceProvider.GetRequiredService<WorldService>();
        }

        public abstract void Handle(Client client, string alias, string[] arguments);

        public abstract void Help(Client client, string alias);
    }
}