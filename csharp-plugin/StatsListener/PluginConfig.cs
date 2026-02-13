using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StatsListener
{
    public class PluginConfig
    {
        //Must match names in json so they can be mapped automatically when Deserialize gets called
        public string ingestUrl { get; set; } = "";   
        public string apiKey { get; set; } = "";
        public string frontendUrl { get; set; } = "";

        public static PluginConfig LoadConfig(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Config file not found: {path}");
            }

            string json = File.ReadAllText(path);

            var config = JsonSerializer.Deserialize<PluginConfig>(json) ?? throw new InvalidOperationException("Config deserialised to null");

            config.ingestUrl = config.ingestUrl.Trim() ?? "";
            config.apiKey = config.apiKey.Trim() ?? "";

            if (!Uri.TryCreate(config.ingestUrl, UriKind.Absolute, out _))
                throw new InvalidOperationException($"Invalid ingestUrl in config: '{config.ingestUrl}'");

            if (String.IsNullOrEmpty(config.apiKey))
                throw new InvalidOperationException("Missign apiKey in config");

            Console.WriteLine($"Loaded ingestUrl='{config.ingestUrl}', apiKey length = {config.apiKey?.Length ?? 0}");

            return config;
        }
    }

}
