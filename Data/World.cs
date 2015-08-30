using System;
using System.Diagnostics;
using System.Threading.Tasks;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.Packets.Server;

namespace PokeD.Server.Data
{
    public class World : IUpdatable, IDisposable
    {
        public bool DoDayCycle { get; set; }
        public int TimeOffset { get; set; }
        public string CurrentTime { get; set; }

        public Season Season { get; set; }
        public Weather Weather { get; set; }

        private readonly Server _server;

        public World(Server server)
        {
            _server = server;

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
                _server.SendToAllPlayers(new WorldDataPacket { DataItems = GetWorld() });

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

        public DataItems GetWorld()
        {
            if (DoDayCycle)
            {
                var now = DateTime.Now;
                if (TimeOffset != 0)
                    CurrentTime = now.AddSeconds(TimeOffset).Hour + "," + now.AddSeconds(TimeOffset).Minute + "," + now.AddSeconds(TimeOffset).Second;
                else
                    CurrentTime = DateTime.Now.Hour + "," + DateTime.Now.Minute + "," + DateTime.Now.Second;
            }
            else
                CurrentTime = "12,0,0";
            
            return new DataItems(new [] { ((int)Season).ToString(), ((int)Weather).ToString(), CurrentTime });
        }


        public void Dispose()
        {

        }
    }
}