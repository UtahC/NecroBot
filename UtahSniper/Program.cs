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
        public static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "--registerurl")
                {
                    RegisterUrl();
                    return (int)SnipeEnum.RegisterUrlSuccess;
                }
                else if (args[0].ToLower() == "--removeurl")
                {
                    RemoveUrl();
                    return (int)SnipeEnum.RemoveUrlSuccess;
                }
            }

            Logger.SetLogger(new ConsoleLogger(LogLevel.LevelUp), subPath);

            var machine = new StateMachine();
            var stats = new Statistics();
            var profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subPath);
            var profileConfigPath = Path.Combine(profilePath, "Config");
            var configFile = Path.Combine(profileConfigPath, "config.json");
            var parseSuccess = Enum.TryParse(args[0].Substring(14).Split('/')[0].Replace("'", "").Replace(" ", "").Replace(".", ""), out targetPoke);
            lat = double.Parse(args[0].Substring(14).Split('/')[1].Split(',')[0].Split('(').LastOrDefault().Split(')').LastOrDefault());
            lng = double.Parse(args[0].Substring(14).Split('/')[1].Split(',')[1].Split('(').LastOrDefault().Split(')').LastOrDefault());

            Console.WriteLine("Argument: " + args[0]);
            Console.WriteLine($"Target pokemon: {targetPoke} ({lat},{lng})");

            GlobalSettings settings;

            if (File.Exists(configFile) && parseSuccess)
            {
                settings = GlobalSettings.Load(subPath);
            }
            else
            {
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
                machine.AsyncStart(new UtahLoginState(), session);
            }
            Task.Run(() => 
            {
                while (true)
                {
                    if (SnipeResult != SnipeEnum.Unknow)
                        QuitEvent.Set();
                    QuitEvent.WaitOne(1000);
                }
            });
            QuitEvent.WaitOne();
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
