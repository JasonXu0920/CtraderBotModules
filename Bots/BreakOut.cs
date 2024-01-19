using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SampleBreakoutcBot : Robot
    {
        [Parameter("Quantity (Lots)", Group = "Volume", DefaultValue = 1, MinValue = 0.01, Step = 0.01)]
        public double Quantity { get; set; }

        [Parameter("Stop Loss (pips)", Group = "Protection", DefaultValue = 20, MinValue = 1)]
        public int StopLossInPips { get; set; }

        [Parameter("Take Profit (pips)", Group = "Protection", DefaultValue = 40, MinValue = 1)]
        public int TakeProfitInPips { get; set; }

        [Parameter("Source", Group = "Bollinger Bands")]
        public DataSeries Source { get; set; }

        [Parameter("Band Height (pips)", Group = "Bollinger Bands", DefaultValue = 40.0, MinValue = 0)]
        public double BandHeightPips { get; set; }

        [Parameter("Bollinger Bands Deviations", Group = "Bollinger Bands", DefaultValue = 2)]
        public double Deviations { get; set; }

        [Parameter("Bollinger Bands Periods", Group = "Bollinger Bands", DefaultValue = 20)]
        public int Periods { get; set; }

        [Parameter("Bollinger Bands MA Type", Group = "Bollinger Bands")]
        public MovingAverageType MAType { get; set; }

        [Parameter("Consolidation Periods", Group = "Bollinger Bands", DefaultValue = 2)]
        public int ConsolidationPeriods { get; set; }

        BollingerBands bollingerBands;
        string label = "Sample Breakout cBot";
        int consolidation;

        protected override void OnStart()
        {
            bollingerBands = Indicators.BollingerBands(Source, Periods, Deviations, MAType);
        }

        protected override void OnBar()
        {
            var top = bollingerBands.Top.Last(1);
            var bottom = bollingerBands.Bottom.Last(1);

            if (top - bottom <= BandHeightPips * Symbol.PipSize)
            {
                consolidation = consolidation + 1;
            }
            else
            {
                consolidation = 0;
            }

            if (consolidation >= ConsolidationPeriods)
            {
                var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);
                if (Ask > top)
                {
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, label, StopLossInPips, TakeProfitInPips);
                    consolidation = 0;
                }
                else if (Bid < bottom)
                {
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, label, StopLossInPips, TakeProfitInPips);
                    consolidation = 0;
                }
            }
        }
    }
}
