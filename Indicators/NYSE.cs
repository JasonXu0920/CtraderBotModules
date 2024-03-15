using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class NYSE : Indicator
    {
        [Parameter(DefaultValue = 13)]
        public double UTCHour { get; set; }

        [Parameter(DefaultValue = 30)]
        public double UTCMinute { get; set; }

        [Parameter(DefaultValue = 3)]
        public int Minute { get; set; }

        [Parameter(DefaultValue = 60)]
        public int LineLength { get; set; }

        [Parameter(DefaultValue = "White")]
        public string kleur { get; set; }



        public double price;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
        }

        public override void Calculate(int index)
        {
            if (Bars.OpenTimes.LastValue.Hour == UTCHour && Bars.OpenTimes.LastValue.Minute == UTCMinute)
            {
                price = Bars.OpenPrices.LastValue;
                Chart.DrawTrendLine(Convert.ToString(price), Bars.OpenTimes.LastValue, price, Bars.OpenTimes.LastValue.AddMinutes(LineLength), price, kleur);
            }

            if (Bars.OpenTimes.LastValue.Hour == UTCHour && Bars.OpenTimes.LastValue.Minute == UTCMinute + Minute)
            {
                Chart.DrawTrendLine(Convert.ToString(price), Bars.OpenTimes.LastValue, price, Bars.OpenTimes.LastValue.AddMinutes(LineLength), price, kleur);
            }

        }
    }
}