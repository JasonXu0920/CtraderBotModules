using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;


namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Cyf_TheSqueeze : Indicator
    {
        
        //private string Bullet = "●●";
        private string Squeeze = "●";
        private string UP = "▲";
        private string Down = "▼";
        private const cAlgo.API.VerticalAlignment vAlign = cAlgo.API.VerticalAlignment.Center;
        private const cAlgo.API.HorizontalAlignment hAlign = cAlgo.API.HorizontalAlignment.Center;
        private bool Squeeze_State;
        //**********************

       
        private MacdHistogram MacD;
        /// Keltner channel Stuff ///

        /// Momentum Stuff ///////////
        private MomentumOscillator _momentum;
        private LinearRegressionSlope LR_Slope;

        // [Parameter(DefaultValue = false)]
        //public bool Show_Trend { get; set; }

        [Parameter(DefaultValue = true)]
        public bool LR_Based { get; set; }

        [Parameter(DefaultValue = 12)]
        public int Periods { get; set; }

        [Parameter(DefaultValue = 10)]
        public int Klt_ATR_Periods { get; set; }

        [Parameter(DefaultValue = 20)]
        public int Klt_Periods { get; set; }

        [Parameter(DefaultValue = 1.5)]
        public double Klt_stdDev { get; set; }

        [Parameter(DefaultValue = 20)]
        public int bb_Periods { get; set; }

        [Parameter(DefaultValue = 2.0)]
        public double bb_stdDev { get; set; }
        ////////////////////////////////////////////////////
        [Parameter("LongCycle", DefaultValue = 26)]
        public int LngCycle { get; set; }

        [Parameter("ShortCycle", DefaultValue = 12)]
        public int ShrtCycle { get; set; }

        [Parameter("SigPeriod", DefaultValue = 9)]
        public int SigPeriod { get; set; }

        [Parameter("MCD_Factor", DefaultValue = 0.5, MinValue = 0.01, Step = 0.01)]
        public double Mac_factor { get; set; }
        ///////////////////////////////////
        [Output("Mac_D", Color = Colors.LimeGreen, PlotType = PlotType.DiscontinuousLine, Thickness = 1)]
        public IndicatorDataSeries Mac_D { get; set; }
        //////////////////////////////////////////////////////

        [Output("Momentum_Down", Color = Colors.Red, PlotType = PlotType.Histogram, Thickness = 3)]
        public IndicatorDataSeries moScillatorDown { get; set; }

        [Output("Momentum_Up", Color = Colors.Blue, PlotType = PlotType.Histogram, Thickness = 3)]
        public IndicatorDataSeries moScillatorUp { get; set; }

        [Output("Momentum_Down2", Color = Colors.LightPink, PlotType = PlotType.Histogram, Thickness = 3)]
        public IndicatorDataSeries moScillatorDown2 { get; set; }

        [Output("Momentum_Up2", Color = Colors.CornflowerBlue, PlotType = PlotType.Histogram, Thickness = 3)]
        public IndicatorDataSeries moScillatorUp2 { get; set; }

        /////////////////////////////////

        private KeltnerChannels Klt;
        private BollingerBands bb;
        

        /// Bollinger Band Stuff ///


        [Parameter("Source")]
        public DataSeries bb_Source { get; set; }


       

       protected override void Initialize()
        {
            
            MacD = Indicators.MacdHistogram(LngCycle, ShrtCycle, SigPeriod);
            // Initialize keltner Channels 
            _momentum = Indicators.MomentumOscillator(MarketSeries.Close, Periods);
            LR_Slope = Indicators.LinearRegressionSlope(MarketSeries.Close, Periods);
            Klt = Indicators.KeltnerChannels(Klt_Periods, MovingAverageType.Exponential, Klt_ATR_Periods, MovingAverageType.Simple, Klt_stdDev);
            bb = Indicators.BollingerBands(bb_Source, bb_Periods, bb_stdDev, MovingAverageType.Simple);

            
        }

        public override void Calculate(int index)
        {
           
            Mac_D[index] = MacD.Histogram[index] * Mac_factor;

            if (LR_Based == false)
            {
                double momentum = _momentum.Result[index];
                RefreshData();

                if (_momentum.Result[index] >= 100.0)
                {
                    if (_momentum.Result.IsRising())
                    {
                        moScillatorUp[index] = (_momentum.Result[index] - 100.0);

                    }
                    else
                    {
                        moScillatorUp2[index] = (_momentum.Result[index] - 100.0);
                    }
                    moScillatorUp[index] = (_momentum.Result[index] - 100.0);
                    ////////////////////////////////////////////////////////////////////////
                                 
ChartObjects.RemoveObject("Direction");
                    ChartObjects.DrawText("Direction", UP, StaticPosition.BottomRight, Colors.Green);


                }
                else
                {
                    if (_momentum.Result.IsFalling())
                    {
                        moScillatorDown[index] = (_momentum.Result[index] - 100.0);
                    }
                    else
                    {
                        moScillatorDown2[index] = (_momentum.Result[index] - 100.0);
                    }

                    moScillatorDown[index] = (_momentum.Result[index] - 100.0);
                    ////////////////////////////////////////////////////////////////////////
                         
ChartObjects.RemoveObject("Direction");
                    ChartObjects.DrawText("Direction", Down, StaticPosition.BottomRight, Colors.Red);


                }
            }
            /// the Indicator is Linear Regression Based
            else
            {

                if (LR_Slope.Result[index] >= 0.0)
                {
                    if (LR_Slope.Result.IsRising())
                    {
                        moScillatorUp[index] = LR_Slope.Result[index];
                    }
                    else
                    {
                        moScillatorUp2[index] = LR_Slope.Result[index];
                    }
                    ChartObjects.RemoveObject("Direction");
                    ChartObjects.DrawText("Direction", UP, StaticPosition.BottomRight, Colors.Green);

                }
                else
                {
                    if (LR_Slope.Result.IsFalling())
                    {
                        moScillatorDown[index] = (LR_Slope.Result[index]);
                    }

                    else
                    {
                        moScillatorDown2[index] = (LR_Slope.Result[index]);
                    }

                    ChartObjects.RemoveObject("Direction");
                    ChartObjects.DrawText("Direction", Down, StaticPosition.BottomRight, Colors.Red);


                }
            }


            if (bb.Top[index] < Klt.Top[index] && bb.Bottom[index] > Klt.Bottom[index])
            {
                Squeeze_State = true;

                ChartObjects.DrawText("Direction1" + index, Squeeze, index, 0, vAlign, hAlign, Colors.Red);
            }
            else if (Squeeze_State)
            {
                ChartObjects.DrawText("Direction1" + index, Squeeze, index, 0, vAlign, hAlign, Colors.Green);
                
                Squeeze_State = false;
                Notifications.PlaySound("C:\\Sounds\\Bike Horn.wav");

            }
        }


        protected override void OnTimer()
        {
            
            ChartObjects.RemoveObject("Inside");
        }
    }

}