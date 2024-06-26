using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace RiskManager
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class RiskManagementBot : Robot
    {
        [Parameter("Risk Percentage", DefaultValue = 1.0)]
        public double RiskPercentage { get; set; }

        private double accountBalance;

        protected override void OnStart()
        {
            // Initialize the account balance
            accountBalance = Account.Balance;

            // Subscribe to the events when pending orders are modified
            PendingOrders.Modified += OnPendingOrdersModified;
        }

        protected override void OnTick()
        {
            // Adjust the lot sizes of all pending orders
            foreach (var order in PendingOrders)
            {
                if (order.StopLoss.HasValue)
                {
                    AdjustOrderRisk(order);
                }
            }
        }

        private void OnPendingOrdersModified(PendingOrderModifiedEventArgs args)
        {
            // Adjust the lot size when a pending order is modified
            var order = args.PendingOrder;
            AdjustOrderRisk(order);
        }

        private void AdjustOrderRisk(PendingOrder order)
        {
            if (order.StopLoss.HasValue)
            {
                double riskAmount = (RiskPercentage / 100.0) * accountBalance;
                double stopLossPips = Math.Abs(order.TargetPrice - order.StopLoss.Value) / Symbol.PipSize;

                double lotSize = riskAmount / (stopLossPips * Symbol.PipValue);
                lotSize = RoundToNearestThousand(lotSize);
                if (lotSize < 1000)
                {
                    lotSize = 1000;
                }

                ModifyPendingOrderVolume(order, lotSize);
            }
        }

        private void ModifyPendingOrderVolume(PendingOrder order, double lotSize)
        {
            ModifyPendingOrder(order, order.TargetPrice, order.StopLossPips, order.TakeProfitPips, order.ExpirationTime, lotSize);
        }
        private double RoundToNearestThousand(double value)
        {
            return Math.Round(value / 1000) * 1000;
        }

        protected override void OnStop()
        {
            // Unsubscribe from the events
            PendingOrders.Modified -= OnPendingOrdersModified;
        }

    }
}