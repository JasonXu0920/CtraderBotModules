using System;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class EMAcrossover : Robot
    {
        [Parameter("Telegram Bot Key", DefaultValue = "__bot_key__")]
        public string BOT_API_KEY { get; set; }

        [Parameter("ChannelId", DefaultValue = "")]
        public string ChannelId { get; set; }

        List<string> _telegramChannels = new List<string>();



        private ExponentialMovingAverage slowMa;
        private ExponentialMovingAverage fastMa;

        [Parameter("Source")]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow Periods", DefaultValue = 50)]
        public int SlowPeriods { get; set; }

        [Parameter("Fast Periods", DefaultValue = 9)]
        public int FastPeriods { get; set; }

        public bool down = false;

        public bool up = false;

        protected override void OnStart()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            fastMa = Indicators.ExponentialMovingAverage(SourceSeries, FastPeriods);
            slowMa = Indicators.ExponentialMovingAverage(SourceSeries, SlowPeriods);

            _telegramChannels.Add(ChannelId);
            SendMessageToAllChannels("Bot online: " + Server.TimeInUtc);
        }

        protected override void OnTick()
        {
            // Put your core logic here

        }

        protected override void OnBar()
        {
            if (slowMa.Result.HasCrossedAbove(fastMa.Result, 0) || slowMa.Result.HasCrossedBelow(fastMa.Result, 0))
            {
                down = true;
                up = true;
            }
            if (slowMa.Result.Last(0) > fastMa.Result.Last(0) && down)
            {
                SendMessageToAllChannels(Chart.SymbolName + " " + Chart.TimeFrame + " " + "EMA Crossover");
                Chart.DrawIcon("crossover" + Server.TimeInUtc, ChartIconType.DownArrow, Server.Time, Bars.LastBar.High, Color.Red);
                down = false;
                up = true;
            }

            if (slowMa.Result.Last(0) < fastMa.Result.Last(0) && up)
            {
                SendMessageToAllChannels(Chart.SymbolName + " " + Chart.TimeFrame + " " + "EMA Crossover");
                Chart.DrawIcon("crossover" + Server.TimeInUtc, ChartIconType.UpArrow, Server.Time, Bars.LastBar.Low, Color.Green);
                up = false;
                down = true;
            }
        }

        protected override void OnStop()
        {
            SendMessageToAllChannels("Bot offline: " + Server.TimeInUtc);
        }

        private void SendMessageToAllChannels(string message)
        {
            foreach (var c in _telegramChannels)
            {
                SendMessageToChannel(c, message);
            }
        }

        private string SendMessageToChannel(string chat_id, string message)
        {
            var values = new Dictionary<string, string> 
            {
                {
                    "chat_id",
                    chat_id
                },
                {
                    "text",
                    message
                }
            };

            return MakeTelegramRequest(BOT_API_KEY, "sendMessage", values);
        }

        private string MakeTelegramRequest(string api_key, string method, Dictionary<string, string> values)
        {
            string TELEGRAM_CALL_URI = string.Format("https://api.telegram.org/bot{0}/{1}", api_key, method);

            var request = WebRequest.Create(TELEGRAM_CALL_URI);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";

            StringBuilder data = new StringBuilder();
            foreach (var d in values)
            {
                data.Append(string.Format("{0}={1}&", d.Key, d.Value));
            }
            byte[] byteArray = Encoding.UTF8.GetBytes(data.ToString());
            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();

            dataStream.Write(byteArray, 0, byteArray.Length);

            dataStream.Close();

            WebResponse response = request.GetResponse();

            Print("DEBUG {0}", ((HttpWebResponse)response).StatusDescription);

            dataStream = response.GetResponseStream();

            StreamReader reader = new StreamReader(dataStream);

            string outStr = reader.ReadToEnd();

            Print("DEBUG {0}", outStr);

            reader.Close();

            return outStr;


        }
    }
}