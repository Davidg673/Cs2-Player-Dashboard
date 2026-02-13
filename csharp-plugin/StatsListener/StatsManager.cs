using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using System.Collections.Concurrent;
using System.Numerics;
using static StatsListener.StatsManager;


namespace StatsListener
{
    using PlayerSnapshot = ConcurrentDictionary<ulong, StatsManager.PlayerStats>;
    using WeaponSnapshot = ConcurrentDictionary<ulong, ConcurrentDictionary<string, WeaponStats>>;
    public class StatsManager
    {

        ConcurrentDictionary<ulong, PlayerStats> statsCache;  //Small cache used to upload to avoid lag from constant transmission

        ConcurrentDictionary<ulong, ConcurrentDictionary<string, WeaponStats>> weaponStatsCache;
        ConcurrentDictionary<ulong, DateTime> playerJoinedTime;

        string frontendUrl;
        int timeToPrint = 0;

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

        public IngestClient ingestClient;



        public StatsManager(BasePlugin plugin, PluginConfig config)
        {
            ingestClient = new IngestClient(config.ingestUrl, config.apiKey);
            frontendUrl = config.frontendUrl;
        }

        public void Start()
        {

            statsCache = new ConcurrentDictionary<ulong, PlayerStats>();
            weaponStatsCache = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, WeaponStats>>();
            playerJoinedTime = new ConcurrentDictionary<ulong, DateTime>();
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

            Server.PrintToChatAll($"Stats Saved!");

            if (timeToPrint <= 0)
            {
                Server.PrintToChatAll($"\x04 Visit \x03{frontendUrl}/login\x04 to see your stats! Link can also be found in console!");
                foreach (var player in Utilities.GetPlayers())
                {
                    if (player == null || !player.IsValid || player.IsBot) continue;
                    player.PrintToConsole("==============================");
                    player.PrintToConsole(" YOUR STATS DASHBOARD ");
                    player.PrintToConsole($" {frontendUrl}/login ");
                    player.PrintToConsole("==============================");
                }


                timeToPrint = 3;
            } else timeToPrint--;

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
                @event.Userid.PrintToChat($"\x04 Visit \x03{frontendUrl}/login\x04 to see your stats! Link can also be found in console!");
                @event.Userid.PrintToConsole("==============================");
                @event.Userid.PrintToConsole(" YOUR STATS DASHBOARD ");
                @event.Userid.PrintToConsole($" {frontendUrl}/login ");
                @event.Userid.PrintToConsole("==============================");

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
        public void FlushToBackend()
        {
            ////Create Snapshot of data and wipe it for next round. snapshot ensures ogirinal data can still record without waiting on HTTP operations.
            ConcurrentDictionary<ulong, PlayerStats> snapshotPlayer;
            ConcurrentDictionary<ulong, ConcurrentDictionary<string,WeaponStats>> snapshotWeapon;
            List<IngestPayload> payloads = new List<IngestPayload>();


            if (statsCache.Count == 0 && weaponStatsCache.Count == 0) return;

            snapshotPlayer = new ConcurrentDictionary<ulong, PlayerStats>(statsCache);
            statsCache.Clear();
            

            snapshotWeapon = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, WeaponStats>>(weaponStatsCache.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ConcurrentDictionary<string, WeaponStats>(kvp.Value)
                ));
            weaponStatsCache.Clear();


            Console.WriteLine("=== FLUSHING PLAYER STATS ===");

            foreach (var kvp in snapshotPlayer)
            {
                IngestPayload payload = ConvertToPayload(kvp.Key,kvp.Value, snapshotWeapon);
                payloads.Add(payload);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await ingestClient.SendManyAsync(payloads);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Stats Listener] {ex}");
                }
            });
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
        /// <summary>
        /// Converts snapshot of player stats dictionary to a payload (JSON) ready for HTTP 
        /// </summary>
        /// <param name="playerSnapshot"></param>
        /// <param name="weaponSnapshot"></param>
        /// <returns> Payload object as representation of JSON </returns>
        private static IngestPayload ConvertToPayload(ulong key, PlayerStats stats, WeaponSnapshot weaponSnapshot) 
        {
            var payload = new IngestPayload();

            var dt = stats.lastPlayed.ToUniversalTime();
            dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc);

            payload.player = new PlayerPayload
            {
                steamid = key.ToString(),
                kills = stats.kills,
                headshots = stats.headshots,
                damage_dealt = stats.damageDealt,
                damage_received = stats.damageReceived,
                bomb_plants = stats.bombPlants,
                bomb_defuses = stats.bombDefuses,
                playtime = stats.timePlayed,
                last_played = dt,
                rounds_won = stats.roundsWon,
                rounds_lost = stats.roundsLost
            };

            if (weaponSnapshot.TryGetValue(key,out var weaponDict))
            {
                foreach (var weaponkvp in weaponDict)
                {
                    payload.weapons.Add(new WeaponPayload
                    {
                        weapon = weaponkvp.Key,
                        kills = weaponkvp.Value.kills,
                        headshots = weaponkvp.Value.headshots,
                        shots_hit = weaponkvp.Value.shotsHit,
                        damage_dealt = weaponkvp.Value.damageDealt
                    });
                }
            }

            return payload;
        }

    }
}
