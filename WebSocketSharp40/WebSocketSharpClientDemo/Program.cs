using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharpClient;
using WebSocket4Net;

namespace WebSocketSharpClientDemo
{
    class Program
    {
        private static WebSocket4Net.WebSocket ws = null;
        static void Main(string[] args)
        {
            //SocketClient client = new SocketClient();
            //client.Run("192.168.10.253", 8900);
            //Console.ReadKey(false);
            Start();
        }

        private static void Start()
        {
            try
            {
                ws = new WebSocket("ws://192.168.10.253:8900/");
                ws.Opened += new EventHandler(ws_Opened);
                ws.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(ws_Error);
                ws.Closed += new EventHandler(ws_Closed);
                ws.MessageReceived += new EventHandler<MessageReceivedEventArgs>(ws_MessageReceived);
                ws.Open();

                Console.ReadKey(false);
            }
            catch (Exception ex)
            { }
        }

        static void ws_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //throw new NotImplementedException();
            Console.WriteLine("ws_MessageReceived");
        }

        static void ws_DataReceived(object sender, DataReceivedEventArgs e)
        {
            //throw new NotImplementedException();
            Console.WriteLine("ws_DataReceived");
        }

        static void ws_Closed(object sender, EventArgs e)
        {
            try
            {
                //throw new NotImplementedException();
                Console.WriteLine("ws_Closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void ws_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            //throw new NotImplementedException();
            Console.WriteLine("ws_Error");
        }

        static void ws_Opened(object sender, EventArgs e)
        {
            if (ws != null)
                ws.Send("hello");
        }
    }
}
