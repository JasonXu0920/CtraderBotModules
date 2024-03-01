// System
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Sockets;
// cAlgo
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.API.Requests;
using cAlgo.Indicators;
 
 
namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class Client : Robot
    {
 
 
        [Parameter(DefaultValue = true)]
        public bool Start { get; set; }
 
        [Parameter(DefaultValue = 1, MaxValue = 3, MinValue = 1)]
        public int Slot { get; set; }
 
        public int port = 8888;
        public static string server = "localhost";
 
        public static string responseData = "";
        private string responseFromServer = "";
 
        private int Leverage = 100;
 
        [Parameter("Login", DefaultValue = "")]
        public string login { get; set; }
 
        [Parameter("Password", DefaultValue = "")]
        public string pass { get; set; }
 
        [Parameter("VolumeMax", DefaultValue = 50000)]
        public int VolumeMax { get; set; }
 
        [Parameter("Auto Stop Loss (pips)", DefaultValue = 0)]
        public int StopLoss { get; set; }
 
        [Parameter("Auto Take Profit (pips) ", DefaultValue = 0)]
        public int TakeProfit { get; set; }
 
        [Parameter(DefaultValue = "C:/Windows/Media/tada.wav")]
        public string SoundFile { get; set; }
 
        List<string> PosOpenID = new List<string>();
        List<string> PosCloseID = new List<string>();
        List<string> PosServerID = new List<string>();
        List<string> PosServerAll = new List<string>();
 
 
        protected override void OnStart()
        {
            Print("!!!! Breakermin Slave Copy robot start !!!! Min Account leverage 1:100");
 
            if (Account.Leverage < Leverage)
            {
                Print("Account leverage incorrect !!! You need leverage >= 1:100, stoping... copier");
                Stop();
 
            }
        }
 
        protected override void OnBar()
        {
            Notifications.PlaySound(SoundFile);
        }
 
        protected override void OnTick()
        {
            initializePositions();
            getPositions();
            comparePositions();
            initializePositions();
            clPostions();
        }
 
        protected override void OnStop()
        {
            Print("Breakermind Slave Copy stop.");
        }
 
//====================================================================================================================
//                                                                                                Compare    Positions
//====================================================================================================================
        protected void clPostionsAll()
        {
 
            var positions = Positions;
 
            foreach (var position in positions)
            {
                ClosePosition(position);
            }
 
        }
 
//====================================================================================================================
//                                                                                                Compare    Positions
//====================================================================================================================
        protected void clPostions()
        {
 
            var positions = Positions;
 
            foreach (var position in positions)
            {
                if (!PosServerID.Contains(position.Comment) && position.Comment != "")
                {
                    ClosePosition(position);
                }
            }
 
        }
//====================================================================================================================
//                                                                                                Compare    Positions
//====================================================================================================================
        protected void comparePositions()
        {
 
            try
            {
 
                string inp = "" + responseFromServer;
                //string goog = responseFromServer.Substring(0, 8);
                string goog = responseFromServer;
                //Print(goog);
 
                inp = goog.Substring(4);
                string[] pp = inp.Split('#');
 
                //Print(pp[0]);
                goog = pp[0];
 
                if (goog != "TXT|")
                {
                    // cut [GO] and |[OG]
                    inp = inp.Substring(4);
                    inp = inp.Substring(0, inp.Length - 6);
                    Print(inp);
                    // pociapaÄ na pozycjÄ lista
                    PosServerID.Clear();
                    string[] posin = inp.Split('|');
                    PosServerAll = new List<string>(posin);
 
                    foreach (string pos in posin)
                    {
                        //Print(pos);
                        string[] p = pos.Split(';');
                        PosServerID.Add(p[0]);
                        if (!PosOpenID.Contains(p[0]) && p[0] != "" && !PosCloseID.Contains(p[0]))
                        {
 
 
 
 
                            ///// co tu nie tak
 
                            Symbol sym = MarketData.GetSymbol(p[1]);
 
                            if (p[2] == "BUY")
                            {
                                //Print("BUY " + p[1]);
                                if (p[5] == null || p[5] == "")
                                    p[5] = "0";
 
                                if (p[6] == null || p[6] == "")
                                    p[6] = "0";
 
 
 
                                double sl1 = 0;
                                double tp1 = 0;
 
                                if (p[5] != "0")
                                {
                                    double pips1 = Convert.ToDouble(p[4]) - Convert.ToDouble(p[5]);
                                    sl1 = pips1 / sym.PipSize;
                                }
 
 
                                if (p[6] != "0")
                                {
                                    double pips2 = Convert.ToDouble(p[6]) - Convert.ToDouble(p[4]);
                                    tp1 = pips2 / sym.PipSize;
                                }
 
                                ExecuteMarketOrder(TradeType.Buy, sym, Convert.ToInt64(Convert.ToDecimal(p[3])), "", sl1, tp1, 1, p[0]);
                                //ExecuteMarketOrder(TradeType.Buy, Symbol, Convert.ToInt64(Convert.ToDecimal(p[4])), "Slave", Convert.ToDouble(p[6]), Convert.ToDouble(p[7]));
                            }
 
                            if (p[2] == "SELL")
                            {
 
                                if (p[5] == null)
                                    p[5] = "0";
 
                                if (p[4] == null)
                                    p[4] = "0";
 
 
                                if (p[5] == null)
                                    p[5] = "0";
 
 
                                double sl = 0;
                                double tp = 0;
 
                                if (p[5] != "0")
                                {
                                    double pips0 = Convert.ToDouble(p[5]) - Convert.ToDouble(p[4]);
                                    sl = pips0 / sym.PipSize;
                                }
 
                                if (p[6] != "0")
                                {
                                    double pips01 = Convert.ToDouble(p[4]) - Convert.ToDouble(p[6]);
                                    tp = pips01 / sym.PipSize;
                                }
 
                                ExecuteMarketOrder(TradeType.Sell, sym, Convert.ToInt64(Convert.ToDecimal(p[3])), "", sl, tp, 1, p[0]);
                                //ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, MyLabel, StopLoss, TakeProfit);
                            }
                        }
                        initializePositions();
                    }
                }
 
                // close all position if empty
                if (goog == "[GO][OG]")
                {
                    clPostionsAll();
                }
            } catch (Exception rrr)
            {
                Print(rrr);
            }
        }
 
//====================================================================================================================
//                                                                                                Initialize Positions
//====================================================================================================================
        protected void initializePositions()
        {
            // open id
            PosOpenID.Clear();
            var AllPositions = Positions;
            foreach (var position in AllPositions)
            {
                if (position.Comment != "")
                {
                    PosOpenID.Add(position.Comment);
                }
            }
 
            // colose id
            PosCloseID.Clear();
            foreach (HistoricalTrade trade in History)
            {
                if (trade.Comment != "")
                {
                    PosCloseID.Add(trade.Comment);
                }
 
            }
        }
 
 
//================================================================================================================
//                                                                                   End Send POST to HTTPS Server
//================================================================================================================
 
        public void getPositions()
        {
            ///===========================================================
 
            string pos = "GET#" + Slot + "#";
            // end of stream
            pos = pos + '\0';
 
            try
            {
                // Create a TcpClient to send and recive message from server.
                Int32 port = 8888;
                TcpClient client = new TcpClient(server, port);
 
                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(pos);
 
                // Get a client stream for reading and writing.
                NetworkStream stream = client.GetStream();
 
 
                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);
                Print("Sent: " + pos);
 
 
                // Buffer to store the response bytes.
                data = new Byte[1024];
 
                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Print("Received: {0}", responseData);
 
                // Process the data sent by the client.
                var name = "Up";
                var text = "Copyrights breakermind.com";
                var staticPos = StaticPosition.TopRight;
                var color = Colors.Yellow;
                ChartObjects.DrawText(name, text, staticPos, color);
 
                // Close everything.
                stream.Close();
                client.Close();
 
            } catch (ArgumentNullException e)
            {
                Print("ArgumentNullException: {0}", e);
            } catch (SocketException e)
            {
                Print("SocketException: {0}", e);
            }
            responseFromServer = responseData;
        }
 
 
 
    }
}