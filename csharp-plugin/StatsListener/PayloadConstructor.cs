using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsListener
{
    public class PlayerPayload
    {
        public string steamid { get; set; } = "";
        public int kills { get; set; }
        public int deaths { get; set; }
        public int assists { get; set; }
        public int headshots { get; set; }
        public int damage_dealt { get; set; }
        public int damage_received { get; set; }
        public int bomb_plants { get; set; }
        public int bomb_defuses { get; set; }
        public int playtime { get; set; }
        public DateTime last_played { get; set; }
        public int rounds_won { get; set; }
        public int rounds_lost { get; set; }

    }

    public class WeaponPayload
    {
        public string weapon { get; set; } = "";
        public int kills { get; set; }
        public int headshots { get; set; }
        public int shots_hit { get; set; }
        public int damage_dealt { get; set; }
    }

    public class IngestPayload
    {
        public PlayerPayload player { get; set; } = new();
        public List<WeaponPayload> weapons { get; set; } = new();
    }
}
