using CounterStrikeSharp.API.Modules.Entities;
using MySqlConnector;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace StatsListener

{
    public class DatabaseManager
    {
        private readonly string connString;

        public DatabaseManager(string connString)
        {
            this.connString = connString;
        }

        public void Start()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckTableExists();
                } catch (Exception ex)
                {
                    Console.WriteLine($"[Stats Listener] {ex.Message}");
                }
            });
        }

        public async Task CheckTableExists()
        {
            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();

            await new MySqlCommand(@"CREATE TABLE IF NOT EXISTS player_stats( 
                                      steamid VARCHAR(18) NOT NULL PRIMARY KEY,
                                      kills INT UNSIGNED NOT NULL DEFAULT 0,
                                      deaths INT UNSIGNED NOT NULL DEFAULT 0,
                                      assists INT UNSIGNED NOT NULL DEFAULT 0,
                                      headshots INT UNSIGNED NOT NULL DEFAULT 0,
                                      damage_dealt INT UNSIGNED NOT NULL DEFAULT 0,
                                      damage_received INT UNSIGNED NOT NULL DEFAULT 0,
                                      bomb_plants INT UNSIGNED NOT NULL DEFAULT 0,
                                      bomb_defuses INT UNSIGNED NOT NULL DEFAULT 0,
                                      playtime INT UNSIGNED NOT NULL DEFAULT 0,
                                      last_played TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, 
                                      rounds_won INT UNSIGNED NOT NULL DEFAULT 0,
                                      rounds_lost INT UNSIGNED NOT NULL DEFAULT 0);", conn).ExecuteNonQueryAsync();

            await new MySqlCommand(@"CREATE TABLE IF NOT EXISTS player_weapon_stats( 
                                      steamid VARCHAR(18) NOT NULL,
                                      weapon VARCHAR(18) NOT NULL,
                                      kills INT UNSIGNED NOT NULL DEFAULT 0,
                                      headshots INT UNSIGNED NOT NULL DEFAULT 0,
                                      shots_hit INT UNSIGNED NOT NULL DEFAULT 0,
                                      damage_dealt INT UNSIGNED NOT NULL DEFAULT 0,
                                      PRIMARY KEY (steamid,weapon));", conn).ExecuteNonQueryAsync();
        }

        public async Task SaveStatsAsync(ulong steamID, int kills, int deaths,int assists,int headshots,int damageDealt,int damageReceived,int bombPlants,
                                         int bombDefuses,int timePlayed,DateTime lastPlayedDate,int roundsWon,int roundsLost)
        {
            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();
            string templateCmd;

 
            templateCmd = @"INSERT INTO player_stats (steamid,kills,deaths,assists,headshots,damage_dealt,damage_received,bomb_plants,bomb_defuses,playtime,last_played,rounds_won,rounds_lost)
                                        VALUES (@steam,@kills,@deaths,@assists,@headshots,@dealt,@received,@plants,@defuses,@playtime,@lastplayed,@won,@lost)  
                                        ON DUPLICATE KEY UPDATE 
                                                        kills = kills + @kills,
                                                        deaths = deaths + @deaths,
                                                        assists = assists + @assists,
                                                        headshots = headshots + @headshots,
                                                        damage_dealt = damage_dealt + @dealt,
                                                        damage_received = damage_received + @received,
                                                        bomb_plants = bomb_plants + @plants,
                                                        bomb_defuses = bomb_defuses + @defuses,
                                                        playtime = playtime +@playtime,
                                                        last_played = @lastplayed,
                                                        rounds_won = rounds_won + @won,
                                                        rounds_lost = rounds_lost + @lost;";


            var cmd = new MySqlCommand(templateCmd, conn);

            cmd.Parameters.AddWithValue("@steam", steamID.ToString());
            cmd.Parameters.AddWithValue("@kills", kills);
            cmd.Parameters.AddWithValue("@deaths", deaths);
            cmd.Parameters.AddWithValue("@assists", assists);
            cmd.Parameters.AddWithValue("@headshots", headshots);
            cmd.Parameters.AddWithValue("@dealt", damageDealt);
            cmd.Parameters.AddWithValue("@received", damageReceived);
            cmd.Parameters.AddWithValue("@plants", bombPlants);
            cmd.Parameters.AddWithValue("@defuses", bombDefuses);
            cmd.Parameters.AddWithValue("@playtime", timePlayed);
            cmd.Parameters.AddWithValue("@lastplayed", lastPlayedDate);
            cmd.Parameters.AddWithValue("@won", roundsWon);
            cmd.Parameters.AddWithValue("@lost", roundsLost);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveSingleWeaponStatsAsync(ulong steamID, string weapon, int kills,int headshots, int shotsHit, int damageDealt)
        {
            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();
            string templateCmd;

            templateCmd = @"INSERT INTO player_weapon_stats (steamid,weapon,kills,headshots,shots_hit,damage_dealt)
                                        VALUES (@steam,@weapon,@kills,@headshots,@hit,@dmg)  
                                        ON DUPLICATE KEY UPDATE 
                                                        kills = kills + @kills,
                                                        headshots = headshots + @headshots,
                                                        shots_hit = shots_hit +@hit,
                                                        damage_dealt = damage_dealt + @dmg;";
                                                                                

            var cmd = new MySqlCommand(templateCmd, conn);

            cmd.Parameters.AddWithValue("@steam", steamID.ToString());
            cmd.Parameters.AddWithValue("@weapon", weapon);
            cmd.Parameters.AddWithValue("@kills", kills);
            cmd.Parameters.AddWithValue("@headshots", headshots);
            cmd.Parameters.AddWithValue("@hit", shotsHit);
            cmd.Parameters.AddWithValue("@dmg", damageDealt);


            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StatListener] DB connection failed: {ex.Message}");
                return false;
            }
        }

    }
}
