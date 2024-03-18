using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class mcginley : Indicator
    {
        [Parameter()]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 10)]
        public int Period { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        private IndicatorDataSeries price;

        protected override void Initialize()
        {
            price = CreateDataSeries();

        }

        public override void Calculate(int index)
        {

            if (index < 10)
            {
                price[index] = MarketSeries.Close.LastValue;
                Result[index] = MarketSeries.Close.LastValue;
                return;
            }
            price[index] = (MarketSeries.High[index] + MarketSeries.Low[index]) / 2;

            Result[index] = Result[index - 1] + (Source[index] - Result[index - 1]) / (0.6 * Period * Math.Pow(Source[index] / Result[index - 1], 4));
        }
    }
}