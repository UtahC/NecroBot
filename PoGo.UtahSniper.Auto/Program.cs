using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UtahSniper.Auto
{

    public class Pokesnipers
    {
        public Result[] results { get; set; }
    }

    public class Result
    {
        public int id { get; set; }
        public string name { get; set; }
        public string coords { get; set; }
        public DateTime until { get; set; }
        public int iv { get; set; }
        public object[] attacks { get; set; }
        public string icon { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            DateTime scannedTime = new DateTime(1970, 1, 1);
            while (true)
            {
                var content = GetResponseString(new Uri("http://pokesnipers.com//api/v1/pokemon.json?referrer=home"));
                var pokesnipers = JsonConvert.DeserializeObject(content, new Pokesnipers().GetType()) as Pokesnipers;
                var pokemons = pokesnipers.results.Where(p => p.until > scannedTime).OrderBy(p => p.until).ToList();

                if (pokemons.Count == 0) Thread.Sleep(10000);

                foreach (var pokemon in pokemons)
                {
                    Process process = null;
                    if (IsRare(pokemon) || IsHighIV(pokemon))
                    {
                        Console.WriteLine($"There is a {pokemon.name} with {pokemon.iv}% IV at ({pokemon.coords}). Starting to catch it.");
                        string string4Sniper2 = $"pokesniper2://{pokemon.name}/{pokemon.coords}";
                        //SnipeEnum result = (SnipeEnum)UtahSniper.Program.Main(new string[] { string4Sniper2 });
                        process = Process.Start(AppDomain.CurrentDomain.BaseDirectory + "\\UtahSniper.exe", string4Sniper2);
                        scannedTime = pokemon.until;
                        Thread.Sleep(5000);
                    }
                    while (Process.GetProcessesByName("UtahSniper").Count() > 0)
                        Thread.Sleep(1000);
                    if (process != null)
                        Console.WriteLine((SnipeEnum)process.ExitCode);
                    if (pokemon.until < DateTime.UtcNow)
                        break;
                }
            }
        }

        private static bool IsHighIV(Result pokemon)
        {
            if (pokemon.iv >= 90) return true;
            return false;
        }

        private static bool IsRare(Result pokemon)
        {
            switch (pokemon.name)
            {
                case "Snorlax": return true;//卡比獸
                case "Chansey": return true;//吉利蛋
                case "Blastoise": return true;//水箭龜
                case "Venusaur": return true;//妙蛙花
                case "Charizard": return true;//噴火龍
                case "Lapras": return true;
                case "Dragonite": return true;
                case "Dragonair": return true;
                case "Dratini": return true;
                case "Gyarados": return true;
                case "Ditto": return true;
                case "Articuno": return true;//冰鳥
                case "Zapdos": return true;//電鳥
                case "Moltres": return true;//火鳥
                case "Mew": return true;
                case "Mewtwo": return true;
                default: return false;
            }
        }

        private static string GetResponseString(Uri uri)
        {
            HttpClient client = new HttpClient();
            var task = Task.Run(() => client.GetStringAsync(uri));
            task.Wait();

            return task.Result;
        }
    }
}
