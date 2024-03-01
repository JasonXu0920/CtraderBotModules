using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
  
namespace server
{
    public class server
    {
 
        private string positions = "";
        // Declare a two dimensional array 
        // users allowed 10
        private static int maxSlots = 11;
        private string[,] posArray = new string[maxSlots+1, 1];
 
        ManualResetEvent tcpClientConnected = new ManualResetEvent(false);
  
        void ProcessIncomingData(object obj)
        {
            TcpClient client = (TcpClient)obj;
            string ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
 
            Console.WriteLine("Client IP : " + ip);
             
            string sb = "";
            using (NetworkStream stream = client.GetStream())
            {
                  
                    int i;
                    try
                    {
                        while ((i = stream.ReadByte()) != '\0')
                        {
                            sb = sb + (char)i;
                            //Console.WriteLine(sb.ToString());
                        }
 
                        using (System.IO.StreamWriter w = File.AppendText("log.txt"))
                        {
                            w.WriteLine(DateTime.Now.ToLongTimeString() + " IP: " + ip + " TXT: " + sb.ToString());
                        }
 
                        string[] split = Regex.Split(sb, "#");
                        string reply = "ACCESSDENIED" + '\0';
 
                        if (split[0].ToString() == "SET")
                        {
                            int nr = Convert.ToInt32(split[1]);
                            if(nr < 1 && nr >  maxSlots){
                            nr = 0;
                            }
 
                            // POSITIONS
                            posArray[nr, 0] = split[2].ToString();
                             
                            //string reply = "ack: " + sb.ToString() + '\0';
                            reply = "ack: " + split[0].ToString() + " " + split[2].ToString() + '\0';
                            stream.Write(Encoding.ASCII.GetBytes(reply), 0, reply.Length);
                        }
 
                        if (split[0].ToString() == "GET")
                        {
 
                            int gnr = Convert.ToInt32(split[1]);
                            if (gnr < 1 && gnr > maxSlots)
                            {
                                gnr = 0;
                            }
                             
                            positions = posArray[gnr, 0];
                            Console.WriteLine("TXT|" + positions);
                            reply = "TXT|" + positions.ToString() + '\0';
                            stream.Write(Encoding.ASCII.GetBytes(reply), 0, reply.Length);
                        }
 
                        if (split[0].ToString() == "PI")
                        {
                            reply = "PI" + '\0';
                            stream.Write(Encoding.ASCII.GetBytes(reply), 0, reply.Length);
                        }
 
                        if (split[0].ToString() != "PI" && split[0].ToString() != "GET" && split[0].ToString() != "SET")
                        {
                            stream.Write(Encoding.ASCII.GetBytes(reply), 0, reply.Length);
                        }
 
                    }
                    catch (Exception e) { }
 
            }
            Console.WriteLine(sb.ToString());
 
        }
  
        void ProcessIncomingConnection(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(ar);
            
 
            ThreadPool.QueueUserWorkItem(ProcessIncomingData, client);
            tcpClientConnected.Set();
        }
  
        public void start()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            TcpListener listener = new TcpListener(endpoint);
            listener.Start();
             
            while (true)
            {
                tcpClientConnected.Reset();
                listener.BeginAcceptTcpClient(new AsyncCallback(ProcessIncomingConnection), listener);
                tcpClientConnected.WaitOne();
            }
        }
    }
  
    class Program
    {
        static void Main(string[] args)
        {
 
            Console.WriteLine("Multi user server. Recive save and send data to clients max 10 accounts.");
            //DateTime.Now.ToLongTimeString()
            Console.WriteLine(DateTime.Now + " Waiting for connections....");
            try{
            server s = new server();
            s.start();
            }catch(Exception e){}
        }
    }
}