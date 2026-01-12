using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StatsListener
{
    public class PluginConfig
    {
        public DatabaseConfig Database { get; set; }
        public PluginSettings PluginSettings { get; set; }

        public static PluginConfig LoadConfig(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Config file not found: {path}");
            }

            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<PluginConfig>(json);
            return config;
        }
    }

    public class DatabaseConfig
    {
        public string Host { get; set; } = "cs2_server-cs2-db-1";
        public int Port { get; set; } = 3306;
        public string User { get; set; } = "server";
        public string Password { get; set; } = "changeme";
        public string Database_Name { get; set; } = "db";

        public string GetConnectionString()
        {
            return $"Server={Host};Port={Port};Database={Database_Name};Uid={User};Pwd={Password};";
        }
    }

    public class PluginSettings
    {
    }
}
