using System;
using System.Threading.Tasks;

using Aragas.Core.Wrappers;

using PokeD.Core.Data.PokeApi;

namespace PokeD.Server
{
    public partial class Server
    {
        private const int MaxPokemon = 721;
        private const int MaxItem = 749;
        private const int MaxType = 18;
        private const int MaxAbility = 190;
        private const int MaxEgggroup = 15;

        private static async Task MultiTask(int size, int max, Func<int, Task> func)
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
                InputWrapper.ConsoleWrite($"Caching Pokemon {index.ToString("000")}");
                await PokeApiV2.GetPokemon(new ResourceUri($"api/v2/pokemon/{index}/", true));
            }
            catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Pokemon {index.ToString("000")}"); }
        }
        private static async Task CachePokemonSpecies(int index)
        {
            try
            {
                InputWrapper.ConsoleWrite($"Caching Pokemon Species {index.ToString("000")}");
                await PokeApiV2.GetPokemonSpecies(new ResourceUri($"api/v2/pokemon-species/{index}/", true));
            }
            catch (Exception)
            {
                Logger.Log(LogType.Warning, $"Failed Caching Pokemon Species {index.ToString("000")}");
            }
        }
        private static async Task CacheItem(int index)
        {
            try
            {
                InputWrapper.ConsoleWrite($"Caching Item {index.ToString("000")}");
                await PokeApiV2.GetItems(new ResourceUri($"api/v2/item/{index}/", true));
            }
            catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Item {index.ToString("000")}"); }
        }
        private static async Task CacheType()
        {
            for (var i = 1; i <= MaxType; i++)
            {
                try
                {
                    InputWrapper.ConsoleWrite($"Caching Type {i.ToString("00")}");
                    await PokeApiV2.GetTypes(new ResourceUri($"api/v2/type/{i}/", true));
                }
                catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Type {i.ToString("00")}"); }
            }
        }
        private static async Task CacheAbility()
        {
            for (var i = 1; i <= MaxAbility; i++)
            {
                try
                {
                    InputWrapper.ConsoleWrite($"Caching Ability {i.ToString("000")}");
                    await PokeApiV2.GetAbilities(new ResourceUri($"api/v2/ability/{i}/", true));
                }
                catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Ability {i.ToString("000")}"); }
            }
        }
        private static async Task CacheEggGroup()
        {
            for (var i = 1; i <= MaxEgggroup; i++)
            {
                try
                {
                    InputWrapper.ConsoleWrite($"Caching Egg Group {i.ToString("00")}");
                    await PokeApiV2.GetEggGroups(new ResourceUri($"api/v2/egg-group/{i}/", true));
                }
                catch (Exception) { Logger.Log(LogType.Warning, $"Failed Caching Egg Group {i.ToString("00")}"); }
            }
        }

        private static void PreCache()
        {
            Task.WaitAll(
                MultiTask(16, MaxPokemon, CachePokemon),
                MultiTask(16, MaxPokemon, CachePokemonSpecies),
                MultiTask(16, MaxItem, CacheItem),
                CacheType(), CacheAbility(), CacheEggGroup());
        }
    }
}
