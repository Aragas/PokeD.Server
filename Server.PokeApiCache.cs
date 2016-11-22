using System;
using System.Threading.Tasks;

using PCLExt.Input;

using PokeD.Core.Data.PokeApi;

namespace PokeD.Server
{
    public partial class Server
    {
        private const int CacheMaxPokemon = 721;
        private const int CacheMaxItem = 749;
        private const int CacheMaxType = 18;
        private const int CacheMaxAbility = 190;
        private const int CacheMaxEgggroup = 15;

        private static async Task CacheDoMultiTask(int size, int max, Func<int, Task> func)
        {
            var index = 0;
            var array = new Task[size];

            for (index = 1; index + size <= max; index += size)
            {
                for (var j = 0; j < array.Length; j++)
                    array[j] = func(index + j);
                
                await Task.WhenAll(array);
            }
            if (max - index > 0)
            {
                array = new Task[max - index];
                for (var i = 0; i < array.Length; i++)
                    array[i] = func(index + i);

                await Task.WhenAll(array);
            }
        }

        private static async Task CachePokemon(int index)
        {
            try
            {
                Input.ConsoleWrite($"Caching Pokemon {index:000}");
                await PokeApiV2.GetPokemon(new ResourceUri($"api/v2/pokemon/{index}/", true));
            }
            catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Pokemon {index:000}"); }
        }
        private static async Task CachePokemonSpecies(int index)
        {
            try
            {
                Input.ConsoleWrite($"Caching Pokemon Species {index:000}");
                await PokeApiV2.GetPokemonSpecies(new ResourceUri($"api/v2/pokemon-species/{index}/", true));
            }
            catch (Exception)
            {
                Logger.Log(LogType.Warning, $"Failed Caching Pokemon Species {index:000}");
            }
        }
        private static async Task CacheItem(int index)
        {
            try
            {
                Input.ConsoleWrite($"Caching Item {index:000}");
                await PokeApiV2.GetItems(new ResourceUri($"api/v2/item/{index}/", true));
            }
            catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Item {index:000}"); }
        }
        private static async Task CacheType()
        {
            for (var i = 1; i <= CacheMaxType; i++)
            {
                try
                {
                    Input.ConsoleWrite($"Caching Type {i:00}");
                    await PokeApiV2.GetTypes(new ResourceUri($"api/v2/type/{i}/", true));
                }
                catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Type {i:00}"); }
            }
        }
        private static async Task CacheAbility()
        {
            for (var i = 1; i <= CacheMaxAbility; i++)
            {
                try
                {
                    Input.ConsoleWrite($"Caching Ability {i:000}");
                    await PokeApiV2.GetAbilities(new ResourceUri($"api/v2/ability/{i}/", true));
                }
                catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Ability {i:000}"); }
            }
        }
        private static async Task CacheEggGroup()
        {
            for (var i = 1; i <= CacheMaxEgggroup; i++)
            {
                try
                {
                    Input.ConsoleWrite($"Caching Egg Group {i:00}");
                    await PokeApiV2.GetEggGroups(new ResourceUri($"api/v2/egg-group/{i}/", true));
                }
                catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Egg Group {i:00}"); }
            }
        }

        private static void PreCache()
        {
            Task.WaitAll(
                CacheDoMultiTask(16, CacheMaxPokemon, CachePokemon),
                CacheDoMultiTask(16, CacheMaxPokemon, CachePokemonSpecies),
                CacheDoMultiTask(16, CacheMaxItem, CacheItem),
                CacheType(),
                CacheAbility(),
                CacheEggGroup());
        }
    }
}