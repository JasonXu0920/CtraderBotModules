using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(ScalePrecision = 3, IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class BAMMRenkoTrend : Indicator
    {

        [Parameter("Entry - Only above/below major trend", DefaultValue = true)]
        public bool UseMajorTrendMA { get; set; }

        [Parameter("Entry - All MAs must point in trend direction", DefaultValue = true)]
        public bool MATrendDirection { get; set; }

        [Parameter("Entry - Minor MAs must point in trend direction", DefaultValue = true)]
        public bool MinorMATrendDirection { get; set; }

        [Parameter("Entry - Use ADX Down", DefaultValue = true)]
        public bool UseADXDown { get; set; }

        [Parameter("MA01 Type", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MAType1 { get; set; }

        [Parameter("MA01 Period", DefaultValue = 16)]
        public int MAPeriod1 { get; set; }

        [Parameter("MA02 Type", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MAType2 { get; set; }

        [Parameter("MA02 Period", DefaultValue = 8)]
        public int MAPeriod2 { get; set; }

        [Parameter("MA03 Type (Trend)", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MAType3 { get; set; }

        [Parameter("MA03 Period", DefaultValue = 64)]
        public int MAPeriod3 { get; set; }

        [Parameter("ADX Period", DefaultValue = 6)]
        public int ADXPeriod { get; set; }

        [Parameter("ADX Level", DefaultValue = 32)]
        public int ADXLevel { get; set; }

        [Parameter("Show winners/loosers", DefaultValue = true)]
        public bool ShowWinLoose { get; set; }

        [Parameter("Show RRR", DefaultValue = true)]
        public bool ShowRatio { get; set; }

        [Parameter("Show win/loose pips", DefaultValue = false)]
        public bool ShowWinLoosePips { get; set; }

        [Parameter("Show Steps", DefaultValue = true)]
        public bool ShowSteps { get; set; }

        [Parameter("Draw Icon", DefaultValue = true)]
        public bool DrawIcon { get; set; }

        [Parameter("Renko Block size", DefaultValue = 4)]
        public int RenkoBlockSize { get; set; }

        [Parameter("Spread in pips", DefaultValue = 0.5)]
        public double Spread { get; set; }

        [Output("Steps", LineColor = "Blue")]
        public IndicatorDataSeries Result { get; set; }

        [Output("Loosers", LineColor = "Red")]
        public IndicatorDataSeries Loosers { get; set; }

        [Output("Winners", LineColor = "Green")]
        public IndicatorDataSeries Winners { get; set; }

        [Output("LoosePips", LineColor = "Yellow")]
        public IndicatorDataSeries LoosePips { get; set; }  

        [Output("WinPips", LineColor = "Yellow")]
        public IndicatorDataSeries WinPips { get; set; }  

        [Output("RRRatio", LineColor = "Yellow")]
        public IndicatorDataSeries RRRatio { get; set; }

        [Output("AvgRRRatio", LineColor = "Yellow")]
        public IndicatorDataSeries AvgRRRatio { get; set; }

        [Parameter()]
        public DataSeries Source { get; set; }

        private MovingAverage MA1;
        private MovingAverage MA2;
        private MovingAverage MA3;
        private DirectionalMovementSystem DMS;
        private Bar bar;
        private int TrendSignal = 0;
        private double _loosePips = 0;
        private double _looseTrades = 0;
        private double _winPips = 0;
        private double _winTrades = 0;
        private double _avgrrr = 0;

        protected override void Initialize()
        {
            TrendSignal = 0;

            MA1 = Indicators.MovingAverage(Source, MAPeriod1, MAType1);
            MA2 = Indicators.MovingAverage(Source, MAPeriod2, MAType2);
            MA3 = Indicators.MovingAverage(Source, MAPeriod3, MAType3);
            DMS = Indicators.DirectionalMovementSystem(ADXPeriod);

        }

        public override void Calculate(int index)
        {

            if (index == 0)
            {
                return;
            }

            bar = Bars[index];

            if (IsEntryPossible(index) == true)
            {
                UpdateDirection(index);
                
                if(DrawIcon == true){
                    Chart.DrawIcon(bar.OpenTime.ToString(), ChartIconType.Star, index, bar.High, Color.Blue);
                }
                Result[index] = ShowSteps == true ? TrendSignal : 0;
            }
            else
            {

                int _absTrend = Math.Abs(TrendSignal);

                if(_absTrend == 1) {
                    _loosePips = (_loosePips + (2 * RenkoBlockSize) + (2*Spread));
                    _looseTrades = _looseTrades + 2;
                } else if(_absTrend == 2) {
                    _loosePips = (_loosePips + (1 * RenkoBlockSize) + (2*Spread));
                    _looseTrades = _looseTrades + 1;
                } else if(_absTrend > 3) {
                    _winTrades = _winTrades + 1;
                    _winPips = _winPips + ((_absTrend * RenkoBlockSize) - (2*Spread));
                }

                double _rrr = (_winPips/_loosePips);

                TrendSignal = 0;
                Result[index] = 0;
                Loosers[index] = ShowWinLoose == true ? _looseTrades : 0;
                LoosePips[index] = ShowWinLoosePips == true ? _loosePips : 0;
                Winners[index] = ShowWinLoose == true ? _winTrades : 0;
                WinPips[index] = ShowWinLoosePips == true ? _winPips : 0;
                RRRatio[index] = ShowRatio == true ? _rrr : 0;
                _avgrrr = _avgrrr + _rrr;
                AvgRRRatio[index] = ShowRatio == true ? (_avgrrr/index) : 0;
            }

        }
        private bool isGreenCandle(double lastBarOpen, double lastBarClose)
        {
            return (lastBarOpen < lastBarClose) ? true : false;
        }

        private void UpdateDirection(int index)
        {

            try
            {
                Bar thisCandle = Bars[index];
                Bar lastCandle = Bars[index - 1];

                bool thisCandleGreen = isGreenCandle(thisCandle.Open, thisCandle.Close);
                bool lastCandleGreen = isGreenCandle(lastCandle.Open, lastCandle.Close);

                if (thisCandleGreen == true)
                {
                    TrendSignal = TrendSignal + 1;
                }
                else if (thisCandleGreen == false)
                {
                    TrendSignal = TrendSignal - 1;
                }                    

            } catch (Exception e)
            {
                Print("Could update direction, due to {0}" + e.StackTrace);
            }

            return;
        }

        private bool IsEntryPossible(int index)
        {
            // Print("Hello im {0} and result is {1} and is {2}", index, ARN.Up[index], ARN.Down[index]);

            if(index == 0)
            {
                return false;
            }

            try
            {

                bool greenCandle = isGreenCandle(bar.Open, bar.Close);
               
                if (greenCandle == true && (bar.Close < MA1.Result[index] || bar.Close < MA2.Result[index]))
                {
                    return false;
                }

                if (greenCandle == false && (bar.Close > MA1.Result[index] || bar.Close > MA2.Result[index]))
                {
                    return false;
                }

                if ((DMS.ADX[index] < ADXLevel) || (UseADXDown == true && (DMS.ADX[index - 1] > DMS.ADX[index])))
                {
                    return false;
                }

                if (UseMajorTrendMA == true)
                {
                    if (greenCandle == true && (bar.Close < MA3.Result[index]))
                    {
                        return false;
                    }
                    else if (greenCandle == false && (bar.Close > MA3.Result[index]))
                    {
                        return false;
                    }

                }

                if (MATrendDirection == true)
                {
                    if( greenCandle == true && ((MA1.Result[index] < MA1.Result[(index-1)]) || (MA2.Result[index] < MA2.Result[(index-1)]) || (MA3.Result[index] < MA3.Result[(index-1)]) ))
                    {
                        return false;
                    }
                    else if( greenCandle == false && ((MA1.Result[index] > MA1.Result[(index-1)]) || (MA2.Result[index] > MA2.Result[(index-1)]) || (MA3.Result[index] > MA3.Result[(index-1)]) ))
                    {        
                        return false;
                    }
                }

                if (MinorMATrendDirection == true)
                {
                    if( greenCandle == true && ((MA1.Result[index] < MA1.Result[(index-1)]) || (MA2.Result[index] < MA2.Result[(index-1)]) ))
                    {
                        return false;
                    }
                    else if( greenCandle == false && ((MA1.Result[index] > MA1.Result[(index-1)]) || (MA2.Result[index] > MA2.Result[(index-1)]) ))
                    {        
                        return false;
                    }
                }
                
                return true;

            } catch (Exception e)
            {
                Print("Could not check entry, due to {0}" + e.StackTrace);
                return false;
            }
        }

    }
}