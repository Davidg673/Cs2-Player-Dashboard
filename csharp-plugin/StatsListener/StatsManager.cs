using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace StatsListener
{
    public class StatsManager
    {

        ConcurrentDictionary<ulong, PlayerStats> statsCache;  //Small cache used to upload to database to avoid lag from constant transmission

        ConcurrentDictionary<ulong, ConcurrentDictionary<string, WeaponStats>> weaponStatsCache;
        ConcurrentDictionary<ulong, DateTime> playerJoinedTime;

        public class WeaponStats //Used to store individual weapon stats per player for any weapon they use that round
        {
            public int kills;
            public int headshots;
            public int shotsHit;
            public int damageDealt;
        }

        public class PlayerStats 
        {
            public int kills;
            public int deaths;
            public int assists;
            public int headshots;
            public int damageDealt;
            public int damageReceived;
            public int bombPlants;
            public int bombDefuses;
            public int timePlayed;
            public DateTime lastPlayed = DateTime.UtcNow;
            public int roundsWon;
            public int roundsLost;

        }

        public DatabaseManager dbManager;



        public StatsManager(BasePlugin plugin, DatabaseConfig dbConfig)
        {
            dbManager = new DatabaseManager(dbConfig.GetConnectionString());
        }

        public void Start()
        {

            statsCache = new ConcurrentDictionary<ulong, PlayerStats>();
            weaponStatsCache = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, WeaponStats>>();
            playerJoinedTime = new ConcurrentDictionary<ulong, DateTime>();

            dbManager.Start();
        }




        //Function called whenever player dies and contains relevant stats to their death which will be sent to db
        public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            var assister = @event.Assister;
            bool headshot = @event.Headshot;
            int totalDamage = @event.DmgHealth + @event.DmgArmor;
            var weaponEntity = attacker?.Pawn?.Value?.WeaponServices?.ActiveWeapon?.Value;

            if (victim!= null && !victim.IsBot && victim.IsValid)
                UpdateStats(victim.SteamID, false, true,false,false,0,totalDamage,false,false,DateTime.UtcNow,false,false);
            

            if (attacker!=null && attacker.IsValid && attacker!= victim && !attacker.IsBot)
            {
                UpdateStats(attacker.SteamID, true, false, false, headshot, 0, 0, false, false, DateTime.UtcNow, false, false);
                string weaponName = NormalizeWeapon(weaponEntity.DesignerName);
                UpdateStats(attacker.SteamID, weaponName, true, headshot,false,0);
            }


            if (assister != null && !assister.IsBot && assister.IsValid)
                UpdateStats(assister.SteamID,false,false,true,false,0,0,false,false,DateTime.UtcNow,false,false);


            return HookResult.Continue;
        }

        public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
        {
            if (@event.Userid != null && !@event.Userid.IsBot)
                UpdateStats(@event.Userid.SteamID, false, false, false, false, 0, 0, true, false, DateTime.UtcNow, false, false);

            return HookResult.Continue;
        }


        public HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
        {
            if (@event.Userid!=null && !@event.Userid.IsBot)
                UpdateStats(@event.Userid.SteamID, false, false, false, false, 0, 0, false, true, DateTime.UtcNow, false, false);

            return HookResult.Continue;
        }


        public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            int winnerTeam=@event.Winner;

            foreach (var player in Utilities.GetPlayers())
            {
                if (player.UserId == null || player.IsBot) continue;

                RecordTimePlayed(player.SteamID);

                if ((int)player.Team == winnerTeam)
                    UpdateStats(player.SteamID,false,false,false,false,0,0,false,false,DateTime.UtcNow,true,false);
                else
                    UpdateStats(player.SteamID, false, false, false, false, 0, 0, false, false, DateTime.UtcNow, false, true);
            }

            return HookResult.Continue;
        }

        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (@event.Userid==null || @event.Userid.IsBot) return HookResult.Continue;

            RecordTimePlayed(@event.Xuid);
            playerJoinedTime.TryRemove(@event.Xuid, out _);           

            return HookResult.Continue;
        }


        public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (@event.Userid !=null && !@event.Userid.IsBot && @event.Userid.SteamID!=0)
            {
                playerJoinedTime.TryAdd(@event.Userid.SteamID, DateTime.UtcNow);
            }

            
            return HookResult.Continue;
        }


        public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            int totalDamage = @event.DmgHealth + @event.DmgArmor;

            if (attacker!=null && attacker.IsValid && !attacker.IsBot &&  attacker != @event.Userid)
            {
                string weaponName = NormalizeWeapon(@event.Weapon);
                UpdateStats(attacker.SteamID, weaponName, false, false, true, totalDamage);
                UpdateStats(attacker.SteamID, false, false, false, false, totalDamage, 0, false, false, DateTime.UtcNow, false, false);
            }
               
            return HookResult.Continue;
        }



        // takes any relevant data submitted to it ***from an event call***, parses it and sends it further to database. This will be the central hub for collecting/filtering any data
        private void UpdateStats(ulong steamID,bool isKill, bool isDeath, bool isAssist, bool isHeadshot, int damageDealt, int damageReceived, bool bombPlanted,
                           bool bombDefused, DateTime lastPlayed, bool roundWon, bool roundLost)
        {
            var current = statsCache.GetOrAdd(steamID,_ => new PlayerStats());


            if (isKill) Interlocked.Increment(ref current.kills);
            if (isDeath) Interlocked.Increment(ref current.deaths);
            if (isAssist) Interlocked.Increment(ref current.assists);
            if (isHeadshot) Interlocked.Increment(ref current.headshots);
            Interlocked.Add(ref current.damageDealt,damageDealt);
            Interlocked.Add(ref current.damageReceived, damageReceived);
            if (bombPlanted) Interlocked.Increment(ref current.bombPlants);
            if (bombDefused) Interlocked.Increment(ref current.bombDefuses);
            if (roundWon) Interlocked.Increment(ref current.roundsWon);
            if (roundLost) Interlocked.Increment(ref current.roundsLost);
            current.lastPlayed = lastPlayed;
  
            
        }

        private void UpdateStats(ulong steamID,string weapon,bool isKill, bool isHeadshot, bool shotHit, int damageDealt)
        {
            var playerWeapons = weaponStatsCache.GetOrAdd(steamID, _ => new ConcurrentDictionary<string, WeaponStats>());
            var weaponStats = playerWeapons.GetOrAdd(weapon, _ => new WeaponStats());

            if (isKill) Interlocked.Increment(ref weaponStats.kills);
            if (isHeadshot) Interlocked.Increment(ref weaponStats.headshots);
            if (shotHit) Interlocked.Increment(ref weaponStats.shotsHit);
            Interlocked.Add(ref weaponStats.damageDealt, damageDealt);

        }

        private void RecordTimePlayed(ulong steamID)
        {

            int timePlayed = 0;
            var current = statsCache.GetOrAdd(steamID, _ => new PlayerStats());


            if (playerJoinedTime.TryGetValue(steamID, out var joinTime))
            {

                timePlayed = (int)(DateTime.UtcNow - joinTime).TotalSeconds;
                playerJoinedTime[steamID] = DateTime.UtcNow;
            }
            Interlocked.Add(ref current.timePlayed, timePlayed);

        }


        //reads saved data in dictionary and communicates with db manager for transfer
        public async Task FlushToDatabaseAsync()
        {
            ConcurrentDictionary<ulong, PlayerStats> snapshotPlayer;
            ConcurrentDictionary<ulong, ConcurrentDictionary<string,WeaponStats>> snapshotWeapon;
            var tasks = new List<Task>();


            if (statsCache.Count == 0 && weaponStatsCache.Count == 0) return;

            snapshotPlayer = new ConcurrentDictionary<ulong, PlayerStats>(statsCache);
            statsCache.Clear();
            

            snapshotWeapon = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, WeaponStats>>(weaponStatsCache.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ConcurrentDictionary<string, WeaponStats>(kvp.Value)
                ));
            weaponStatsCache.Clear();


            Console.WriteLine("=== FLUSHING PLAYER STATS ===");

            foreach ( var entity in snapshotPlayer)
            {
                var val = entity.Value;
                //Console.WriteLine($@"Player {entity.Key}, Kills: {entity.Value.kills}, Deaths: {entity.Value.deaths},Assists: {entity.Value.assists},Headshots: {entity.Value.headshots},
                //                 Damage dealt: {entity.Value.damageDealt}, Damage Received: {entity.Value.damageReceived}, Bomb plants: {entity.Value.bombPlants}, Bomb Defuses: {entity.Value.bombDefuses},
                //                 Time played cummulative: {entity.Value.timePlayed}, last played: {entity.Value.lastPlayed}, rounds won: {entity.Value.roundsWon}, rounds won: {entity.Value.roundsWon}");
                //DEBUG CONSOLE PRINT
                tasks.Add(dbManager.SaveStatsAsync(entity.Key, val.kills, val.deaths,val.assists,val.headshots,val.damageDealt,
                                                    val.damageReceived,val.bombPlants,val.bombDefuses,val.timePlayed, val.lastPlayed,val.roundsWon,val.roundsLost));
            }

            foreach (var entity in snapshotWeapon)
            {
                var weapons = entity.Value;
                foreach (var weaponEntry in weapons)
                {
                    WeaponStats weaponStat = weaponEntry.Value;
                    //Console.WriteLine($"Player {entity.Key},Weapon: {weaponEntry.Key}, Kills: {weaponStat.kills}, Headshots: {weaponStat.headshots}, Shots fired: {weaponStat.shotsFired}, Shots hit: {weaponStat.shotsHit}");
                    //DEBUG CONSOLE PRINT
                    tasks.Add(dbManager.SaveSingleWeaponStatsAsync(entity.Key, weaponEntry.Key, weaponStat.kills, weaponStat.headshots, weaponStat.shotsHit,weaponStat.damageDealt));
                }
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("==============================");

        }



        private static string NormalizeWeapon(string weapon)
        {
            if (string.IsNullOrEmpty(weapon))
                return "unknown";

            weapon = weapon.ToLowerInvariant();

            if (weapon.StartsWith("weapon_"))
                weapon = weapon.Substring(7);

            return weapon;
        }
    }
}
