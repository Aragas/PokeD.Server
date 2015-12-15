using System;

namespace PokeD.Server.Extensions
{
    public static class DateTimeExtensions
    {
        public static int ToUnixTime(this DateTime currentTime)
        {
            var zuluTime = currentTime.ToUniversalTime();
            var unixEpoch = new DateTime(1970, 1, 1);
            var unixTimeStamp = (int) zuluTime.Subtract(unixEpoch).TotalSeconds;

            //var epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
            //var unixTicks = new TimeSpan(currentTime.Ticks) - epochTicks;
            //var unixTime = (int) unixTicks.TotalSeconds;

            return unixTimeStamp;
        }
    }
}
