sing System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class TickMotioncBot : Robot
    {
        [Parameter(DefaultValue = 0.01)]
        public double Vol { get; set; }

        [Parameter(DefaultValue = 100)]
        public int Ticks { get; set; }

        [Parameter(DefaultValue = 0.5)]
        public double Decision { get; set; }

        [Parameter(DefaultValue = 0.05)]
        public double DecisionDelta { get; set; }

        [Parameter(DefaultValue = 5)]
        public int Tries { get; set; }

        [Parameter(DefaultValue = 20)]
        public double MinProfit { get; set; }

        [Parameter(DefaultValue = 100)]
        public double SL { get; set; }


        private int _tickNo;
        private int _try;
        private Random _random = new Random(10232);
        private string _label = "TickMotion";
        private int _win = 0;

        protected override void OnStart()
        {
            Positions.Closed += PositionsOnClosed;
            // Put your initialization logic here
        }

        private void PositionsOnClosed(PositionClosedEventArgs positionClosedEventArgs)
        {
            if (positionClosedEventArgs.Reason == PositionCloseReason.StopLoss)
            {
                var d = DecisionDelta * (positionClosedEventArgs.Position.TradeType == TradeType.Buy ? 1 : -1);
                Decision += d;
                _win = 0;
            }
        }

        protected override void OnTick()
        {
            if (_tickNo == 0)
            {
                TradeType tradeType;
                double r = 0.5;
                //_random.NextDouble();
                if (r > Decision)
                {
                    tradeType = TradeType.Buy;
                }
                else
                {
                    tradeType = TradeType.Sell;
                }

                Print(tradeType + " " + Decision);
                var volumeInUnits = Symbol.QuantityToVolumeInUnits(Vol);
                ExecuteMarketOrder(tradeType, SymbolName, volumeInUnits, _label, SL, SL * 3);
            }

            if (_tickNo == Ticks)
            {
                var longPositions = Positions.FindAll(_label, SymbolName, TradeType.Buy);
                var shortPositions = Positions.FindAll(_label, SymbolName, TradeType.Sell);
                if (longPositions.Any())
                {
                    var pos = longPositions[0];
                    if (pos.NetProfit > MinProfit)
                    {
                        Decision -= DecisionDelta;
                        _tickNo = -1;
                        pos.Close();
                        _try = 0;
                        _win++;
                        if (_win > 2)
                        {
                            Decision = 1 - Decision;
                            _win = 0;
                        }
                    }
                    else if (_try > Tries)
                    {
                        Decision += DecisionDelta;
                        pos.Close();
                        _tickNo = -1;
                        _try = 0;
                    }
                    else
                    {
                        _try++;
                        _tickNo = 0;
                    }
                }
                else if (shortPositions.Any())
                {
                    var pos = shortPositions[0];
                    if (pos.NetProfit > MinProfit)
                    {
                        Decision += DecisionDelta;
                        _tickNo = -1;
                        pos.Close();
                        _try = 0;
                    }
                    else if (_try > Tries)
                    {
                        Decision -= DecisionDelta;
                        pos.Close();
                        _tickNo = -1;
                        _try = 0;
                    }
                    else
                    {
                        _try++;
                        _tickNo = 0;
                    }
                }
                else
                {
                    _tickNo = -1;
                    _try = 0;
                }

                Decision = Math.Max(0, Math.Min(1, Decision));

            }

            _tickNo++;
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}