using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Indicator(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SampleBullsPower : Indicator
    {
        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 13, MinValue = 2)]
        public int Periods { get; set; }

        [Parameter("MA Type", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MAType { get; set; }

        [Output("Result", LineColor = "Orange", PlotType = PlotType.Histogram)]
        public IndicatorDataSeries Result { get; set; }

        private MovingAverage movingAverage;

        protected override void Initialize()
        {
            movingAverage = Indicators.MovingAverage(Source, Periods, MAType);
        }

        public override void Calculate(int index)
        {
            Result[index] = Bars.HighPrices[index] - movingAverage.Result[index];
        }
    }
}