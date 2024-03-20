using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class RandomcBot : CBotBase
    {
        [Parameter("Rsi period", Group = "Rsi", DefaultValue = 14, MinValue = 10, MaxValue = 20, Step = 1)]
        public int RsiPeriod { get; set; }

        [Parameter("Atr period", Group = "ATR", DefaultValue = 14, MinValue = 1, MaxValue = 20000, Step = 1)]
        public int AtrPeriod { get; set; }

        [Parameter("TP factor", Group = "Money", DefaultValue = 2, MinValue = 1, MaxValue = 2.5)]
        public double TpFactor { get; set; }

        [Parameter("RSI PR Array", Group = "PR", DefaultValue = "")]
        public string RsiPrArray { get; set; }

        [Parameter("No Rand", Group = "PR", DefaultValue = false)]
        public bool NoRand { get; set; }

        [Parameter("SL", Group = "Money", DefaultValue = 40, MinValue = 1, MaxValue = 1000)]
        public int SLVal { get; set; }

        [Parameter("Risk", Group = "Money", DefaultValue = 1)]
        public double Risk { get; set; }


        private RelativeStrengthIndex _rsi;
        private MacdCrossOver _macdC;
        //private IndicatorPr _rsiPr;
        private IndicatorNPr _rsiMacdPr;
        //private int _rsiQVal;
        private int[] _rsiQVals;
        private double _currMinVal;
        private double _currMaxVal;

        private Dictionary<int, TradePr> _trades = new Dictionary<int, TradePr>();
        private Dictionary<int, TradeNPr> _tradesN = new Dictionary<int, TradeNPr>();

        public static RandomcBot Singleton;

        protected override void OnStart()
        {
            Singleton = this;
            Indicators.AverageTrueRange(AtrPeriod, MovingAverageType.Exponential);
            _rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, RsiPeriod);
            _macdC = Indicators.MacdCrossOver(26, 12, 9);
            Positions.Closed += Closed;

            var rsiMin = 0;
            var rsiMax = 100;
            var q = 10;
            /*if (!string.IsNullOrEmpty(RsiPrArray))
            {
                _rsiPr = IndicatorPr.Parse(rsiMin, rsiMax, diff, RsiPrArray, NoRand);
            }
            else
            {
                _rsiPr = new IndicatorPr(rsiMin, rsiMax, q, diff, NoRand);
            }*/

            var mMin = -0.001;
            var mMax = 0.001;
            var qm = 10;
            var diffm = 0.1;
            _rsiMacdPr = new IndicatorNPr(new double[] 
            {
                rsiMin,
                mMin
            }, new double[] 
            {
                rsiMax,
                mMax
            }, new int[] 
            {
                q,
                qm
            }, diffm, NoRand);


            _currMinVal = double.MaxValue;
            _currMaxVal = double.MinValue;
        }

        private string Str(int[] val)
        {
            return String.Join(",", val);
        }

        private void Closed(PositionClosedEventArgs pcea)
        {
//            var signal = _trades[pcea.Position.Id].Signal;
//            var qVal = _trades[pcea.Position.Id].QVal;

            var signal = _tradesN[pcea.Position.Id].Signal;
            var qVals = _tradesN[pcea.Position.Id].QVals;
            switch (pcea.Reason)
            {
                case PositionCloseReason.Closed:
                    break;
                case PositionCloseReason.StopLoss:
                    Print("SL " + signal + " " + Str(qVals));
                    Print(_rsiMacdPr.ToString());
                    _rsiMacdPr.UpdateMatrix(qVals, signal, -1);
                    Print(_rsiMacdPr.ToString());
                    break;
                case PositionCloseReason.TakeProfit:
                    Print("TP " + signal + " " + Str(qVals));
                    Print(_rsiMacdPr.ToString());
                    _rsiMacdPr.UpdateMatrix(qVals, signal, 1);
                    Print(_rsiMacdPr.ToString());
                    break;
                case PositionCloseReason.StopOut:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _currMinVal = double.MaxValue;
            _currMaxVal = double.MinValue;
        }

        protected override void OnBar()
        {
            try
            {
                var signalWithId = OnTrade(Risk, TpFactor);
                if (signalWithId != null)
                {
                    /*_trades[signalWithId.Item2] = new TradePr 
                    {
                        QVal = _rsiQVal,
                        Signal = signalWithId.Item1 == TradeType.Buy ? 1 : -1
                    };*/

                    _tradesN[signalWithId.Item2] = new TradeNPr 
                    {
                        QVals = _rsiQVals,
                        Signal = signalWithId.Item1 == TradeType.Buy ? 1 : -1
                    };
                }
            } catch (Exception ex)
            {
                Print(ex);
            }

            _currMinVal = Math.Min(_currMinVal, Bars.ClosePrices.LastValue);
            _currMaxVal = Math.Max(_currMaxVal, Bars.ClosePrices.LastValue);
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }

        public RandomcBot() : base("Random cBot")
        {
        }

        protected override TradeType? GetSignal()
        {
            //var signal = _rsiPr.GetCurrentSignal(_rsi.Result.LastValue, out _rsiQVal);
            var signal = _rsiMacdPr.GetCurrentSignal(new[] 
            {
                _rsi.Result.LastValue,
                _macdC.Histogram.LastValue
            }, out _rsiQVals);
            return GetTradeType(signal);
        }

        protected override double GetStopLoss()
        {
            return SLVal;
        }

        protected override TradeType Filter(TradeType tradeType, ref double stopLossInPips, ref double risk)
        {
            return tradeType;
        }
    }

    public class TradeNPr
    {
        public int[] QVals;
        public double Signal;
    }

    public class TradePr
    {
        public int QVal;
        public double Signal;
    }

    public class IndicatorNPr
    {
        private readonly double[] _min;
        private readonly double[] _max;
        private readonly int[] _q;
        private readonly double _diff;
        private readonly ProbMatrixN _probMatrix;
        private readonly Random _random;
        private readonly bool _noRand;

        public IndicatorNPr(double[] min, double[] max, int[] q, double diff, bool noRand)
        {
            _min = min;
            _max = max;
            _q = q;
            _diff = diff;
            _probMatrix = new ProbMatrixN(q);
            _noRand = noRand;

            _random = new Random();
        }

        public double GetCurrentSignal(double[] indVal, out int[] qVal)
        {
            qVal = Quantize(indVal);

            return _probMatrix.Select(_noRand ? 0.5 : _random.NextDouble(), qVal);
        }

        public void UpdateMatrix(int[] qval, double signal, double result)
        {
            double diff = _diff;
            if (result * signal < 0)
                diff = -diff;
            _probMatrix.Update(diff, qval);
        }

        private int[] Quantize(double[] vals)
        {
            List<int> results = new List<int>();
            for (int i = 0; i < vals.Length; i++)
            {
                var val = vals[i];
                if (val < _min[i])
                    val = _min[i];
                else if (val >= _max[i])
                    val = _max[i];
                var qval = (int)Math.Floor((val - _min[i]) / (_max[i] - _min[i]) * _q[i]);
                if (qval >= _q[i])
                    qval = _q[i] - 1;

                results.Add(qval);
            }

            return results.ToArray();
        }

        public override string ToString()
        {
            return _probMatrix.ToString();
        }
    }

    public class ProbMatrixN
    {
        private readonly double[] _probs;
        private readonly int[] _qs;

        public ProbMatrixN(int[] qs)
        {
            _qs = qs;
            var size = qs.Aggregate(1, (p, x) => p * x);
            _probs = Enumerable.Repeat(0.5, size).ToArray();
        }

        public ProbMatrixN(double[] probs, int[] qs)
        {
            _probs = probs;
            _qs = qs;
        }

        public double Select(double rand, int[] vals)
        {
            var ind = GetInd(vals);
            if (ind >= _probs.Length)
                ind = _probs.Length - 1;
            if (ind < 0)
                ind = 0;
            return rand < _probs[ind] ? 1 : -1;

            //10, 30, 20
            //5,6,7
            //5+6*10+7*10*30
        }

        public void Update(double diff, int[] vals)
        {
            var ind = GetInd(vals);
            _probs[ind] += diff;
            if (_probs[ind] > 1)
                _probs[ind] = 1;
            else if (_probs[ind] < 0)
                _probs[ind] = 0;
        }

        public override string ToString()
        {
            return string.Join("|", _probs.Select(x => x.ToString("0.0000")));
        }

        private int GetInd(int[] vals)
        {
            int ind = 0;
            int mul = 1;
            for (int i = 0; i < _qs.Length; i++)
            {
                ind += vals[i] * mul;
                mul = mul * _qs[i];
            }

            return ind;
        }
    }

    public class IndicatorPr
    {
        private readonly double _min;
        private readonly double _max;
        private readonly int _q;
        private readonly double _diff;
        private readonly ProbMatrix _probMatrix;
        private readonly Random _random;
        private readonly bool _noRand;

        public IndicatorPr(double min, double max, int q, double diff, bool noRand) : this(min, max, q, diff, Enumerable.Repeat(0.5, q).ToArray(), noRand)
        {
        }

        public IndicatorPr(double min, double max, int q, double diff, double[] matrix, bool noRand)
        {
            _min = min;
            _max = max;
            _q = q;
            _diff = diff;
            _probMatrix = new ProbMatrix(matrix);
            _noRand = noRand;

            _random = new Random();
        }

        public double GetCurrentSignal(double indVal, out int qVal)
        {
            qVal = Quantize(indVal);
            return _probMatrix.Select(_noRand ? 0.5 : _random.NextDouble(), qVal);
        }

        public void UpdateMatrix(int qval, double signal, double result)
        {
            double diff = _diff;
            if (result * signal < 0)
                diff = -diff;
            _probMatrix.Update(diff, qval);
        }

        private int Quantize(double val)
        {
            if (val < _min)
                val = _min;
            else if (val >= _max)
                val = _max - 1;
            return (int)Math.Floor((val - _min) / (_max - _min) * _q);
        }

        public override string ToString()
        {
            return _probMatrix.ToString();
        }

        public static IndicatorPr Parse(double min, double max, double diff, string input, bool noRand)
        {
            var vals = input.Split('|').Select(double.Parse).ToArray();
            return new IndicatorPr(min, max, vals.Length, diff, vals, noRand);
        }
    }

    public class ProbMatrix
    {
        private readonly double[] _probs;

        public ProbMatrix(double[] probs)
        {
            _probs = probs;
        }

        public double Select(double rand, int val)
        {
            return rand < _probs[val] ? 1 : -1;
        }

        public void Update(double diff, int val)
        {
            _probs[val] += diff;
            if (_probs[val] > 1)
                _probs[val] = 1;
            else if (_probs[val] < 0)
                _probs[val] = 0;
        }

        public override string ToString()
        {
            return string.Join("|", _probs.Select(x => x.ToString("0.0000")));
        }
    }
}
