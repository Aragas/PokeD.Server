﻿using System;

namespace PokeD.Server.Commands
{
    [Flags]
    public enum PermissionFlags
    {
        None            = 0,
        UnVerified      = 1,
        User            = 2,
        Moderator       = 4,
        Administrator   = 8,
        Server          = 16,


        UnVerifiedOrHigher      = UnVerified | User | Moderator | Administrator | Server,
        UserOrHigher            = User | Moderator | Administrator | Server,
        ModeratorOrHigher       = Moderator | Administrator | Server,
        AdministratorOrHigher   = Administrator | Server,

        //Any                     = UnVerified | User | Moderator | Administrator | Server,
    }
}