using System;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    [Flags]
    public enum MuteStatus
    {
        None                = 0,
        Completed           = 1,
        ClientNotFound      = 2,
        MutedYourself       = 4,
        IsNotMuted          = 8,
    }
}