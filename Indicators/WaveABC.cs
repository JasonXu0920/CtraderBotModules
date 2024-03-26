using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, ScalePrecision = 0)]
    public class Cyf_Wave_ABC : Indicator
    {
        private MacdCrossOver MAC1;
        private MacdCrossOver MAC2;
        private MacdCrossOver MAC3;
        private MacdCrossOver MAC4;
        private MacdCrossOver MAC5;
        private MacdCrossOver MAC6;

        [Parameter("WaveA", DefaultValue = true)]
        public bool Show_WaveA { get; set; }

        [Parameter("WaveB", DefaultValue = true)]
        public bool Show_WaveB { get; set; }

        [Parameter("WaveC", DefaultValue = true)]
        public bool Show_WaveC { get; set; }


        [Output("WaveC2", Color = Colors.LimeGreen, IsHistogram = true, Thickness = 3)]
        public IndicatorDataSeries WaveC2 { get; set; }

        [Output("WaveC1", Color = Colors.DarkGreen, IsHistogram = true, Thickness = 3)]
        public IndicatorDataSeries WaveC1 { get; set; }

        [Output("WaveB2", Color = Colors.Orange, IsHistogram = true, Thickness = 3)]
        public IndicatorDataSeries WaveB2 { get; set; }

        [Output("WaveB1", Color = Colors.SaddleBrown, IsHistogram = true, Thickness = 3)]
        public IndicatorDataSeries WaveB1 { get; set; }

        [Output("WaveA2", Color = Colors.DodgerBlue, IsHistogram = true, Thickness = 3)]
        public IndicatorDataSeries WaveA2 { get; set; }

        [Output("WaveA1", Color = Colors.Blue, IsHistogram = true, Thickness = 3)]
        public IndicatorDataSeries WaveA1 { get; set; }


        protected override void Initialize()
        {
            if (Show_WaveA)
            {
                MAC1 = Indicators.MacdCrossOver(34, 8, 34);
                MAC2 = Indicators.MacdCrossOver(55, 8, 55);
            }

            if (Show_WaveB)
            {

                MAC3 = Indicators.MacdCrossOver(89, 8, 89);
                MAC4 = Indicators.MacdCrossOver(144, 8, 144);
            }
            if (Show_WaveC)
            {
                MAC5 = Indicators.MacdCrossOver(233, 8, 233);
                MAC6 = Indicators.MacdCrossOver(377, 8, 377);
            }


        }

        public override void Calculate(int index)
        {
            if (Show_WaveA)
            {
                WaveA1[index] = MAC1.Histogram[index];
                WaveA2[index] = MAC2.Histogram[index];
            }
            if (Show_WaveB)
            {
                WaveB1[index] = MAC3.Histogram[index];
                WaveB2[index] = MAC4.Histogram[index];
            }
            if (Show_WaveC)
            {
                WaveC1[index] = MAC5.Histogram[index];
                WaveC2[index] = MAC6.MACD[index];
            }
        }
    }
}