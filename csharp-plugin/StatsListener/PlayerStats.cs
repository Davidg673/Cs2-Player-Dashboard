using System;
using System.Collections.Generic;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using System.Data;
using MySql.Data;
using System.Threading;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Modules.Entities;



namespace StatListener
{
    public class PlayerStats : BasePlugin
    {
        public override string ModuleName => "Player stats";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor =>  "Davidg.528";

        Dictionary<ulong, (int kills, int deaths)> statsCache;  //Small cache used to upload to database to avoid lag from constant transmission
        CancellationTokenSource flushCts;   //controller for the cancellation token which is a way to signal the thread that it has to stop
        //tokens used here to cancel immediately using try/catch structure and not wait for loop to run again in order to stop


        public override void Load(bool hotReload)
        {
            base.Load(hotReload);
            statsCache = new Dictionary<ulong, (int kills, int deaths)>();

            flushCts = new CancellationTokenSource();

           Task.Run( () =>StartFlushingStats(flushCts.Token));

            RegisterEventHandler<EventPlayerDeath>((@event, info) => 
            {
                ulong attackerID = @event.Attacker.SteamID;
                ulong victimID = @event.Userid.SteamID;
                updatePlayerStats(attackerID, true, false);
                updatePlayerStats(victimID, false, true);


                return HookResult.Continue;
            }, HookMode.Post);
            
        
        }

        public override void Unload(bool hotReload)
        {
            flushCts.Cancel();
            base.Unload(hotReload);
        }

        void updatePlayerStats(ulong steamID, bool isKill, bool isDeath)
        {
            if (!statsCache.ContainsKey(steamID))
                statsCache[steamID] = (0, 0);

            var current = statsCache[steamID];
            statsCache[steamID] = (current.kills + (isKill ? 1 : 0), current.deaths + (isDeath?1 : 0));
        }

        async Task StartFlushingStats(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    FlushStatsToDatabase();
                    await Task.Delay(10000);
                }
            }
            catch (TaskCanceledException)
            {

            }
        }

        void FlushStatsToDatabase()
        {
            Console.WriteLine("=== FLUSHING PLAYER STATS ===");
            foreach (var entity in statsCache)
            {
                Console.WriteLine($"Player {entity.Key} - Kills: {entity.Value.kills}, Deaths: {entity.Value.deaths}");
            }
            Console.WriteLine("==============================");
        }

    }

}
