using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

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
            Season = Season.Spring;
            Weather = Weather.Sunny;
            DoDayCycle = true;
        }

        public static int WorldProcessorThreadTime { get; set; }

        public void Update()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                {
                }

                if (watch.ElapsedMilliseconds < 1000)
                {
                    var time = (int) (1000 - watch.ElapsedMilliseconds);
                    if (time < 0)
                        time = 0;

                    WorldProcessorThreadTime = (int) watch.ElapsedMilliseconds;
                    Task.Delay(time).Wait();
                }

                watch.Reset();
                watch.Start();
            }
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