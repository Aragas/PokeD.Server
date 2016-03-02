using System;
using System.Diagnostics;

using Aragas.Core.Interfaces;

using PokeD.Core.Data.P3D;

namespace PokeD.Server.Data
{
    public class World : IUpdatable, IDisposable
    {
        public bool UseLocation { get; set; }
        bool LocationChanged { get; set; }

        public string Location { get { return _location; } set { LocationChanged = _location != value; _location = value; } }
        string _location = string.Empty;

        public bool UseRealTime { get; set; } = true;

        public bool DoDayCycle { get; set; } = true;

        public Season Season { get; set; } = Season.Spring;

        public Weather Weather { get; set; } = Weather.Sunny;

        public TimeSpan CurrentTime
        {
            get { TimeSpan timeSpan; return TimeSpan.TryParseExact(CurrentTimeString, "HH\\,mm\\,ss", null, out timeSpan) ? timeSpan : TimeSpan.Zero; }
            set { CurrentTimeString = value.Hours + "," + value.Minutes + "," + value.Seconds; }
        }
        string CurrentTimeString { get; set; }

        TimeSpan TimeSpanOffset => TimeSpan.FromSeconds(TimeOffset);
        int TimeOffset { get; set; }


        Stopwatch Watch = Stopwatch.StartNew();
        public void Update()
        {
            if (Watch.ElapsedMilliseconds < 1000) { return; }

            Watch.Reset();
            Watch.Start();

            TimeOffset++;
        }


        public DataItems GenerateDataItems()
        {
            if (DoDayCycle)
            {
                var now = DateTime.Now;
                if (TimeOffset != 0)
                    if (UseRealTime)
                        CurrentTimeString = now.AddSeconds(TimeOffset).Hour + "," + now.AddSeconds(TimeOffset).Minute + "," + now.AddSeconds(TimeOffset).Second;
                    else
                        CurrentTime += TimeSpanOffset;
                else
                    if (UseRealTime)
                        CurrentTimeString = DateTime.Now.Hour + "," + DateTime.Now.Minute + "," + DateTime.Now.Second;
            }
            else
                CurrentTimeString = "12,0,0";
            
            return new DataItems(((int) Season).ToString(), ((int) Weather).ToString(), CurrentTimeString);
        }


        public void Dispose() { }
    }
}