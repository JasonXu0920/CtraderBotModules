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
    public class SignalProvider : Robot
    {
        [Parameter(DefaultValue = true)]
        public bool Start { get; set; }
 
        [Parameter(DefaultValue = 1, MaxValue = 3, MinValue = 1)]
        public int Slot { get; set; }
 
        public int port = 8888;
        public static string server = "localhost";
        public static string responseData = "";
        private string openPositionsString = "";
        private string closeHistoryPosition = "";
 
 
        protected override void OnStart()
        {
            Print("Trader Play ...");
        }
 
        protected override void OnBar()
        {
            if (!Start)
            {
                Stop();
            }
 
            Connect(sendPositions());
        }
 
 
 
//====================================================================================================================
//                                                                                         Socket send
//====================================================================================================================
 
        public void Connect(string PosAll = "")
        {
            ///===========================================================
 
            string pos = "SET#" + Slot + "#";
            //pos = pos + "SPREAD:" + Symbol.Spread + "|";
            pos = pos + PosAll;
            // end of stream
            pos = pos + '\0';
 
            // Process the data sent by the client.
            Print("String " + pos);
            ////===========================================================
 
 
            var name = "Copy";
            var text = "Copyrights breakermind.com";
            var staticPos = StaticPosition.TopRight;
            var color = Colors.YellowGreen;
            ChartObjects.DrawText(name, text, staticPos, color);
 
 
 
            try
            {
                // Create a TcpClient to send and recive message from server.
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
 
        }
 
 
 
//====================================================================================================================
//                                                                                          Get Positions
//====================================================================================================================
        protected string sendPositions()
        {
 
            // open position
            openPositionsString = "[GO]";
 
            foreach (var position in Positions)
            {
 
                // BUY positions
                if (position.TradeType == TradeType.Buy)
                {
                    openPositionsString += position.Id + ";" + position.SymbolCode + ";" + "BUY" + ";" + position.Volume + ";" + position.EntryPrice + ";" + position.StopLoss + ";" + position.TakeProfit + ";" + position.EntryTime + "|";
                }
 
                // SELL positions
                if (position.TradeType == TradeType.Sell)
                {
                    openPositionsString += position.Id + ";" + position.SymbolCode + ";" + "SELL" + ";" + position.Volume + ";" + position.EntryPrice + ";" + position.StopLoss + ";" + position.TakeProfit + ";" + position.EntryTime + "|";
                }
 
            }
            openPositionsString += "[OG]";
 
 
 
            closeHistoryPosition = "#";
            foreach (HistoricalTrade tr in History)
            {
                // this month closed positions
                if (DateTime.Now.Month == tr.EntryTime.Month)
                {
                    closeHistoryPosition += tr.PositionId + "|";
                }
            }
 
            return openPositionsString + closeHistoryPosition;
 
        }
 
 
//end
    }
}