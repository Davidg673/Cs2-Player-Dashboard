using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using McMaster.NETCore.Plugins;
using StatsListener;
using System.Reflection;
using System.Threading.Tasks;


namespace StatsListener
{
    public class PlayerStats : BasePlugin
    {
        public override string ModuleName => "Player stats";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor =>  "Davidg.528";

        StatsManager statsManager;

        public override void Load(bool hotReload)
        {
            base.Load(hotReload);

            string gameDir = Server.GameDirectory;
            string configPath = Path.Combine(
                    gameDir,
                    "csgo",
                    "addons",
                    "counterstrikesharp",
                    "configs",
                    "plugins",
                    "StatsListener",
                    "StatsListener.json"
                );

            PluginConfig config;

            try
            {
                config = PluginConfig.LoadConfig(configPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SatsListener] {ex.Message}");
                return;
            }

            statsManager = new StatsManager(this,config.Database);

            bool dbReady = statsManager.dbManager.TestConnectionAsync().GetAwaiter().GetResult();
            if (!dbReady)
            {
                Console.WriteLine($"[SatsListener] Database not available. Plugin will not start. Make sure config file is setup correctly");
                return;
            }

            statsManager.Start();

            RegisterEventHandler<EventPlayerDeath>(statsManager.OnPlayerDeath, HookMode.Post);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Post);

        }

        public override void Unload(bool hotReload)
        {
            base.Unload(hotReload);
            statsManager.Stop();
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            Task.Run(async () =>
            {
                await statsManager.FlushToDatabaseAsync();
            }); 
            return HookResult.Continue;
        }
    }

}
