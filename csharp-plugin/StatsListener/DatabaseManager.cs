using MySqlConnector;
using System.Threading.Tasks;
namespace StatsListener
{
    public class DatabaseManager
    {
        private readonly string connString;

        public DatabaseManager(string connString)
        {
            this.connString = connString;
        }

        public async Task CheckTableExists()
        {
            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();

            await new MySqlCommand(@"CREATE TABLE IF NOT EXISTS player_stats( 
                                      steamid VARCHAR(18) NOT NULL PRIMARY KEY);",conn).ExecuteNonQueryAsync();

            var columns = new (string Name, string Type, string Default)[]
            {
                ("kills","INT UNSIGNED NOT NULL","0"),
                ("deaths","INT UNSIGNED NOT NULL","0"),
                ("playtime","INT UNSIGNED NULL","NULL"),
                ("last_Played","DATETIME NOT NULL","CURRENT_TIMESTAMP"),


            };

            foreach (var col in columns)
            {
                var templateCmd = $"ALTER TABLE player_stats ADD COLUMN IF NOT EXISTS {col.Name} {col.Type} DEFAULT {col.Default};";

                await new MySqlCommand(templateCmd, conn).ExecuteNonQueryAsync();
            }
        }

        public async Task SaveStatsAsync(ulong steamID, int kills, int deaths, int timePlayed,DateTime ?logoutDate)
        {
            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();
            string templateCmd;

            if (logoutDate.HasValue)
            {
                templateCmd = @"INSERT INTO player_stats (steamid,kills,deaths,playtime,last_played)
                                            VALUES (@s,@k,@d,@p,@l)  
                                            ON DUPLICATE KEY UPDATE 
                                                            kills = kills + @k,
                                                            deaths = deaths + @d,
                                                            playtime = playtime +@p,
                                                            last_played = @l;";
            }
            else
            {
                templateCmd = @"INSERT INTO player_stats (steamid,kills,deaths,playtime,last_played)
                                            VALUES (@s,@k,@d,@p,@l)  
                                            ON DUPLICATE KEY UPDATE 
                                                            kills = kills + @k,
                                                            deaths = deaths + @d,
                                                            playtime = playtime +@p;";
            }

            var cmd = new MySqlCommand(templateCmd, conn);

            cmd.Parameters.AddWithValue("@s", steamID.ToString());
            cmd.Parameters.AddWithValue("@k", kills);
            cmd.Parameters.AddWithValue("@d", deaths);
            cmd.Parameters.AddWithValue("@p", timePlayed);

            if (logoutDate.HasValue)
                cmd.Parameters.AddWithValue("@l", logoutDate.Value);

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
