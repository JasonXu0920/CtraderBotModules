using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class PriceActioncBot : Robot
    {
        [Parameter("Width", Group = "Action", DefaultValue = 0.001)]
        public double Width { get; set; }

        [Parameter("Candles", Group = "Action", DefaultValue = 3)]
        public int Candles { get; set; }

        [Parameter("SL Candles", Group = "Action", DefaultValue = 1)]
        public double SLCandles { get; set; }

        [Parameter("SL Resistance mode", Group = "Action", DefaultValue = false)]
        public bool SLResistanceMode { get; set; }

        [Parameter("TP Candles", Group = "Action", DefaultValue = 1)]
        public double TPCandles { get; set; }

        [Parameter("Sl Resistance Protection factor", Group = "Action", DefaultValue = 1)]
        public double SLResistanceProtectionFactor { get; set; }

        [Parameter("BarMode", Group = "Action", DefaultValue = false)]
        public bool BarMode { get; set; }

        [Parameter("Volume multiplier", Group = "Money", DefaultValue = 1)]
        public double VolumeMultiplier { get; set; }

        [Parameter("Volume shift", Group = "Money", DefaultValue = 0)]
        public double VolumeShift { get; set; }

        [Parameter("Volume range count", Group = "Money", DefaultValue = 20)]
        public int VolumeRangeCount { get; set; }

        [Parameter("Risk", Group = "Money", DefaultValue = 1)]
        public double Risk { get; set; }

        [Parameter("Volume based risk", Group = "Money", DefaultValue = false)]
        public bool VolumeBasedRisk { get; set; }

        [Parameter("Trend indicator period", DefaultValue = 24, Group = "Trend indicator")]
        public int TIPeriod { get; set; }

        [Parameter("Trend indicator timeframe", DefaultValue = null, Group = "Trend indicator")]
        public TimeFrame TITimeframe { get; set; }

        [Parameter("Use Trend indicator", DefaultValue = false, Group = "Trend indicator")]
        public bool UseTrendIndicator { get; set; }

        private Candles _candles;

        private const string Label = "PriceAction cBot";

        private TrendIndicator _trendIndicator;

        private double _riskSum;
        private double _riskDiff;
        private int _trades;
        private double _prevPrice;

        protected override void OnStart()
        {
            _candles = new Candles();
            _trendIndicator = Indicators.GetIndicator<TrendIndicator>(TIPeriod, MarketData.GetBars(TITimeframe).ClosePrices);
        }

        protected override void OnTick()
        {
            if (BarMode)
                return;
            OnTrade();
        }

        protected override void OnBar()
        {
            if (!BarMode)
                return;
            OnTrade();
        }

        private void OnTrade()
        {
            try
            {
                double price;
                long volume;
                long time;
                if (BarMode == false)
                {
                    var ticks = MarketData.GetTicks();
                    var tick = ticks.LastTick;
                    price = CandleHelpers.GetPrice(tick);
                    volume = 1;
                    time = CandleHelpers.TotalMillisecs(tick.Time);
                }
                else
                {
                    price = Bars.Last(1).Close;
                    volume = (long)Bars.TickVolumes.Last(1);
                    time = CandleHelpers.TotalMillisecs(Bars.LastBar.OpenTime);
                }

                if (Math.Abs(price - _prevPrice) < 1E-07)
                    return;
                _prevPrice = price;

                if (!_candles.Tick(price, volume, time, Width))
                    return;

                if (UseTrendIndicator && _trendIndicator.Result.IsFalling())
                {
                    return;
                }

                var slInPips = Math.Max(1, Symbol.ToPips(SLCandles * Width));
                var tpInPips = Math.Max(1, Symbol.ToPips(TPCandles * Width));
                var longPositions = Positions.FindAll(Label, SymbolName, TradeType.Buy);
                var shortPositions = Positions.FindAll(Label, SymbolName, TradeType.Sell);

                var hasOpenPositionsLong = longPositions.Length > 0;
                var hasOpenPositionsShort = shortPositions.Length > 0;
                var hasOpenPositions = hasOpenPositionsLong || hasOpenPositionsShort;
                var upNContinous = _candles.GetLastNContinous(CandleDirection.Up);

                if (upNContinous > Candles && !hasOpenPositions)
                {
                    if (SLResistanceMode)
                    {
                        slInPips = GetStopLossInPipsBasedOnResistance(CandleDirection.Up, price, slInPips);
                    }

                    var volumeInUnits = GetVolumeInUnits(upNContinous, slInPips);
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, Label, slInPips, tpInPips);
                    return;
                }

                var downNContinous = _candles.GetLastNContinous(CandleDirection.Down);

                if (downNContinous > Candles && !hasOpenPositions)
                {
                    if (SLResistanceMode)
                    {
                        slInPips = GetStopLossInPipsBasedOnResistance(CandleDirection.Down, price, slInPips);
                    }

                    var volumeInUnits = GetVolumeInUnits(downNContinous, slInPips);
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, Label, slInPips, tpInPips);
                    return;
                }

                if (!_candles.CheckHasNewResistance())
                    return;

                foreach (var longPosition in longPositions)
                {
                    var stopLoss = GetStopLossBasedOnResistance(CandleDirection.Up);
                    if (stopLoss == null)
                        continue;

                    if (longPosition.StopLoss < stopLoss)
                    {
                        ModifyPosition(longPosition, stopLoss, null);
                    }
                }

                foreach (var shortPosition in shortPositions)
                {
                    var stopLoss = GetStopLossBasedOnResistance(CandleDirection.Down);
                    if (stopLoss == null)
                        continue;

                    if (shortPosition.StopLoss > stopLoss)
                    {
                        ModifyPosition(shortPosition, stopLoss, null);
                    }
                }
            } catch (Exception ex)
            {
                Print(ex);
                Stop();
            }
        }

        private double? GetStopLossBasedOnResistance(CandleDirection direction)
        {
            var resistance = _candles.GetLastResistance(direction, Width);
            if (resistance == null)
                return null;

            return direction == CandleDirection.Up ? resistance.Price - SLResistanceProtectionFactor * Width : resistance.Price + SLResistanceProtectionFactor * Width;
        }

        private double GetStopLossInPipsBasedOnResistance(CandleDirection direction, double currPrice, double defaultValue)
        {
            var stopLoss = GetStopLossBasedOnResistance(direction);
            return stopLoss.HasValue ? Math.Abs(Symbol.ToPips(stopLoss.Value - currPrice)) : defaultValue;
        }

        private double GetVolumeInUnits(int upNContinous, double slInPips)
        {
            double volumeInUnits;
            if (VolumeBasedRisk)
            {
                var risk = GetVolumeRisk(upNContinous);
                volumeInUnits = Symbol.GetVolume(risk, Account.Balance, slInPips);
                _riskSum += risk;
                _riskDiff += volumeInUnits / Symbol.GetVolume(Risk, Account.Balance, slInPips);
            }
            else
            {
                volumeInUnits = Symbol.GetVolume(Risk, Account.Balance, slInPips);
                _riskSum += Risk;
            }

            _trades++;
            return volumeInUnits;
        }

        private double GetVolumeRisk(int checkN)
        {
            var minMax = _candles.GetVolumeToTicksRange(VolumeRangeCount);
            var volumeToTicks = _candles.GetAverageVolumeToTicks(checkN);
            if (Math.Abs(minMax.Item2 - minMax.Item1) < 1E-05)
                return Risk / 10;
            var volumeDiff = 2 * (volumeToTicks - minMax.Item1) / (minMax.Item2 - minMax.Item1) - 1;
            var volumeFunc = CandleHelpers.Sigmoid(volumeDiff * VolumeMultiplier + VolumeShift);
            var risk = Risk * volumeFunc;
            return risk;
        }

        protected override void OnStop()
        {
            var longPositions = Positions.FindAll(Label, SymbolName, TradeType.Buy);
            var shortPositions = Positions.FindAll(Label, SymbolName, TradeType.Sell);
            foreach (var longPosition in longPositions)
            {
                longPosition.Close();
            }

            foreach (var shortPosition in shortPositions)
            {
                shortPosition.Close();
            }

            if (_trades > 0)
            {
                Print("AVG RISK = {0}", _riskSum / _trades);
                Print("AVG RISK Diff = {0}", _riskDiff / _trades);
            }
        }
    }

    public enum CandleDirection
    {
        Up,
        Down
    }

    public enum ResistanceType
    {
        Top,
        Bottom
    }

    public class Candle
    {
        public CandleDirection Direction { get; set; }
        public int Normalized { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public long Volume { get; set; }
        public long Ticks { get; set; }

        public double VolumeToTicksRatio
        {
            get { return Ticks > 0 ? (double)Volume / Ticks : 0; }
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4} {5} {6}", Direction, Normalized, Min, Max, Volume, Ticks, VolumeToTicksRatio);
        }
    }

    public class Resistance
    {
        public ResistanceType ResistanceType { get; set; }
        public double Price { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", ResistanceType, Price);
        }
    }

    public class Candles
    {
        public Candle Last
        {
            get { return _items.LastOrDefault() ?? _current; }
        }

        public Candle Prev(int n)
        {
            return _items.Count > n ? _items[_items.Count - n] : _current;
        }

        private Candle _current;
        private long _currentTime;
        private readonly List<Candle> _items;
        private readonly List<Resistance> _resistance;
        private bool _hasNewResistance;

        public Candles()
        {
            _items = new List<Candle>();
            _resistance = new List<Resistance>();
        }

        public bool Tick(double price, long volume, long time, double width)
        {
            var normalizedValue = CandleHelpers.NormalizePrice(price, width);
            if (_current == null)
            {
                _currentTime = time;
                _current = new Candle 
                {
                    Normalized = normalizedValue,
                    Min = price,
                    Max = price
                };
                return false;
            }

            CandleDirection? direction = null;

            if (normalizedValue > _current.Normalized + 1)
            {
                direction = CandleDirection.Up;
            }
            else if (normalizedValue < _current.Normalized - 1)
            {
                direction = CandleDirection.Down;
            }

            _current.Min = Math.Min(_current.Min, price);
            _current.Max = Math.Max(_current.Max, price);
            _current.Volume += volume;
            _current.Ticks += time - _currentTime;
            _currentTime = time;

            if (direction != null)
            {
                if (_current.Direction == CandleDirection.Up && direction == CandleDirection.Down)
                {
                    _resistance.Add(new Resistance 
                    {
                        ResistanceType = ResistanceType.Top,
                        Price = _current.Max
                    });
                    _hasNewResistance = true;
                }
                else if (_current.Direction == CandleDirection.Down && direction == CandleDirection.Up)
                {
                    _resistance.Add(new Resistance 
                    {
                        ResistanceType = ResistanceType.Bottom,
                        Price = _current.Min
                    });
                    _hasNewResistance = true;
                }

                int @add = direction == CandleDirection.Up ? 1 : -1;
                for (int currNorm = _current.Normalized + @add; currNorm != normalizedValue; currNorm += @add)
                {
                    _items.Add(new Candle 
                    {
                        Direction = direction.Value,
                        Normalized = currNorm,
                        Min = price,
                        Max = price
                    });
                }

                _current = _items.Last();
                return true;
            }

            return false;
        }

        public int GetLastNContinous(CandleDirection direction)
        {
            return Enumerable.Reverse(_items).TakeWhile(x => x.Direction == direction).Count();
        }

        public double GetAverageVolumeToTicks(int n)
        {
            return Enumerable.Reverse(_items).Take(n).Average(x => x.VolumeToTicksRatio);
        }

        public Tuple<double, double> GetVolumeToTicksRange(int n)
        {
            var range = Enumerable.Reverse(_items).Take(n).ToList();
            return new Tuple<double, double>(range.Min(x => x.VolumeToTicksRatio), range.Max(x => x.VolumeToTicksRatio));
        }

        public Resistance GetLastResistance(CandleDirection direction, double width)
        {
            ResistanceType resistanceTypeSearch;
            double m;
            if (direction == CandleDirection.Up)
            {
                resistanceTypeSearch = ResistanceType.Bottom;
                m = 1;
            }
            else
            {
                resistanceTypeSearch = ResistanceType.Top;
                m = -1;
            }

            var price = CandleHelpers.NormalizePriceRev(_current.Normalized, width);
            return _resistance.FindLast(x => x.ResistanceType == resistanceTypeSearch && (x.Price - price) * m < 0);
        }

        public bool CheckHasNewResistance()
        {
            if (_hasNewResistance)
            {
                _hasNewResistance = false;
                return true;
            }

            return false;
        }
    }

    public static class CandleHelpers
    {
        public static int NormalizePrice(double val, double width)
        {
            return (int)Math.Floor(val * (1.0 / width));
        }

        public static double NormalizePriceRev(int val, double width)
        {
            return (double)val * width;
        }

        public static double GetPrice(Tick tick)
        {
            return (tick.Ask + tick.Bid) / 2;
        }

        public static long TotalMillisecs(DateTime date)
        {
            return (long)(date - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        public static double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }
    }

    public static class SymbolExtensions
    {
        /// <summary>
        /// Returns a symbol pip value
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>double</returns>
        public static double GetPip(this Symbol symbol)
        {
            return symbol.TickSize / symbol.PipSize * Math.Pow(10, symbol.Digits);
        }

        /// <summary>
        /// Returns a price value in terms of pips
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="price">The price level</param>
        /// <returns>double</returns>
        public static double ToPips(this Symbol symbol, double price)
        {
            return price * symbol.GetPip();
        }

        /// <summary>
        /// Returns a price value in terms of ticks
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="price">The price level</param>
        /// <returns>double</returns>
        public static double ToTicks(this Symbol symbol, double price)
        {
            return price * Math.Pow(10, symbol.Digits);
        }

        /// <summary>
        /// Returns the amount of risk percentage based on stop loss amount
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="stopLossInPips">Stop loss amount in Pips</param>
        /// <param name="accountBalance">The account balance</param>
        /// <param name="volume">The volume amount in units (Not lots)</param>
        /// <returns>double</returns>
        public static double GetRiskPercentage(this Symbol symbol, double stopLossInPips, double accountBalance, double volume)
        {
            return Math.Abs(stopLossInPips) * symbol.PipValue / accountBalance * 100.0 * volume;
        }

        /// <summary>
        /// Returns the amount of stop loss in Pips based on risk percentage amount
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="riskPercentage">Risk percentage amount</param>
        /// <param name="accountBalance">The account balance</param>
        /// <param name="volume">The volume amount in units (Not lots)</param>
        /// <returns>double</returns>
        public static double GetStopLoss(this Symbol symbol, double riskPercentage, double accountBalance, double volume)
        {
            return riskPercentage / (symbol.PipValue / accountBalance * 100.0 * volume);
        }

        /// <summary>
        /// Returns the amount of volume based on your provided risk percentage and stop loss
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="riskPercentage">Risk percentage amount</param>
        /// <param name="accountBalance">The account balance</param>
        /// <param name="stopLossInPips">Stop loss amount in Pips</param>
        /// <returns>double</returns>
        public static double GetVolume(this Symbol symbol, double riskPercentage, double accountBalance, double stopLossInPips)
        {
            return symbol.NormalizeVolumeInUnits(riskPercentage / (Math.Abs(stopLossInPips) * symbol.PipValue / accountBalance * 100));
        }

        /// <summary>
        /// Rounds a price level to the number of symbol digits
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="price">The price level</param>
        /// <returns>double</returns>
        public static double Round(this Symbol symbol, double price)
        {
            return Math.Round(price, symbol.Digits);
        }
    }
}