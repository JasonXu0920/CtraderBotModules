using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class TURTLE : Robot
    {
        [Parameter("Volume", DefaultValue = 1)]
        public int Volume { get; set; }

        [Parameter("dc Period1 - entry", DefaultValue = 20)]
        public int dcPeriod1 { get; set; }

        [Parameter("dc Period2 - exit", DefaultValue = 10)]
        public int dcPeriod2 { get; set; }

        [Parameter("ema Period1", DefaultValue = 1)]
        public int emaPeriod1 { get; set; }

        [Parameter("ema Period2", DefaultValue = 63)]
        public int emaPeriod2 { get; set; }

        [Parameter("atr Period", DefaultValue = 20)]
        public int atrPeriod { get; set; }

        private DonchianChannel dc1;
        private DonchianChannel dc2;

        private ExponentialMovingAverage ema1;
        private ExponentialMovingAverage ema2;

        private AverageTrueRange atr;

        protected override void OnStart()
        {
            dc1 = Indicators.DonchianChannel(dcPeriod1);
            dc2 = Indicators.DonchianChannel(dcPeriod2);

            ema1 = Indicators.ExponentialMovingAverage(MarketSeries.Close, emaPeriod1);
            ema2 = Indicators.ExponentialMovingAverage(MarketSeries.Close, emaPeriod2);

            atr = Indicators.AverageTrueRange(atrPeriod, MovingAverageType.Exponential);
        }

        protected override void OnTick()
        {
            int N = (int)(atr.Result.LastValue / Symbol.PipSize);

            if (Positions.Count == 0)
            {
                double stopLoss = 2 * N;
                if (Symbol.Ask > dc1.Top.LastValue)                /*&& ema1.Result.LastValue > ema2.Result.LastValue*/
                {
                    ExecuteMarketOrder(TradeType.Buy, Symbol.Name, Volume, "1", stopLoss, 0);
                }
                if (Symbol.Bid < dc1.Bottom.LastValue)                /*&& ema1.Result.LastValue < ema2.Result.LastValue*/
                {
                    ExecuteMarketOrder(TradeType.Sell, Symbol.Name, Volume, "1", stopLoss, 0);
                }
            }

            foreach (var position in Positions)
            {
                if (Symbol.Ask < dc2.Bottom.LastValue && position.TradeType == TradeType.Buy)
                {
                    ClosePosition(position);
                }
                if (Symbol.Bid > dc2.Top.LastValue && position.TradeType == TradeType.Sell)
                {
                    ClosePosition(position);
                }

                double step = N / 2;
                if (position.Pips > step && position.VolumeInUnits < 2 * Volume)
                {
                    ModifyPosition(position, 2 * Volume);
                }
                if (position.Pips > 2 * step && position.VolumeInUnits < 3 * Volume)
                {
                    ModifyPosition(position, 3 * Volume);
                }
                if (position.Pips > 3 * step && position.VolumeInUnits < 4 * Volume)
                {
                    ModifyPosition(position, 4 * Volume);
                }

                if (position.TradeType == TradeType.Buy)
                {
                    if (position.Pips > step && position.VolumeInUnits < 2 * Volume)
                    {
                        ModifyPosition(position, Symbol.Ask - 2 * N * Symbol.PipSize, 0);
                    }
                    if (position.Pips > 2 * step && position.VolumeInUnits < 3 * Volume)
                    {
                        ModifyPosition(position, Symbol.Ask - 2 * N * Symbol.PipSize, 0);
                    }
                    if (position.Pips > 3 * step && position.VolumeInUnits < 4 * Volume)
                    {
                        ModifyPosition(position, Symbol.Ask - 2 * N * Symbol.PipSize, 0);
                    }
                }
                if (position.TradeType == TradeType.Sell)
                {
                    if (position.Pips > step && position.VolumeInUnits < 2 * Volume)
                    {
                        ModifyPosition(position, Symbol.Bid + 2 * N * Symbol.PipSize, 0);
                    }
                    if (position.Pips > 2 * step && position.VolumeInUnits < 3 * Volume)
                    {
                        ModifyPosition(position, Symbol.Bid + 2 * N * Symbol.PipSize, 0);
                    }
                    if (position.Pips > 3 * step && position.VolumeInUnits < 4 * Volume)
                    {
                        ModifyPosition(position, Symbol.Bid + 2 * N * Symbol.PipSize, 0);
                    }
                }
            }
        }

        protected override void OnStop()
        {
            foreach (var position in Positions)
            {
                ClosePositionAsync(position);
            }
            foreach (var pendingOrder in PendingOrders)
            {
                CancelPendingOrderAsync(pendingOrder);
            }
        }


    }
}