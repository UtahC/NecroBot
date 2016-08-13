using CloudFlareUtilities;
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PoGo.NecroBot.Logic;
using POGOProtos.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UtahSniper.Auto
{
    public class SniperInfo
    {
        public ulong EncounterId { get; set; }
        public DateTime ExpirationTimestamp { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public PokemonId Id { get; set; }
        public string SpawnPointId { get; set; }
        public PokemonMove Move1 { get; set; }
        public PokemonMove Move2 { get; set; }
        public double IV { get; set; }

        [JsonIgnore]
        public DateTime TimeStampAdded { get; set; } = DateTime.Now;
    }

    public class PokemonLocation
    {
        public PokemonLocation(double lat, double lon)
        {
            latitude = lat;
            longitude = lon;
        }

        public long Id { get; set; }
        public double expires { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public int pokemon_id { get; set; }
        public PokemonId pokemon_name { get; set; }

        [JsonIgnore]
        public DateTime TimeStampAdded { get; set; } = DateTime.Now;

        public bool Equals(PokemonLocation obj)
        {
            return Math.Abs(latitude - obj.latitude) < 0.0001 && Math.Abs(longitude - obj.longitude) < 0.0001;
        }

        public override bool Equals(object obj) // contains calls this here
        {
            var p = obj as PokemonLocation;
            if (p == null) // no cast available
            {
                return false;
            }

            return Math.Abs(latitude - p.latitude) < 0.0001 && Math.Abs(longitude - p.longitude) < 0.0001;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return latitude.ToString("0.00000000000") + ", " + longitude.ToString("0.00000000000");
        }
    }

    public class PokemonLocation_pokezz
    {

        public double time { get; set; }
        public double lat { get; set; }
        public double lng { get; set; }
        public string iv { get; set; }
        public double _iv
        {
            get
            {
                try
                {
                    return Convert.ToDouble(iv, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return 0;
                }
            }
        }
        public PokemonId name { get; set; }
        public Boolean verified { get; set; }
    }

    public class PokemonLocation_pokesnipers
    {
        public int id { get; set; }
        public double iv { get; set; }
        public PokemonId name { get; set; }
        public string until { get; set; }
        public string coords { get; set; }
    }

    public class PokemonLocation_pokewatchers
    {
        public PokemonId pokemon { get; set; }
        public double timeadded { get; set; }
        public double timeend { get; set; }
        public string cords { get; set; }
    }

    public class ScanResult
    {
        public string Status { get; set; }
        public List<PokemonLocation> pokemons { get; set; }
    }

    public class ScanResult_pokesnipers
    {
        public string Status { get; set; }
        [JsonProperty("results")]
        public List<PokemonLocation_pokesnipers> pokemons { get; set; }
    }

    public class ScanResult_pokewatchers
    {
        public string Status { get; set; }
        public List<PokemonLocation_pokewatchers> pokemons { get; set; }
    }

    class Program
    {
        private static readonly List<SniperInfo> SnipeLocations = new List<SniperInfo>();
        public static List<PokemonLocation> LocsVisited = new List<PokemonLocation>();

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "--registerurl")
                {
                    RegisterUrl();
                    return;
                }
                else if (args[0].ToLower() == "--removeurl")
                {
                    RemoveUrl();
                    return;
                }

                var lines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "subpaths.txt");
                foreach (var line in lines)
                {
                    Process.Start(AppDomain.CurrentDomain.BaseDirectory + "UtahSniper.exe", args[0] + " " + line);
                    //Task.Run(() => UtahSniper.Program.Excute(new string[] { args[0], line }));
                }
            }
            else
            {
                clawler();
                return;
            }
        }

        private static void Skiplagged(string[] args)
        {
            var lines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "subpaths.txt");
            var dic = new Dictionary<string, Process>();
            while (true)
            {
                List<PokemonId> pokemonIds = new List<PokemonId>() { PokemonId.Dragonair, PokemonId.Dratini, PokemonId.Dragonite };
                var location = new Location();
                var scanResult = SnipeScanForPokemon(location);
                var locationsToSnipe = new List<PokemonLocation>();
                if (scanResult.pokemons != null)
                {
                    var filteredPokemon = scanResult.pokemons.Where(q => pokemonIds.Contains(q.pokemon_name));
                    var notVisitedPokemon = filteredPokemon.Where(q => !LocsVisited.Contains(q));
                    var notExpiredPokemon = notVisitedPokemon.Where(q => q.expires < (DateTime.Now.ToUniversalTime() - (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalMilliseconds);

                    if (notExpiredPokemon.Any())
                        locationsToSnipe.AddRange(notExpiredPokemon);
                }

                var _locationsToSnipe = locationsToSnipe.OrderBy(q => q.expires).ToList();

                if (_locationsToSnipe.Any())
                {
                    foreach (var pokemonLocation in _locationsToSnipe)
                    {
                        if (!LocsVisited.Contains(new PokemonLocation(pokemonLocation.latitude, pokemonLocation.longitude)))
                        {
                            var snipeurl = @"pokesniper2://Dratini/" + $"{pokemonLocation.latitude},{pokemonLocation.longitude}";
                            foreach (var line in lines)
                            {
                                if (dic.ContainsKey(line))
                                {
                                    dic[line].Kill();
                                    dic.Remove(line);
                                }
                                var process = Process.Start(AppDomain.CurrentDomain.BaseDirectory + "UtahSniper.exe", args[0] + " " + line);
                                dic.Add(line, process);
                            }
                            Task.Delay(180000);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(scanResult.Status) && scanResult.Status.Contains("fail"))
                    Console.WriteLine("skiplagged is down now.");
                else
                    Console.WriteLine("there is no pokemon to snipe.");
            }
        }

        private static void clawler()
        {
            while (true)
            {
                var pokemons = GetSniperInfoFrom_pokesnipers();

                foreach (var pokemon in pokemons)
                {
                    if (IsRare(pokemon) || IsHighIV(pokemon))
                    {
                        if (pokemon.ExpirationTimestamp < DateTime.UtcNow)
                            break;
                        Console.WriteLine($"There is a {pokemon.Id} at ({pokemon.Latitude},{pokemon.Longitude}). Starting to catch it.");
                        string string4Sniper2 = $"pokesniper2://{pokemon.Id}/{pokemon.Latitude},{pokemon.Longitude}";
                        //SnipeEnum result = (SnipeEnum)UtahSniper.Program.Main(new string[] { string4Sniper2 });
                        var task = Task.Run(() => UtahSniper.Program.Excute(new string[] { string4Sniper2 }));
                        
                        while (!task.IsCompleted) Thread.Sleep(1000);
                        Console.WriteLine((SnipeEnum)task.Result);
                    }

                    if (!LocsVisited.Contains(new PokemonLocation(pokemon.Latitude, pokemon.Longitude)))
                        LocsVisited.Add(new PokemonLocation(pokemon.Latitude, pokemon.Longitude));
                }
                Thread.Sleep(10000);
            }
        }

        private static bool IsHighIV(SniperInfo pokemon)
        {
            if (pokemon.IV >= 90) return true;
            return false;
        }

        private static bool IsRare(SniperInfo pokemon)
        {
            switch (pokemon.Id)
            {
                //case PokemonId.Snorlax: return true;//卡比獸
                //case PokemonId.Chansey: return true;//吉利蛋
                //case PokemonId.Blastoise: return true;//水箭龜
                //case PokemonId.Venusaur: return true;//妙蛙花
                //case PokemonId.Charizard: return true;//噴火龍
                case PokemonId.Lapras: return true;
                //case PokemonId.Dragonite: return true;
                //case PokemonId.Dragonair: return true;
                //case PokemonId.Dratini: return true;
                //case PokemonId.Gyarados: return true;
                //case PokemonId.Ditto: return true;
                //case PokemonId.Articuno: return true;//冰鳥
                //case PokemonId.Zapdos: return true;//電鳥
                //case PokemonId.Moltres: return true;//火鳥
                //case PokemonId.Mew: return true;
                //case PokemonId.Mewtwo: return true;
                default: return false;
            }
        }

        private static List<SniperInfo> GetSniperInfoFrom_pokesnipers(List<PokemonId> pokemonIds = null)
        {

            var uri = $"http://pokesnipers.com/api/v1/pokemon.json";

            ScanResult_pokesnipers scanResult_pokesnipers;
            try
            {
                var handler = new ClearanceHandler();

                // Create a HttpClient that uses the handler.
                var client = new HttpClient(handler);

                // Use the HttpClient as usual. Any JS challenge will be solved automatically for you.
                var fullresp = client.GetStringAsync(uri).Result.Replace(" M", "Male").Replace(" F", "Female").Replace("Farfetch'd", "Farfetchd").Replace("Mr.Maleime", "MrMime");

                scanResult_pokesnipers = JsonConvert.DeserializeObject<ScanResult_pokesnipers>(fullresp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("(PokeSnipers.com) " + ex.Message);
                return null;
            }
            if (scanResult_pokesnipers.pokemons != null)
            {
                foreach (var pokemon in scanResult_pokesnipers.pokemons)
                {
                    try
                    {
                        var SnipInfo = new SniperInfo();
                        SnipInfo.Id = pokemon.name;
                        string[] coordsArray = pokemon.coords.Split(',');
                        SnipInfo.Latitude = Convert.ToDouble(coordsArray[0], CultureInfo.InvariantCulture);
                        SnipInfo.Longitude = Convert.ToDouble(coordsArray[1], CultureInfo.InvariantCulture);
                        SnipInfo.TimeStampAdded = DateTime.Now;
                        SnipInfo.ExpirationTimestamp = Convert.ToDateTime(pokemon.until);
                        SnipInfo.IV = pokemon.iv;
                        SnipeLocations.Add(SnipInfo);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                var locationsToSnipe = SnipeLocations?.Where(q =>
                    !LocsVisited.Contains(new PokemonLocation(q.Latitude, q.Longitude))
                    && !(q.ExpirationTimestamp != default(DateTime) &&
                    q.ExpirationTimestamp > new DateTime(2016) &&
                    // make absolutely sure that the server sent a correct datetime
                    q.ExpirationTimestamp < DateTime.Now)).ToList() ??
                    new List<SniperInfo>();

                return locationsToSnipe.OrderBy(q => q.ExpirationTimestamp).ToList();
            }
            else
                return null;
        }

        private static List<SniperInfo> GetSniperInfoFrom_pokewatchers()
        {

            var uri = $"http://pokewatchers.com/api.php?act=grab";

            ScanResult_pokewatchers scanResult_pokewatchers;
            try
            {
                var handler = new ClearanceHandler();

                // Create a HttpClient that uses the handler.
                var client = new HttpClient(handler);

                // Use the HttpClient as usual. Any JS challenge will be solved automatically for you.
                var fullresp = "{ \"pokemons\":" + client.GetStringAsync(uri).Result.Replace(" M", "Male").Replace(" F", "Female").Replace("Farfetch'd", "Farfetchd").Replace("Mr.Maleime", "MrMime") + "}";

                scanResult_pokewatchers = JsonConvert.DeserializeObject<ScanResult_pokewatchers>(fullresp);
            }
            catch (Exception ex)
            {
                // most likely System.IO.IOException
                Console.WriteLine("(PokeWatchers.com) " + ex.Message);
                return null;
            }
            if (scanResult_pokewatchers.pokemons != null)
            {
                foreach (var pokemon in scanResult_pokewatchers.pokemons)
                {
                    try
                    {
                        var SnipInfo = new SniperInfo();
                        SnipInfo.Id = pokemon.pokemon;
                        string[] coordsArray = pokemon.cords.Split(',');
                        SnipInfo.Latitude = Convert.ToDouble(coordsArray[0], CultureInfo.InvariantCulture);
                        SnipInfo.Longitude = Convert.ToDouble(coordsArray[1], CultureInfo.InvariantCulture);
                        SnipInfo.TimeStampAdded = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(pokemon.timeadded);
                        SnipInfo.ExpirationTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(pokemon.timeend);
                        SnipeLocations.Add(SnipInfo);
                    }
                    catch
                    {
                    }
                }
                var locationsToSnipe = SnipeLocations?.Where(q =>
                    !LocsVisited.Contains(new PokemonLocation(q.Latitude, q.Longitude))
                    && !(q.ExpirationTimestamp != default(DateTime) &&
                    q.ExpirationTimestamp > new DateTime(2016) &&
                    // make absolutely sure that the server sent a correct datetime
                    q.ExpirationTimestamp < DateTime.Now)).ToList() ??
                    new List<SniperInfo>();

                return locationsToSnipe.OrderBy(q => q.ExpirationTimestamp).ToList();
            }
            else
                return null;
        }

        private static ScanResult SnipeScanForPokemon(Location location)
        {
            var formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };

            var offset = 0.06;
            // 0.003 = half a mile; maximum 0.06 is 10 miles
            if (offset < 0.001) offset = 0.003;
            if (offset > 0.06) offset = 0.06;

            var boundLowerLeftLat = location.Latitude - offset;
            var boundLowerLeftLng = location.Longitude - offset;
            var boundUpperRightLat = location.Latitude + offset;
            var boundUpperRightLng = location.Longitude + offset;

            var uri =
                $"http://skiplagged.com/api/pokemon.php?bounds={boundLowerLeftLat.ToString(formatter)},{boundLowerLeftLng.ToString(formatter)},{boundUpperRightLat.ToString(formatter)},{boundUpperRightLng.ToString(formatter)}";

            ScanResult scanResult;
            try
            {
                var request = WebRequest.CreateHttp(uri);
                request.Accept = "application/json";
                request.UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36\r\n";
                request.Method = "GET";
                request.Timeout = 15000;
                request.ReadWriteTimeout = 32000;

                var resp = request.GetResponse();
                var reader = new StreamReader(resp.GetResponseStream());
                var fullresp = reader.ReadToEnd().Replace(" M", "Male").Replace(" F", "Female").Replace("Farfetch'd", "Farfetchd").Replace("Mr.Maleime", "MrMime");

                scanResult = JsonConvert.DeserializeObject<ScanResult>(fullresp);
            }
            catch (Exception ex)
            {
                // most likely System.IO.IOException
                scanResult = new ScanResult
                {
                    Status = "fail",
                    pokemons = new List<PokemonLocation>()
                };
            }
            var a = scanResult.pokemons.ToList();
            return scanResult;
        }

        private static void RemoveUrl()
        {
            Registry.ClassesRoot.DeleteSubKeyTree("pokesniper2");
        }

        private static void RegisterUrl()
        {
            if (Registry.ClassesRoot.GetSubKeyNames().Contains("pokesniper2"))
                RemoveUrl();


            var reg = Registry.ClassesRoot.CreateSubKey("pokesniper2");
            reg.CreateSubKey("DefaultIcon");
            reg.CreateSubKey("Shell").CreateSubKey("Open").CreateSubKey("Command");

            Registry.SetValue("HKEY_CLASSES_ROOT\\pokesniper2\\DefaultIcon", null, "PoGo.UtahSniper.Auto.exe");
            Registry.SetValue("HKEY_CLASSES_ROOT\\pokesniper2\\Shell\\Open\\Command", null, $"\"{AppDomain.CurrentDomain.BaseDirectory}\\PoGo.UtahSniper.Auto.exe\" \"%1\"");
        }
    }
}
