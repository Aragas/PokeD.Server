using System;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    [Flags]
    public enum PermissionFlags
    {
        Default = 0,
        Verified = 1,
        Moderator = 2,
        Administrator = 4,
        Owner = 7,

        VerifiedOrHigher = Verified | Moderator | Administrator,
        ModeratorOrHigher = Moderator | Administrator,
        AdministratorOrHigher = Administrator,
    }
}