using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Alexis : Robot
    {
        [Parameter("fastMA", DefaultValue = 10)]
        public int periodFast { get; set; }
        [Parameter("slowMA", DefaultValue = 20)]
        public int periodSlow { get; set; }
        [Parameter("Source")]
        public DataSeries SourceSeries { get; set; }
        private ExponentialMovingAverage slowMa;
        private ExponentialMovingAverage fastMa;

        [Parameter("BB", DefaultValue = 20)]
        public int periodBB { get; set; }
        [Parameter("Standard deviations", DefaultValue = 2)]
        public int stD { get; set; }
        [Parameter("MA Type", DefaultValue = 2)]
        public MovingAverageType typeMA { get; set; }

        [Parameter("SL1", DefaultValue = 50)]
        public double SL1 { get; set; }
        [Parameter("SL2", DefaultValue = 100)]
        public double SL2 { get; set; }

        public BollingerBands bollingerBands;

        public string traded = "none";

        protected override void OnStart()
        {
            bollingerBands = Indicators.BollingerBands(SourceSeries, periodBB, stD, typeMA);

            fastMa = Indicators.ExponentialMovingAverage(SourceSeries, periodFast);
            slowMa = Indicators.ExponentialMovingAverage(SourceSeries, periodSlow);
        }

        protected override void OnTick()
        {
            if (Symbol.Ask <= bollingerBands.Bottom.LastValue && fastMa.Result.LastValue > slowMa.Result.LastValue && traded == "none" && Positions.Count == 0)
            {
                traded = "buy";
                ExecuteMarketOrder(TradeType.Buy, SymbolName, 1000, "position1", 50, 0);
                ExecuteMarketOrder(TradeType.Buy, SymbolName, 1000, "position2", 100, 0);
            }

            if (Symbol.Ask >= bollingerBands.Top.LastValue && fastMa.Result.LastValue < slowMa.Result.LastValue && traded == "none" && Positions.Count == 0)
            {
                traded = "sell";
                ExecuteMarketOrder(TradeType.Sell, SymbolName, 1000, "position1", 50, 0);
                ExecuteMarketOrder(TradeType.Sell, SymbolName, 1000, "position2", 100, 0);
            }

            if (Positions.Count == 2 && Symbol.Ask <= bollingerBands.Main.LastValue && traded == "sell")
            {
                Positions.Find("position1").Close();
                Positions.Find("position2").ModifyStopLossPips(-2);
            }

            if (Positions.Count == 2 && Symbol.Ask >= bollingerBands.Main.LastValue && traded == "buy")
            {
                Positions.Find("position1").Close();
                Positions.Find("position2").ModifyStopLossPips(-2);
            }

            if (Positions.Count == 1 && Symbol.Ask >= bollingerBands.Top.LastValue && traded == "buy")
            {
                Positions.Find("position2").Close();
            }

            if (Positions.Count == 1 && Symbol.Ask <= bollingerBands.Bottom.LastValue && traded == "sell")
            {
                Positions.Find("position2").Close();
            }

            if (Server.TimeInUtc.Hour == 21)
                traded = "none";
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}