using PoGo.NecroBot.Logic;
using PoGo.NecroBot.Logic.Common;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;
using POGOProtos.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoGo.UtahSniper
{
    public class Sniper
    {
        public PokemonId targetPoke = PokemonId.Missingno;
        public double lat = 0, lng = 0;
        public SnipeEnum SnipeResult = SnipeEnum.Unknow;
        public string subPath = "";
        public async Task<int> Excute(string[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (args.Length == 1)
                args = args[0].Split(' ');

            if (args.Length > 1)
            {
                subPath += args[1];
            }

            //Logger.SetLogger(new ConsoleLogger(LogLevel.LevelUp), subPath);

            var machine = new StateMachine();
            var stats = new Statistics();
            var profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles", subPath);
            var profileConfigPath = Path.Combine(profilePath, "Config");
            var configFile = Path.Combine(profileConfigPath, "config.json");
            var parseSuccess = Enum.TryParse(args[0].Substring(14).Split('/')[0].Replace("'", "").Replace(" ", "").Replace(".", ""), out targetPoke);
            lat = double.Parse(args[0].Substring(14).Split('/')[1].Split(',')[0].Split('(').LastOrDefault().Split(')').LastOrDefault());
            lng = double.Parse(args[0].Substring(14).Split('/')[1].Split(',')[1].Split('(').LastOrDefault().Split(')').LastOrDefault());
            Console.WriteLine($"subpath = {subPath}");

            GlobalSettings settings;

            if (File.Exists(configFile) && parseSuccess)
            {
                settings = GlobalSettings.Load(subPath);
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
                await machine.AsyncStart(new UtahLoginState(), session, cancellationToken);
            }
            return (int)SnipeResult;
        }
    }
}
