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

            var targetPokemons = await FindTarget();
            await Task.Delay(1000);
            if (targetPokemons.Count == 0)
            {
                Logger.Write("target pokemon not found.");
                return;
            }
            await ForceUnban();
            await Task.Delay(1000);
            await CatchTarget(targetPokemons);
        }

        private async static Task<List<MapPokemon>> FindTarget()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var pokemons = await GetNearbyPokemons();
            var lpokemons = pokemons.ToList();
            return lpokemons.Where(p => p.PokemonId == Program.targetPoke).ToList();
        }

        private async static Task ForceUnban()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var pokeStops = await GetPokeStops();
            var closestPokeStop = pokeStops.OrderBy(s =>
                LocationUtils.CalculateDistanceInMeters(
                _session.Client.CurrentLatitude, _session.Client.CurrentLongitude,
                s.Latitude, s.Longitude)).FirstOrDefault();
            await _session.Navigation.Move(
                new GeoCoordinate(closestPokeStop.Latitude, closestPokeStop.Longitude,
                LocationUtils.getElevation(closestPokeStop.Latitude, closestPokeStop.Longitude)),
                120, null, _cancellationToken, true);
            FortSearchResponse fortSearch;
            int counter = 0;
            do
            {
                counter++;
                Logger.Write("Forcing Unbanned: " + counter);
                fortSearch = await _session.Client.Fort.SearchFort(closestPokeStop.Id, closestPokeStop.Latitude, closestPokeStop.Longitude);
            } while (fortSearch.ExperienceAwarded == 0);
            Logger.Write("We are unbanned.");
        }

        private async static Task CatchTarget(IEnumerable<MapPokemon> pokemons)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            foreach (var pokemon in pokemons)
            {
                var encounter =
                    await _session.Client.Encounter.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnPointId);
                await CatchPokemonTask.Execute(_session, _cancellationToken, encounter, pokemon);
            }
        }

        private static async Task<List<MapPokemon>> GetNearbyPokemons()
        {
            var mapObjects = await _session.Client.Map.GetMapObjects();

            var pokemons = mapObjects.Item1.MapCells.SelectMany(i => i.CatchablePokemons)
                .OrderBy(
                    i =>
                        LocationUtils.CalculateDistanceInMeters(_session.Client.CurrentLatitude,
                            _session.Client.CurrentLongitude,
                            i.Latitude, i.Longitude)).ToList();
            
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
                        i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime() &&
                        ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                            LocationUtils.CalculateDistanceInMeters(
                                _session.Settings.DefaultLatitude, _session.Settings.DefaultLongitude,
                                i.Latitude, i.Longitude) < _session.LogicSettings.MaxTravelDistanceInMeters ||
                        _session.LogicSettings.MaxTravelDistanceInMeters == 0)
                );

            return pokeStops.ToList();
        }
    }
}
