using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class HedgingMartingale : Robot
    {
        [Parameter("Initial Quantity (Lots)", DefaultValue = 0.01, MinValue = 0.01, Step = 0.01)]
        public double InitialQuantity { get; set; }

        [Parameter("Restart Initial Quantity (Lots)", DefaultValue = 0.01, MinValue = 0.01, Step = 0.01)]
        public double RestartInitialQuantity { get; set; }

        [Parameter("Take Profit / StopLoss", DefaultValue = 40)]
        public int TakeProfitStopLoss { get; set; }

        [Parameter("Max Loss In Row", DefaultValue = 7)]
        public int MaxLossInRow { get; set; }

        private Random random = new Random();
        private int LossInRowLong = 0;
        private int LossInRowShort = 0;

        protected override void OnStart()
        {
            Positions.Closed += OnPositionsClosed;

            var position = Positions.Find("HedgingMartingale");

            ExecuteOrder(RestartInitialQuantity, TradeType.Buy);
            ExecuteOrder(RestartInitialQuantity, TradeType.Sell);
        }

        private void ExecuteOrder(double quantity, TradeType tradeType)
        {
            Print("The Spread of the symbol is: {0}", Symbol.Spread);
            var volumeInUnits = Symbol.QuantityToVolume(quantity);
            var result = ExecuteMarketOrder(tradeType, Symbol, volumeInUnits, "HedgingMartingale", TakeProfitStopLoss, TakeProfitStopLoss);

            if (result.Error == ErrorCode.NoMoney)
                Stop();
        }

        private void OnPositionsClosed(PositionClosedEventArgs args)
        {
            Print("Closed");
            var position = args.Position;

            if (position.Label != "HedgingMartingale" || position.SymbolCode != Symbol.Code)
                return;

            if (position.GrossProfit > 0)
            {
                if (position.TradeType == TradeType.Buy)
                {
                    LossInRowLong = 0;
                }

                else if (position.TradeType == TradeType.Sell)
                {
                    LossInRowShort = 0;
                }

                ExecuteOrder(InitialQuantity, position.TradeType);
            }
            else
            {
                if (position.TradeType == TradeType.Buy)
                {
                    LossInRowLong = LossInRowLong + 1;
                }

                else if (position.TradeType == TradeType.Sell)
                {
                    LossInRowShort = LossInRowShort + 1;
                }

                if (LossInRowLong > MaxLossInRow || LossInRowShort > MaxLossInRow)
                {
                    ExecuteOrder(InitialQuantity, position.TradeType);
                }
                else
                {
                    ExecuteOrder(position.Quantity * 2, position.TradeType);
                }
            }
        }

        private TradeType GetRandomTradeType()
        {
            return random.Next(2) == 0 ? TradeType.Buy : TradeType.Sell;
        }
    }
}