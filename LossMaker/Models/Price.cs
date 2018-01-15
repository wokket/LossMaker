using CsvHelper.Configuration;
using System;

namespace LossMaker.Models
{
    public class Price
    {
        public int Id { get; set; }
        public int StationId { get; set; }
        public int CommodityId { get; set; }
        public int? Supply { get; set; }
        public int? SupplyBracket { get; set; }
        public int? BuyPrice { get; set; }
        public int? SellPrice { get; set; }

    }


    public class PriceMap : ClassMap<Price>
    {
        public PriceMap()
        {
            Map(m => m.Id).Name("id");
            Map(m => m.StationId).Name("station_id");
            Map(m => m.CommodityId).Name("commodity_id");
            Map(m => m.BuyPrice).Name("buy_price");
            Map(m => m.SellPrice).Name("sell_price");
            Map(m => m.Supply).Name("supply");
            Map(m => m.SupplyBracket).Name("supply_bracket");

        }
    }
}
