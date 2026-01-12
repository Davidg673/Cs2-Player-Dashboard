using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace StatsListener
{
    public class StatsManager
    {
        private readonly object cacheLock = new(); //lock object to make sure cache cannot be accesesd at the same time by multiple threads
        Dictionary<ulong, (int kills, int deaths, int timePlayed, DateTime? lastPlayed)> statsCache;  //Small cache used to upload to database to avoid lag from constant transmission
        
        public DatabaseManager dbManager;


        public StatsManager(BasePlugin plugin, DatabaseConfig dbConfig)
        {
            dbManager = new DatabaseManager(dbConfig.GetConnectionString());
        }

        public void Start()
        {

            statsCache = new Dictionary<ulong, (int kills, int deaths, int  timePlayed, DateTime? lastPlayed)>();


        }

        public void Stop()
        {
        }


        //Function called whenever player dies and contains relevant stats to their death which will be sent to db
        public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            ulong attackerID = @event.Attacker.SteamID;
            ulong victimID = @event.Userid.SteamID;
            Update(attackerID, true, false,0 ,null);
            Update(victimID, false, true,0 , null);

            return HookResult.Continue;
        }


        // takes any relevant data submitted to it, parses it and sends it further to database. This will be the central hub for collecting/filtering any data
        private void Update(ulong steamID, bool isKill, bool isDeath, int timePassed, DateTime? lastPlayed)
        {
            lock (cacheLock)
            {
                if (!statsCache.ContainsKey(steamID))
                    statsCache[steamID] = (0, 0, 0, null);

                var current = statsCache[steamID];
                statsCache[steamID] = (current.kills + (isKill ? 1 : 0), 
                                       current.deaths + (isDeath ? 1 : 0), 
                                       current.timePlayed + timePassed, 
                                       current.lastPlayed ?? lastPlayed);
            }
        }


        //reads saved data in dictionary and communicates with db manager for transfer
        public async Task FlushToDatabaseAsync()
        {
            Console.WriteLine("=== FLUSHING PLAYER STATS ===");

            var tasks = statsCache.Select(entity =>
            {
                var val = entity.Value;
                Console.WriteLine($"Player {entity.Key} - Kills: {entity.Value.kills}, Deaths: {entity.Value.deaths}, Time played cummulative: {entity.Value.timePlayed}, last played: {entity.Value.lastPlayed}");
                return dbManager.SaveStatsAsync(entity.Key, entity.Value.kills, entity.Value.deaths, entity.Value.timePlayed, entity.Value.lastPlayed);
            });

            await Task.WhenAll(tasks);
            statsCache.Clear();

            Console.WriteLine("==============================");


        }


    }
}
