using CloudFlareUtilities;
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PoGo.NecroBot.Logic;
using PoGo.UtahSniper.Auto.Models;
using POGOProtos.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoGo.UtahSniper.Auto
{
    class Program
    {
        private static ArrayList SnipeLocations = new ArrayList();
        private static DateTime _lastSnipe = DateTime.MinValue;

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
                else if (args[0].Contains("pokesniper2://"))
                {
                    var lines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "subpaths.txt");
                    foreach (var line in lines)
                    {
                        Process.Start(AppDomain.CurrentDomain.BaseDirectory + "UtahSniper.exe", args[0] + " " + line);
                        //Task.Run(() => UtahSniper.Program.Excute(new string[] { args[0], line }));
                    }
                }
                else
                    AutoSnipe(args[0]);
            }
        }

        private static void AutoSnipe(string arg)
        {
            var profile = ReadProfile(arg);

            Task.Run(() => ConnectToFeeder());
            while (true)
            {
                var pokemons = ArrayList.Synchronized(SnipeLocations).Clone() as ArrayList;

                foreach (SniperInfo pokemon in pokemons)
                {
                    if (pokemon == null ||
                        profile.isSniping ||
                        profile.LocsVisited.Contains(new PokemonLocation(pokemon.Latitude, pokemon.Longitude)) ||
                        !profile.wantedPoke.Contains(pokemon.Id) ||
                        profile.lastSnipingStartTime.AddMinutes(5) > DateTime.Now)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                        

                    string string4Sniper2 = $"pokesniper2://{pokemon.Id}/{pokemon.Latitude},{pokemon.Longitude}";

                    Console.WriteLine($"There is a {pokemon.Id} at ({pokemon.Latitude},{pokemon.Longitude}). Starting to catch it.");
                    profile.isSniping = true;
                    Task.Run(() =>
                    {
                        Task<int> task;
                        int tryCount = 0;
                        profile.lastSnipingStartTime = DateTime.Now;
                        do
                        {
                            tryCount++;
                            var cancellationTokenSource = new CancellationTokenSource();
                            task = Task.Run(() => UtahSniper.Program.Excute(new string[] { string4Sniper2, profile.subpath }, cancellationTokenSource.Token), cancellationTokenSource.Token);
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();
                            while (!task.IsCompleted)
                            {
                                if (stopwatch.ElapsedMilliseconds > 150000)
                                    cancellationTokenSource.Cancel();
                                Thread.Sleep(1000);
                            }
                            Console.WriteLine($"[{profile.subpath}] {(SnipeEnum)task.Result}");
                        } while ((task.Result == (int)SnipeEnum.PokemonNotFound || task.Result == (int)SnipeEnum.PokeStopNotFound) && tryCount < 2);
                        if (!profile.LocsVisited.Contains(new PokemonLocation(pokemon.Latitude, pokemon.Longitude)))
                            profile.LocsVisited.Add(new PokemonLocation(pokemon.Latitude, pokemon.Longitude));
                        profile.isSniping = false;
                    });
                }
            }
        }

        private static Profile ReadProfile(string subpath)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles", subpath, "PokeWanted.txt");
            var content = File.ReadAllText(path);
            return new Profile(subpath, content);
        }

        private static void ConnectToFeeder()
        {
            try
            {
                var lClient = new TcpClient();
                lClient.Connect("localhost", 16969);

                var sr = new StreamReader(lClient.GetStream());

                while (lClient.Connected)
                {
                    try
                    {
                        var line = sr.ReadLine();
                        if (line == null)
                            throw new Exception("Unable to ReadLine from sniper socket");

                        var info = JsonConvert.DeserializeObject<SniperInfo>(line);

                        bool isLocationExists = false;
                        var snipeLocations = ArrayList.Synchronized(SnipeLocations).Clone() as ArrayList;
                        foreach (SniperInfo loc in snipeLocations)
                        {
                            if (Math.Abs(loc.Latitude - info.Latitude) < 0.0001 && Math.Abs(loc.Longitude - info.Longitude) < 0.0001)
                            {
                                isLocationExists = true;
                                break;
                            }
                            if (loc.ExpirationTimestamp < DateTime.Now || loc.TimeStampAdded.AddMinutes(15) < DateTime.Now)
                                ArrayList.Synchronized(SnipeLocations).Remove(loc);
                        }
                        if (isLocationExists)
                            continue;
                        //if (SnipeLocations().Any(x =>
                        //    Math.Abs(x.Latitude - info.Latitude) < 0.0001 &&
                        //    Math.Abs(x.Longitude - info.Longitude) < 0.0001))
                        //    // we might have different precisions from other sources
                        //    continue;

                        //SnipeLocations.RemoveAll(x => _lastSnipe > x.TimeStampAdded);
                        //SnipeLocationsOperate().RemoveAll(x => x.ExpirationTimestamp < DateTime.Now);
                        //SnipeLocationsOperate().RemoveAll(x => DateTime.Now > x.TimeStampAdded.AddMinutes(15));
                        ArrayList.Synchronized(SnipeLocations).Add(info);
                    }
                    catch (System.IO.IOException)
                    {
                        Console.WriteLine("The connection to the sniping location server was lost.");
                    }
                }
            }
            catch (SocketException)
            {
                // this is spammed to often. Maybe add it to debug log later
            }
            catch (Exception ex)
            {
                // most likely System.IO.IOException
                Console.WriteLine(ex.ToString());
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
                case PokemonId.Golbat: return true;

                case PokemonId.Snorlax: return true;//卡比獸
                //case PokemonId.Chansey: return true;//吉利蛋
                //case PokemonId.Blastoise: return true;//水箭龜
                //case PokemonId.Venusaur: return true;//妙蛙花
                case PokemonId.Charizard: return true;//噴火龍
                case PokemonId.Lapras: return true;
                case PokemonId.Dragonite: return true;
                //case PokemonId.Dragonair: return true;
                //case PokemonId.Dratini: return true;
                case PokemonId.Gyarados: return true;
                //case PokemonId.Ditto: return true;
                case PokemonId.Articuno: return true;//冰鳥
                case PokemonId.Zapdos: return true;//電鳥
                case PokemonId.Moltres: return true;//火鳥
                case PokemonId.Mew: return true;
                case PokemonId.Mewtwo: return true;
                default: return false;
            }
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
