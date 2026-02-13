using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
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

            statsManager = new StatsManager(this,config);

            RegisterEventHandler<EventPlayerDeath>(statsManager.OnPlayerDeath, HookMode.Post);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Post);
            RegisterEventHandler<EventBombPlanted>(statsManager.OnBombPlanted, HookMode.Post);
            RegisterEventHandler<EventBombDefused>(statsManager.OnBombDefused, HookMode.Post);
            RegisterEventHandler<EventRoundEnd>(statsManager.OnRoundEnd, HookMode.Post);
            RegisterEventHandler<EventPlayerDisconnect>(statsManager.OnPlayerDisconnect, HookMode.Post);
            RegisterEventHandler<EventPlayerConnectFull>(statsManager.OnPlayerConnect, HookMode.Post);
            RegisterEventHandler<EventPlayerHurt>(statsManager.OnPlayerHurt, HookMode.Post);

            statsManager.Start();

        }

        public override void Unload(bool hotReload)
        {
            base.Unload(hotReload);
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {

            try
            {
                statsManager.FlushToBackend();
            } 
            catch( Exception ex)
            {
                Console.WriteLine($"[Stats Listener] {ex}");
            }
            return HookResult.Continue;
        }

    }

}
