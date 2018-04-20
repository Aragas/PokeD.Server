using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

using PCLExt.Config;

using PokeD.Core;
using PokeD.Core.Data.P3D;
using PokeD.Core.Services;
using PokeD.Server.Data;
using PokeD.Server.Storage.Files;

namespace PokeD.Server.Services
{
    public class WorldService : BaseServerService
    {
        protected override IConfigFile ServiceConfigFile => new WorldComponentConfigFile(ConfigType);

        private CancellationTokenSource UpdateToken { get; set; } = new CancellationTokenSource();
        private ManualResetEventSlim UpdateLock { get; } = new ManualResetEventSlim(false);

        [ConfigIgnore]
        public bool UseLocation { get; set; }
        private bool LocationChanged { get; set; }

        [ConfigIgnore]
        public string Location
        {
            get => _location;
            set { LocationChanged = _location != value; _location = value; }
        }
        private string _location = string.Empty;

        public bool UseRealTime { get; set; } = true;

        public bool DoDayCycle { get; set; } = true;
        
        public int WeatherUpdateTimeInMinutes { get; set; } = 60;

        public Season Season { get; set; } = Season.Spring;

        public Weather Weather { get; set; } = Weather.Sunny;

        public TimeSpan CurrentTime
        {
            get => TimeSpan.TryParseExact(CurrentTimeString, "hh\\,mm\\,ss", CultureInfo.InvariantCulture, TimeSpanStyles.None, out TimeSpan timeSpan) ? timeSpan : TimeSpan.Zero;
            set => CurrentTimeString = $"{value.Hours:00},{value.Minutes:00},{value.Seconds:00}";
        }
        private string CurrentTimeString { get; set; }

        private TimeSpan TimeSpanOffset { get; set; }
        //private TimeSpan TimeSpanOffset => TimeSpan.FromSeconds(TimeOffset);
        //private int TimeOffset { get; set; }

        private DateTime WorldUpdateTime { get; set; } = DateTime.UtcNow;


        public WorldService(IServiceContainer services, ConfigType configType) : base(services, configType) { }


        private int WeekOfYear => (int) (DateTime.Now.DayOfYear - (DateTime.Now.DayOfWeek - DayOfWeek.Monday) / 7.0 + 1.0);
        private void UpdateWorld()
        {
            switch (WeekOfYear % 4)
            {
                case 1:
                    Season = Season.Winter;
                    break;
                case 2:
                    Season = Season.Spring;
                    break;
                case 3:
                    Season = Season.Summer;
                    break;
                case 0:
                    Season = Season.Fall;
                    break;
                
                default:
                    Season = Season.Summer;
                    break;
            }

            var r = new Random().Next(0, 100);
            switch (Season)
            {
                case Season.Winter:
                    if (r < 20)
                        Weather = Weather.Rain;
                    else if (r >= 20 && r < 50)
                        Weather = Weather.Clear;
                    //else
                    //    Weather = Weather.Snow;
                    break;

                case Season.Spring:
                    if (r < 5)
                        Weather = Weather.Sunny;
                    else if (r >= 5 && r < 40)
                        Weather = Weather.Rain;
                    else
                        Weather = Weather.Clear;
                    break;

                case Season.Summer:
                    if (r < 40)
                        Weather = Weather.Clear;
                    else if(r >= 40 && r < 80)
                        Weather = Weather.Rain;
                    else
                        Weather = Weather.Sunny;
                    break;

                case Season.Fall:
                    //if (r < 5)
                    //    Weather = Weather.Snow;
                    //else 
                    if (r >= 5 && r < 80)
                        Weather = Weather.Rain;
                    else
                        Weather = Weather.Clear;
                    break;
                
                default:
                    Weather = Weather.Clear;
                break;
            }

            Logger.Log(LogType.Event, $"Set Season: {Season}");
            Logger.Log(LogType.Event, $"Set Weather: {Weather}");
        }

        public void UpdateCycle()
        {
            UpdateLock.Reset();

            var watch = Stopwatch.StartNew();
            while (!UpdateToken.IsCancellationRequested)
            {
                if (WorldUpdateTime < DateTime.UtcNow)
                {
                    UpdateWorld();
                    WorldUpdateTime = DateTime.UtcNow.AddMinutes(WeatherUpdateTimeInMinutes);
                }

                if (watch.ElapsedMilliseconds < 1000)
                {
                    var time = (int)(10 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    Thread.Sleep(time);
                }
                TimeSpanOffset.Add(TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds));
                watch.Reset();
                watch.Start();
            }

            UpdateLock.Set();
        }



        public DataItems GenerateDataItems()
        {
            if (DoDayCycle)
            {
                if (TimeSpanOffset != TimeSpan.Zero)
                    if (UseRealTime)
                    {
                        var time = DateTime.Now.Add(TimeSpanOffset);
                        CurrentTimeString = $"{time.Hour:00},{time.Minute:00},{time.Second:00}";
                    }
                    else
                    {
                        CurrentTime += TimeSpanOffset;
                    }
                else if (UseRealTime)
                {
                    var time = DateTime.Now.Add(TimeSpanOffset);
                    CurrentTimeString = $"{time.Hour:00},{time.Minute:00},{time.Second:00}";
                }
            }
            else
                CurrentTimeString = "12,00,00";

            return new DataItems(((int) Season).ToString(), ((int) Weather).ToString(), CurrentTimeString);
        }


        public override bool Start()
        {
            Logger.Log(LogType.Debug, "Loading World...");
            if (!base.Start())
                return false;

            UpdateToken = new CancellationTokenSource();
            new Thread(UpdateCycle)
            {
                Name = "ModuleManagerUpdateTread",
                IsBackground = true
            }.Start();

            Logger.Log(LogType.Debug, "Loaded World.");

            return true;
        }
        public override bool Stop()
        {
            Logger.Log(LogType.Debug, "Unloading World...");
            if (!base.Stop())
                return false;

            if (UpdateToken?.IsCancellationRequested == false)
            {
                UpdateToken.Cancel();
                UpdateLock.Wait();
            }

            Logger.Log(LogType.Debug, "Unloaded World.");

            return true;
        }

        public override void Dispose()
        {
            if (UpdateToken?.IsCancellationRequested == false)
            {
                UpdateToken.Cancel();
                UpdateLock.Wait();
            }
        }
    }
}