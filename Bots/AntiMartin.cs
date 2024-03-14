using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class antimartin : Robot
    {
        [Parameter(DefaultValue = 1000)]
        public int Volume { get; set; }

        [Parameter(DefaultValue = 20)]
        public int TP { get; set; }

        [Parameter(DefaultValue = 20)]
        public int SL { get; set; }

        public int multiplier = 1;
        public double bal;
        public bool trade;
        public bool buy = true;
        public int count = 0;

        protected override void OnStart()
        {
            ExecuteMarketOrder(TradeType.Buy, SymbolName, Volume * multiplier, "buy", SL, TP);
        }

        protected override void OnTick()
        {
            if (Positions.Count == 0 && Account.Balance > bal)
            {
                multiplier = multiplier * 2;
                trade = true;
                count += 1;
            }
            else if (Positions.Count == 0)
            {
                if (buy)
                    buy = false;
                multiplier = 1;
                trade = true;
                count = 0;
            }

            if (count == 20)
                multiplier = 1;

            if (trade)
            {
                bal = Account.Balance;
                trade = false;
                if (buy)
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, Volume * multiplier, "buy", SL, TP);
                else
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, Volume * multiplier, "buy", SL, TP);
            }
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}