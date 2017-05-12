using System;
using System.Collections.Generic;

using PokeD.Server.Clients;
using PokeD.Server.Data;

namespace PokeD.Server.Commands
{
    public abstract class BaseCommandScript
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract IEnumerable<string> Aliases { get; }
        public abstract PermissionFlags Permission { get; }

        public abstract World World { set; }

        protected static PermissionFlags ParsePermissionFlags(string permissionFlags)
        {
            var permissions = permissionFlags.Split(' ');
            var flags = new List<PermissionFlags>();
            foreach (var permission in permissions)
            {
                PermissionFlags flag;
                if (Enum.TryParse(permission, out flag))
                    flags.Add(flag);
            }

            var value = PermissionFlags.None;
            foreach (var flag in flags)
                value |= flag;
            return value;
        }

        public abstract bool Initialize();

        public abstract void Handle(Client client, string alias, string[] arguments);

        public abstract void Help(Client client, string alias);
    }
}