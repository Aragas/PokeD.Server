using System;
using System.Linq;

using OpenWeatherMap;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;

namespace PokeD.Server.Data
{
    public class World : IUpdatable, IDisposable
    {
        public bool UseLocation { get; set; }
        bool LocationChanged { get; set; }
        public string Location { get { return _location; } set { LocationChanged = _location != value; _location = value; } }
        string _location;
        CurrentWeatherResponse CurrentWeatherResponse { get; set; }

        public bool UseRealTime { get; set; }
        public bool DoDayCycle { get; set; }

        public Season Season { get; set; }
        public Weather Weather { get; set; }

        public TimeSpan CurrentTime
        {
            get { TimeSpan timeSpan; return TimeSpan.TryParseExact(CurrentTimeString, "hh\\,mm\\,ss", null, out timeSpan) ? timeSpan : TimeSpan.Zero; }
            set { CurrentTimeString = value.Hours + "," + value.Minutes + "," + value.Seconds; }
        }
        string CurrentTimeString { get; set; }

        TimeSpan TimeSpanOffset { get { return new TimeSpan(0, 0, 0, TimeOffset); } }
        int TimeOffset { get; set; }


        public World()
        {
            Season = Season.Spring;
            Weather = Weather.Sunny;
            DoDayCycle = true;
            UseRealTime = true;
        }


        /// <summary>
        /// Call it one per second.
        /// </summary>
        public void Update()
        {
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
                
                #region Location
                if (UseLocation)
                    if (CurrentWeatherResponse != null || (CurrentWeatherResponse != null && DateTime.Now.Subtract(CurrentWeatherResponse.LastUpdate.Value) > new TimeSpan(0, 20, 0)) || LocationChanged)
                    {
                        var client = new OpenWeatherMapClient();

                        CurrentWeatherResponse = client.CurrentWeather.GetByName(Location).Result;


                        var weather = CurrentWeatherResponse.Weather.Number;
                        if (Enumerable.Range(200, 32).Contains(weather))
                            Weather = Weather.Thunderstorm;
                        else if (Enumerable.Range(300, 21).Contains(weather))
                            Weather = Weather.Rain;
                        else if (Enumerable.Range(500, 31).Contains(weather))
                            Weather = Weather.Rain;
                        else if (weather == 600 || weather == 601 || Enumerable.Range(611, 10).Contains(weather))
                            Weather = Weather.Snow;
                        else if (weather == 602 || weather == 622)
                            Weather = Weather.Blizzard;
                        else if (weather == 701 || weather == 711 || weather == 721 || weather == 741)
                            Weather = Weather.Fog;
                        else if (weather == 762)
                            Weather = Weather.Ash;
                        else if (weather == 751 || weather == 761 || weather == 781)
                            Weather = Weather.Sandstorm;
                        else if (weather == 800)
                            Weather = Weather.Sunny;
                        else if (weather == 801)
                            Weather = Weather.Clear;

                        if (CurrentWeatherResponse.Temperature.Value < 0)
                            Season = Season.Winter;
                        else if (CurrentWeatherResponse.Temperature.Value > 18)
                            Season = Season.Spring;
                        else if (CurrentWeatherResponse.Temperature.Value < 18)
                            Season = Season.Summer;


                        LocationChanged = false;
                    }

                #endregion Location

            }
            else
                CurrentTimeString = "12,0,0";
            
            return new DataItems(new [] { ((int)Season).ToString(), ((int)Weather).ToString(), CurrentTimeString });
        }


        public void Dispose()
        {

        }
    }
}