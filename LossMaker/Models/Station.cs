﻿using System.Collections.Generic;
using System.Diagnostics;

namespace LossMaker.Models
{

    [DebuggerDisplay("Station ({id}:{name})")]
    public sealed class Station
    {

        public Station()
        {
            Prices = new Dictionary<int, Price>();
        }

        public int? id { get; set; }
        public string name { get; set; }
        public int? system_id { get; set; }
        public string max_landing_pad_size { get; set; }
        public int? distance_to_star { get; set; }
        public string type { get; set; }
        public bool has_blackmarket { get; set; }
        public bool has_market { get; set; }
        public bool has_refuel { get; set; }
        public bool has_repair { get; set; }
        public bool has_rearm { get; set; }
        public bool has_outfitting { get; set; }
        public bool has_shipyard { get; set; }
        public bool has_docking { get; set; }
        public bool has_commodities { get; set; }
        public int? market_updated_at { get; set; }
        public bool is_planetary { get; set; }
        public int? body_id { get; set; }

        /// <summary>
        /// indexed by CommodityId
        /// </summary>
        public Dictionary<int, Price> Prices { get;  }
        public string SystemName { get; internal set; }
        public float? DistanceToSystem { get; internal set; }
    }

}
