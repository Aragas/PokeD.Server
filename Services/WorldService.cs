using System;
using System.Diagnostics;

using PCLExt.Config;

using PokeD.Core;
using PokeD.Core.Data.P3D;
using PokeD.Core.Services;
using PokeD.Server.Data;
using PokeD.Server.Storage.Files;

namespace PokeD.Server.Services
{
    public class WorldService : ServerService, IUpdatable
    {
        protected override IConfigFile ServiceConfigFile => new WorldComponentConfigFile(ConfigType);

        [ConfigIgnore]
        public bool UseLocation { get; set; }
        bool LocationChanged { get; set; }

        [ConfigIgnore]
        public string Location
        {
            get => _location;
            set { LocationChanged = _location != value; _location = value; }
        }
        string _location = string.Empty;

        public bool UseRealTime { get; set; } = true;

        public bool DoDayCycle { get; set; } = true;

        public Season Season { get; set; } = Season.Spring;

        public Weather Weather { get; set; } = Weather.Sunny;

        public TimeSpan CurrentTime
        {
            get => TimeSpan.TryParseExact(CurrentTimeString, "hh\\,mm\\,ss", null, out TimeSpan timeSpan) ? timeSpan : TimeSpan.Zero;
            set => CurrentTimeString = $"{value.Hours:00},{value.Minutes:00},{value.Seconds:00}";
        }

        string CurrentTimeString { get; set; }

        TimeSpan TimeSpanOffset => TimeSpan.FromSeconds(TimeOffset);
        int TimeOffset { get; set; }


        public WorldService(IServiceContainer services, ConfigType configType) : base(services, configType) { }


        Stopwatch Watch = Stopwatch.StartNew();
        public void Update()
        {
            if (Watch.ElapsedMilliseconds > 1000)
            {
                TimeOffset++;

                Watch.Reset();
                Watch.Start();
            }
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
                CurrentTimeString = "12,00,00";

            return new DataItems(((int) Season).ToString(), ((int) Weather).ToString(), CurrentTimeString);
        }


        public override bool Start()
        {
            Logger.Log(LogType.Debug, $"Loading World...");
            if (!base.Start())
                return false;

            Logger.Log(LogType.Debug, $"Loaded World.");

            return true;
        }
        public override bool Stop()
        {
            Logger.Log(LogType.Debug, $"Unloading World...");
            if (!base.Stop())
                return false;

            Logger.Log(LogType.Debug, $"Unloaded World.");

            return true;
        }

        public override void Dispose() { }
    }
}
