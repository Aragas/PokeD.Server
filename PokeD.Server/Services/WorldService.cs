using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PokeD.Core.Data.P3D;
using PokeD.Server.Data;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Server.Services
{
    public sealed class WorldService : IHostedService
    {
        public bool UseRealTime { get; set; } = true;
        public bool DoDayCycle { get; set; } = true;

        public int WeatherUpdateTimeInMinutes { get; set; } = 60;

        public Season Season { get; set; } = Season.Spring;
        public Weather Weather { get; set; } = Weather.Sunny;
        private int WeekOfYear => (int) (DateTime.Now.DayOfYear - ((DateTime.Now.DayOfWeek - DayOfWeek.Monday) / 7.0) + 1.0);

        public TimeSpan CurrentTime
        {
            get => TimeSpan.TryParseExact(CurrentTimeString, "hh\\,mm\\,ss", CultureInfo.InvariantCulture, TimeSpanStyles.None, out var timeSpan) ? timeSpan : TimeSpan.Zero;
            set => CurrentTimeString = $"{value.Hours:00},{value.Minutes:00},{value.Seconds:00}";
        }
        private string CurrentTimeString { get; set; } = string.Empty;

        private TimeSpan TimeSpanOffset { get; set; }
        //private TimeSpan TimeSpanOffset => TimeSpan.FromSeconds(TimeOffset);
        //private int TimeOffset { get; set; }

        private DateTime WorldUpdateTime { get; set; } = DateTime.UtcNow;

        private Task? _updateTask;
        private readonly CancellationTokenSource _stoppingCts = new();

        private readonly ILogger _logger;

        public WorldService(ILogger<ChatChannelManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Loading World...");
            _updateTask = UpdateCycleAsync(_stoppingCts.Token);
            _logger.LogDebug("Loaded World.");

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Unloading World...");
            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_updateTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
            _logger.LogDebug("Unloaded World.");
        }


        private void UpdateWorld()
        {
            Season = (WeekOfYear % 4) switch
            {
                1 => Season.Winter,
                2 => Season.Spring,
                3 => Season.Summer,
                0 => Season.Fall,

                _ => Season.Summer,
            };
            var r = new Random().Next(0, 100);
            switch (Season)
            {
                case Season.Winter:
                    if (r < 20)
                        Weather = Weather.Rain;
                    else if (r >= 20 && r < 50)
                        Weather = Weather.Clear;
                    else
                        Weather = Weather.Snow;
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
                    if (r < 5)
                        Weather = Weather.Snow;
                    else if (r >= 5 && r < 80)
                        Weather = Weather.Rain;
                    else
                        Weather = Weather.Clear;
                    break;

                default:
                    Weather = Weather.Clear;
                break;
            }

            _logger.Log(LogLevel.Information, new EventId(30, "Event"), $"Set Season: {Season}");
            _logger.Log(LogLevel.Information, new EventId(30, "Event"), $"Set Weather: {Weather}");
        }

        public async Task UpdateCycleAsync(CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();
            while (!ct.IsCancellationRequested)
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
                    await Task.Delay(time, ct);
                }
                TimeSpanOffset = TimeSpanOffset.Add(TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds));
                watch.Reset();
                watch.Start();
            }
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
    }
}