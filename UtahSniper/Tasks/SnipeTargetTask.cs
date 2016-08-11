using GeoCoordinatePortable;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;
using PokemonGo.RocketAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtahSniper;

namespace PoGo.NecroBot.Logic.Tasks
{
    class SnipeTargetTask
    {
        static private ISession _session;
        static private CancellationToken _cancellationToken;
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            _session = session;
            _cancellationToken = cancellationToken;

            _cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(6000, _cancellationToken);
            var targetPokemons = await FindTarget();
            if (targetPokemons.Count() == 0)
            {
                Logger.Write("target pokemon not found.", color: ConsoleColor.Red);
                Program.SnipeResult = SnipeEnum.PokemonNotFound;
                return;
            }
            Logger.Write("Target pokemon found: " + targetPokemons.FirstOrDefault().PokemonId, color: ConsoleColor.Green);
            await Task.Delay(6000, _cancellationToken);
            await ForceUnban();
            await Task.Delay(6000, _cancellationToken);
            await CatchTarget(targetPokemons);
        }

        private async static Task<IEnumerable<MapPokemon>> FindTarget()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var mapObjects = await _session.Client.Map.GetMapObjects();
            var pokemons = mapObjects.Item1.MapCells.SelectMany(i => i.CatchablePokemons)
                .OrderBy(
                    i =>
                        LocationUtils.CalculateDistanceInMeters(_session.Client.CurrentLatitude,
                            _session.Client.CurrentLongitude,
                            i.Latitude, i.Longitude));

            return pokemons.Where(p => p.PokemonId == Program.targetPoke);
        }

        private async static Task ForceUnban()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var pokeStops = await GetPokeStops();
            var closestPokeStop = pokeStops.OrderBy(s =>
                LocationUtils.CalculateDistanceInMeters(
                _session.Client.CurrentLatitude, _session.Client.CurrentLongitude,
                s.Latitude, s.Longitude)).FirstOrDefault();
            if (closestPokeStop == null)
            {
                Logger.Write("There is no pokestop to visit.", color: ConsoleColor.Red);
                Program.SnipeResult = SnipeEnum.PokeStopNotFound;
                return;
            }
            Logger.Write("Target pokestop found.", color: ConsoleColor.Green);
            await Task.Delay(6000);
            await _session.Navigation.Move(
                new GeoCoordinate(closestPokeStop.Latitude, closestPokeStop.Longitude,
                LocationUtils.getElevation(closestPokeStop.Latitude, closestPokeStop.Longitude)),
                60, null, _cancellationToken, true);
            FortSearchResponse fortSearch;

            Logger.Write("Now trying to force unbanned.");
            int counter = 0;
            do
            {
                counter++;
                
                fortSearch = await _session.Client.Fort.SearchFort(closestPokeStop.Id, closestPokeStop.Latitude, closestPokeStop.Longitude);
            } while (fortSearch.ExperienceAwarded == 0);
            Logger.Write($"Fuck yes, we are unbanned. ({counter})", color: ConsoleColor.Green);
        }

        private async static Task CatchTarget(IEnumerable<MapPokemon> pokemons)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            Logger.Write("Starting to catch target pokemon.");
            foreach (var pokemon in pokemons)
            {
                var encounter =
                    await _session.Client.Encounter.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnPointId);
                await Task.Delay(3000);
                Program.SnipeResult = await UtahCatchPokemonTask.Execute(_session, _cancellationToken, encounter, pokemon);
            }
        }

        private static async Task<IEnumerable<MapPokemon>> GetNearbyPokemons()
        {
            var mapObjects = await _session.Client.Map.GetMapObjects();

            var pokemons = mapObjects.Item1.MapCells.SelectMany(i => i.CatchablePokemons);
            
            return pokemons;
        }

        private static async Task<List<FortData>> GetPokeStops()
        {
            var mapObjects = await _session.Client.Map.GetMapObjects();

            // Wasn't sure how to make this pretty. Edit as needed.
            var pokeStops = mapObjects.Item1.MapCells.SelectMany(i => i.Forts)
                .Where(
                    i =>
                        i.Type == FortType.Checkpoint &&
                        i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()
                );

            return pokeStops.ToList();
        }
    }
}
