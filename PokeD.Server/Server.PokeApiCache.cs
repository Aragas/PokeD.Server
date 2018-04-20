using System;
using System.Threading.Tasks;

using PokeD.Core;
using PokeD.Core.Data.PokeApi;

namespace PokeD.Server
{
    public partial class Server
    {
        private const int CacheMaxPokemon = 721;
        private const int CacheMaxMove = 621;
        private const int CacheMaxItem = 749;
        private const int CacheMaxType = 18;
        private const int CacheMaxAbility = 190;
        private const int CacheMaxEggGroup = 15;

        private static async Task CacheDoMultiTask(int size, int max, Func<int, Task> func)
        {
            int index;
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
                Logger.Log(LogType.Info, $"Caching Pokemon {index:000}");
                await PokeApiV2.GetPokemonAsync(new ResourceUri($"api/v2/pokemon/{index}/", true));
            }
            catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Pokemon {index:000}"); }
        }
        private static async Task CachePokemonSpecies(int index)
        {
            try
            {
                Logger.Log(LogType.Info, $"Caching Pokemon Species {index:000}");
                await PokeApiV2.GetPokemonSpeciesAsync(new ResourceUri($"api/v2/pokemon-species/{index}/", true));
            }
            catch (Exception)
            {
                Logger.Log(LogType.Warning, $"Failed Caching Pokemon Species {index:000}");
            }
        }
        private static async Task CacheMove(int index)
        {
            try
            {
                Logger.Log(LogType.Info, $"Caching Move {index:000}");
                await PokeApiV2.GetItemsAsync(new ResourceUri($"api/v2/move/{index}/", true));
            }
            catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Move {index:000}"); }
        }
        private static async Task CacheItem(int index)
        {
            try
            {
                Logger.Log(LogType.Info, $"Caching Item {index:000}");
                await PokeApiV2.GetItemsAsync(new ResourceUri($"api/v2/item/{index}/", true));
            }
            catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Item {index:000}"); }
        }
        private static async Task CachePokemonType()
        {
            for (var i = 1; i <= CacheMaxType; i++)
            {
                try
                {
                    Logger.Log(LogType.Info, $"Caching Type {i:00}");
                    await PokeApiV2.GetTypesAsync(new ResourceUri($"api/v2/type/{i}/", true));
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
                    Logger.Log(LogType.Info, $"Caching Ability {i:000}");
                    await PokeApiV2.GetAbilitiesAsync(new ResourceUri($"api/v2/ability/{i}/", true));
                }
                catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Ability {i:000}"); }
            }
        }
        private static async Task CacheEggGroup()
        {
            for (var i = 1; i <= CacheMaxEggGroup; i++)
            {
                try
                {
                    Logger.Log(LogType.Info, $"Caching Egg Group {i:00}");
                    await PokeApiV2.GetEggGroupsAsync(new ResourceUri($"api/v2/egg-group/{i}/", true));
                }
                catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Egg Group {i:00}"); }
            }
        }

        private static void PreCache()
        {
            Task.WaitAll(
                CacheDoMultiTask(16, CacheMaxPokemon, CachePokemon),
                CacheDoMultiTask(16, CacheMaxPokemon, CachePokemonSpecies),
                CacheDoMultiTask(16, CacheMaxMove, CacheMove),
                CacheDoMultiTask(16, CacheMaxItem, CacheItem),
                CachePokemonType(),
                CacheAbility(),
                CacheEggGroup());
        }
    }
}