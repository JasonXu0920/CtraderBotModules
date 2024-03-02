using cAlgo.API;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class SampleSMA : Indicator
    {
        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 14)]
        public int Periods { get; set; }

        public enum Option
        {
            SMA,
            EMA
        }
        [Parameter("MA Option", DefaultValue = Option.SMA)]
        public Option SelectedOption { get; set; }

        [Output("Main", LineColor = "Turquoise")]
        public IndicatorDataSeries Result { get; set; }

        private double exp;
        protected override void Initialize()
        {
            exp = 2.0 / (Periods + 1);
        }

        public override void Calculate(int index)
        {   
            if(SelectedOption == "SMA")
            {
                var sum = 0.0;

                for (var i = index - Periods + 1; i <= index; i++)
                    sum += Source[i];

                Result[index] = sum / Periods;
            }

            if(SelectedOption == "EMA")
            {
                var previousValue = Result[index - 1];

                if (double.IsNaN(previousValue))
                    Result[index] = Source[index];
                else
                    Result[index] = Source[index] * exp + previousValue * (1 - exp);
            }

        }
    }
}