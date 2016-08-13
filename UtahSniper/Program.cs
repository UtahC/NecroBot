using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic;
using PoGo.NecroBot.Logic.Common;
using PoGo.NecroBot.Logic.Logging;
using System.Reflection;
using PoGo.NecroBot.Logic.State;
using System.IO;
using Microsoft.Win32;
using POGOProtos.Enums;
using PoGo.NecroBot.Logic.Utils;
using System.Threading;

namespace UtahSniper
{
    public enum SnipeEnum
    {
        Unknow,
        PokemonCaught,
        PokemonFlee,
        PokemonCatchError,
        PokemonCatchNotSure,
        PokemonNotFound,
        PokeStopNotFound,
        RegisterUrlSuccess,
        ConfigFileNotFoundOrArgumentParsedError,
        RemoveUrlSuccess
    }
    public class Program
    {
        public static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);
        private static string subPath = "";
        public static PokemonId targetPoke = PokemonId.Missingno;
        public static double lat = 0, lng = 0;
        public static SnipeEnum SnipeResult = SnipeEnum.Unknow;
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "--registerurl")
                {
                    RegisterUrl();
                    return ;
                }
                else if (args[0].ToLower() == "--removeurl")
                {
                    RemoveUrl();
                    return ;
                }
            }
            Logger.SetLogger(new ConsoleLogger(LogLevel.LevelUp), "");
            int result = 0;
            Task.Run(async () => result = await Excute(args)).Wait();
            while (SnipeResult == SnipeEnum.Unknow) Thread.Sleep(1000);
            Console.WriteLine((SnipeEnum)result);
            QuitEvent.WaitOne();
        }

        public async static Task<int> Excute(string[] args)
        {
            if (args.Length == 1)
                args = args[0].Split(' ');
            
            
            if (args.Length > 1)
            {
                subPath += args[1];
            }

            Console.WriteLine(args[0]);
            Console.WriteLine(subPath);

            //Logger.SetLogger(new ConsoleLogger(LogLevel.LevelUp), subPath);

            var machine = new StateMachine();
            var stats = new Statistics();
            var profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subPath);
            var profileConfigPath = Path.Combine(profilePath, "Config");
            var configFile = Path.Combine(profileConfigPath, "config.json");
            //Console.WriteLine(args[0]);
            //Console.WriteLine(args[1]);
            //Console.WriteLine("poke = " + args[0].Substring(14).Split('/')[0].Replace("'", "").Replace(" ", "").Replace(".", ""));
            //Console.WriteLine("lat = " + args[0].Substring(14).Split('/')[1].Split(',')[0].Split('(').LastOrDefault().Split(')').LastOrDefault());
            //Console.WriteLine("lon = " + args[0].Substring(14).Split('/')[1].Split(',')[1].Split('(').LastOrDefault().Split(')').LastOrDefault());
            //while (true) ;
            var parseSuccess = Enum.TryParse(args[0].Substring(14).Split('/')[0].Replace("'", "").Replace(" ", "").Replace(".", ""), out targetPoke);
            lat = double.Parse(args[0].Substring(14).Split('/')[1].Split(',')[0].Split('(').LastOrDefault().Split(')').LastOrDefault());
            lng = double.Parse(args[0].Substring(14).Split('/')[1].Split(',')[1].Split('(').LastOrDefault().Split(')').LastOrDefault());

            GlobalSettings settings;

            if (File.Exists(configFile) && parseSuccess)
            {
                settings = GlobalSettings.Load(subPath);
                Console.WriteLine(settings.Auth.GoogleUsername);
                Console.WriteLine(settings.Auth.PtcUsername);
            }
            else
            {
                Console.WriteLine("path = " + configFile);
                Console.WriteLine("File.Exitsts = " + File.Exists(configFile));
                Console.WriteLine("parseSuccess = " + parseSuccess);
                Console.ReadKey();
                return (int)SnipeEnum.ConfigFileNotFoundOrArgumentParsedError;
            }

            if (args.Length > 0)
            {
                try
                {
                    settings.DefaultLatitude = lat;
                    settings.DefaultLongitude = lng;
                }
                catch (Exception) { }
                var session = new Session(new ClientSettings(settings), new LogicSettings(settings));

                session.Client.ApiFailure = new ApiFailureStrategy(session);
                var aggregator = new StatisticsAggregator(stats);
                var listener = new ConsoleEventListener();
                session.EventDispatcher.EventReceived += evt => listener.Listen(evt, session);
                session.EventDispatcher.EventReceived += evt => aggregator.Listen(evt, session);
                machine.SetFailureState(new UtahLoginState());
                Logger.SetLoggerContext(session);
                await machine.AsyncStart(new UtahLoginState(), session);
            }
            Logger.Write("done");
            return (int)SnipeResult;
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

            Registry.SetValue("HKEY_CLASSES_ROOT\\pokesniper2\\DefaultIcon", null, "UtahSniper.exe");
            Registry.SetValue("HKEY_CLASSES_ROOT\\pokesniper2\\Shell\\Open\\Command", null, $"\"{AppDomain.CurrentDomain.BaseDirectory}\\UtahSniper.exe\" \"%1\"");
        }
    }
}
