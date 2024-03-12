using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Trend : Indicator
    {
        [Parameter(DefaultValue = 24)]
        public int Period { get; set; }

        [Parameter()]
        public DataSeries Series { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        private MovingAverage _ma;
        private IndicatorDataSeries _lsma;

        protected override void Initialize()
        {
            _ma = Indicators.MovingAverage(Series, Period * 4, MovingAverageType.Simple);
            _lsma = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            CalculateLSMA(index);
            double sum = 0;
            for (int i = 0; i < Period; i++)
            {
                var x = _ma.Result[index - i - 1] - _lsma[index - i];
                sum += Math.Abs(x);
            }

            Result[index] = sum / Period;
        }

        private void CalculateLSMA(int idxf)
        {
            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumCodeviates = 0;
            var period = Period * 2;
            for (int xi = 0; xi < period; xi++)
            {
                int idx = xi + idxf - period;
                var y = Series[idx];
                sumCodeviates += xi * y;
                sumOfX += xi;
                sumOfY += y;
                sumOfXSq += xi * (double)xi;
            }

            var ssX = sumOfXSq - sumOfX * sumOfX / period;

            var sCo = sumCodeviates - sumOfX * sumOfY / period;

            var meanX = sumOfX / period;
            var meanY = sumOfY / period;

            var yIntercept = meanY - sCo / ssX * meanX;
            var slope = sCo / ssX;
            var result = slope * Period + yIntercept;

            _lsma[idxf] = result;
        }
    }
}