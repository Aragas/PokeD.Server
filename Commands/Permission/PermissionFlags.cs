using System;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    [Flags]
    public enum PermissionFlags
    {
        Default = 0,
        GameJolt = 1,
        Moderator = 2,
        Administrator = 4,
        Owner = 8
    }
}