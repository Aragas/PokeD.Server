using System;

namespace PokeD.Server.Statistics.Data
{
    public struct Hotspots
    {
        public string LevelName { get; set; }
        public ulong NumberOfVisits { get; set; }
        public TimeSpan DurationOfVisit { get; set; }
    }
}