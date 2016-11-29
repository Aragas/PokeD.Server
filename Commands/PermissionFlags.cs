using System;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    [Flags]
    public enum PermissionFlags
    {
        None            = 0,
        UnVerified      = 1,
        Verified        = 2,
        Moderator       = 4,
        Administrator   = 8,
        Server          = 16,


        UnVerifiedOrHigher      = UnVerified | Verified | Moderator | Administrator | Server,
        VerifiedOrHigher        = Verified | Moderator | Administrator | Server,
        ModeratorOrHigher       = Moderator | Administrator | Server,
        AdministratorOrHigher   = Administrator | Server,

        Any                     = UnVerified | Verified | Moderator | Administrator | Server,
    }
}