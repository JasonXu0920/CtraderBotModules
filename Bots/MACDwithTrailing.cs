using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SampleMACDcBotgeht : Robot
    {
        [Parameter("Quantity (Lots)", Group = "Volume", DefaultValue = 1, MinValue = 0.01, Step = 0.01)]
        public double Quantity { get; set; }

        [Parameter("Source", Group = "MACD")]
        public DataSeries Source { get; set; }

        [Parameter("Fast EMA Periods", Group = "MACD", DefaultValue = 12)]
        public int FastPeriods { get; set; }

        [Parameter("Slow EMA Periods", Group = "MACD", DefaultValue = 26)]
        public int SlowPeriods { get; set; }

        [Parameter("Signal Periods", Group = "MACD", DefaultValue = 9)]
        public int SignalPeriods { get; set; }

        [Parameter("Stop Loss (Pips)", Group = "Trading", DefaultValue = 10)]
        public int StopLossPips { get; set; }

        [Parameter("Take Profit (Pips)", Group = "Trading", DefaultValue = 10)]
        public int TakeProfitPips { get; set; }

        [Parameter("Trailing Stop Start (Pips)", Group = "Trading", DefaultValue = 2)]
        public int TrailingStopStart { get; set; }

        [Parameter("Trailing Stop Distance (Pips)", Group = "Trading", DefaultValue = 2)]
        public int TrailingStopDistance { get; set; }

        [Parameter("MA1 Periods", Group = "Moving Averages", DefaultValue = 10)]
        public int Ma1Periods { get; set; }

        [Parameter("MA2 Periods", Group = "Moving Averages", DefaultValue = 20)]
        public int Ma2Periods { get; set; }

        private MacdHistogram macd;
        private MovingAverage ma1;
        private MovingAverage ma2;

        protected override void OnStart()
        {
            macd = Indicators.MacdHistogram(Source, FastPeriods, SlowPeriods, SignalPeriods);
            ma1 = Indicators.MovingAverage(Source, Ma1Periods, MovingAverageType.Exponential);
            ma2 = Indicators.MovingAverage(Source, Ma2Periods, MovingAverageType.Exponential);
        }

        protected override void OnTick()
        {
            // Aktualisiere Trailing Stops für offene Positionen
            UpdateTrailingStops();

            // Prüfe, ob eine Position bereits offen ist
            if (Positions.Count > 0) return;

            // Berechne die MACD-Linie
            double macdLine = macd.Histogram.LastValue + macd.Signal.LastValue;

            // Kombinierte Bedingungen für Kauf- und Verkaufssignale
            bool isSellSignal = macd.Signal.Last(1) > macdLine && macd.Signal.Last(2) < (macd.Histogram.Last(2) + macd.Signal.Last(2)) && ma1.Result.LastValue > ma2.Result.LastValue && ma1.Result.Last(1) <= ma2.Result.Last(1) && Bars.LastBar.Close < Bars.LastBar.Open;
            bool isBuySignal = macd.Signal.Last(1) < macdLine && macd.Signal.Last(2) > (macd.Histogram.Last(2) + macd.Signal.Last(2)) && ma1.Result.LastValue < ma2.Result.LastValue && ma1.Result.Last(1) >= ma2.Result.Last(1) && Bars.LastBar.Close > Bars.LastBar.Open;

            if (isSellSignal)
            {
                // Verkaufssignal
                ExecuteMarketOrder(TradeType.Sell, SymbolName, Symbol.QuantityToVolumeInUnits(Quantity), "SampleMACD", StopLossPips, TakeProfitPips);
            }
            else if (isBuySignal)
            {
                // Kaufsignal
                ExecuteMarketOrder(TradeType.Buy, SymbolName, Symbol.QuantityToVolumeInUnits(Quantity), "SampleMACD", StopLossPips, TakeProfitPips);
            }
        }

        private void UpdateTrailingStops()
        {
            foreach (var position in Positions)
            {
                if (position.SymbolName != SymbolName || position.Label != "SampleMACD")
                    continue;

                double pipsProfit = position.NetProfit / Symbol.PipValue;

                if (pipsProfit >= TrailingStopStart)
                {
                    double newStopLossPrice;

                    if (position.TradeType == TradeType.Buy)
                    {
                        newStopLossPrice = position.EntryPrice + TrailingStopStart * Symbol.PipSize;
                        newStopLossPrice = Math.Max(newStopLossPrice, Symbol.Bid - TrailingStopDistance * Symbol.PipSize);
                    }
                    else
                    {
                        newStopLossPrice = position.EntryPrice - TrailingStopStart * Symbol.PipSize;
                        newStopLossPrice = Math.Min(newStopLossPrice, Symbol.Ask + TrailingStopDistance * Symbol.PipSize);
                    }

                    if (position.StopLoss == null || Math.Abs(newStopLossPrice - position.StopLoss.Value) >= Symbol.PipSize)
                    {
                        ModifyPosition(position, newStopLossPrice, position.TakeProfit);
                    }
                }
            }
        }
    }
}