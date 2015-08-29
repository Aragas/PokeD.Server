using System;
using System.Collections.Generic;

using PokeD.Core.Interfaces;

namespace PokeD.Server.Data
{
    public class World : IUpdatable, IDisposable
    {
        public bool DoDayCycle { get; set; }
        public int TimeOffset { get; set; }
        public string CurrentTime { get; set; }

        public Season Season { get; set; }
        public Weather Weather { get; set; }

        public World()
        {
            Season = Season.Winter;
            Weather = Weather.Blizzard;
            DoDayCycle = true;
        }

        public void Update()
        {

        }

        public List<string> GetWorld()
        {
            if (DoDayCycle)
            {
                DateTime now = DateTime.Now;
                if (TimeOffset != 0)
                    CurrentTime = now.AddSeconds(TimeOffset).Hour + "," + now.AddSeconds(TimeOffset).Minute + "," + now.AddSeconds(TimeOffset).Second;
                else
                    CurrentTime = DateTime.Now.Hour + "," + DateTime.Now.Minute + "," + DateTime.Now.Second;
            }
            else
                CurrentTime = "12,0,0";
            
            return new List<string> { ((int)Season).ToString(), ((int)Weather).ToString(), CurrentTime };
        }


        public void Dispose()
        {

        }
    }
}