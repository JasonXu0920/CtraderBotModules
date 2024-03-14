using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class FreeFallcBot : Robot
    {
        [Parameter("Gravity", Group = "Fall", DefaultValue = 1)]
        public double Gravity { get; set; }
        
        [Parameter("Friction when no open pos", Group = "Fall", DefaultValue = 0.01)]
        public double FrictionNoOpenPos { get; set; }

        [Parameter("Friction", Group = "Fall", DefaultValue = 0.1)]
        public double Friction { get; set; }

        [Parameter("Threshold", Group = "Fall", DefaultValue = 0.0002)]
        public double Threshold { get; set; }

        [Parameter("BarMode", Group = "Action", DefaultValue = false)]
        public bool BarMode { get; set; }

        [Parameter("Volume", Group = "Money", DefaultValue = 0.01)]
        public double Volume { get; set; }

        private const string Label = "PriceAction cBot";


        private double _VYDown;
        private double _VYUp;
        private double _posYDown;
        private double _posYUp;
        private double _prevPrice;
        
        private AverageTrueRange _atr;

        protected override void OnStart()
        {
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Simple);
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
            double price;
            if (BarMode == false)
            {
                var ticks = MarketData.GetTicks();
                var tick = ticks.LastTick;
                price = (tick.Ask + tick.Bid) / 2;
            }
            else
            {
                price = Bars.Last(0).Close;
            }


            if (Math.Abs(_posYUp) < 0.0000001)
            {
                _posYUp = price;
                _posYDown = price;
                _prevPrice = price;
                return;
            }

            var slope = Math.Abs(price - _prevPrice);
            var sign = Math.Sign(price - _prevPrice);
            var minPips = _atr.Result.LastValue; //Symbol.TickSize;
            var diff = slope / minPips;
            var alpha = Math.Atan(diff);
            var delta = Math.Sin(alpha);

            var g = delta * Gravity * minPips;
            
            _prevPrice = price;
            
            var shortPositions = Positions.FindAll(Label, SymbolName, TradeType.Sell);
            var longPositions = Positions.FindAll(Label, SymbolName, TradeType.Buy);

            bool hasOpenPos = shortPositions.Any() || longPositions.Any();
            var friction = hasOpenPos ? Friction : FrictionNoOpenPos;

            if (price <= _posYDown && sign < 0)
            {
                _VYDown += g;
            }
            else
            {
                _VYDown -= g * friction;
                _posYDown = price;
            }

            if (price >= _posYUp && sign > 0)
            {
                _VYUp += g;
            }
            else
            {
                _VYUp -= g * friction;
                _posYUp = price;
            }

            if (_VYDown < 0) _VYDown = 0;
            if (_VYUp < 0) _VYUp = 0;

            _posYDown -= _VYDown;
            _posYUp += _VYUp;
            
            //Print("{0:0.0000000} {1:0.0000000} {2:0.0000000} {3:0.0000000} {4:0.0000000} {5:0.0000000}", _VYDown, _VYUp, g, _posYDown, _posYUp, price);
            var threshold = Threshold * _atr.Result.LastValue;
            if (_VYDown > threshold && _VYDown > _VYUp)
            {
                // SELL
                if (!hasOpenPos)
                {
                    var volumeInUnits = Symbol.QuantityToVolumeInUnits(Volume);
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, Label);
                }
            }
            else if (Math.Abs(_VYDown) < 0.0000001)
            {
                var shortPos = shortPositions.FirstOrDefault();
                shortPos?.Close();
            }

            if (_VYUp > threshold && _VYUp >= _VYDown)
            {
                // BUY
                if (!hasOpenPos)
                {
                    var volumeInUnits = Symbol.QuantityToVolumeInUnits(Volume);
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, Label);
                }
            }
            else if (Math.Abs(_VYUp) < 0.0000001)
            {
                var longPos = longPositions.FirstOrDefault();
                longPos?.Close();
            }
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
        public static double GetRiskPercentage(this Symbol symbol, double stopLossInPips, double accountBalance,
            double volume)
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
        public static double GetStopLoss(this Symbol symbol, double riskPercentage, double accountBalance,
            double volume)
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
        public static double GetVolume(this Symbol symbol, double riskPercentage, double accountBalance,
            double stopLossInPips)
        {
            return symbol.NormalizeVolumeInUnits(riskPercentage /
                                                 (Math.Abs(stopLossInPips) * symbol.PipValue / accountBalance * 100));
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