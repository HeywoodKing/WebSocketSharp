using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Net;

namespace WebSocketSharpUtil
{
    public static class Debugger
    {
        static UdpClient client;
        static Debugger()
        {
            client = new UdpClient();
            client.Connect(new IPEndPoint(IPAddress.Loopback, 3000));

            Clear();
        }

        public static void Clear()
        {
            Console.Clear();
            SendCmd(0xfe, 0);
        }

        public static void Write(ConsoleColor color, object obj)
        {
            Write(color, obj.ToString());
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Write(ConsoleColor color, string message)
        {
            if (message == null)
                return;

            var dbgMsg = message;
            if (dbgMsg.Length > 80)
                dbgMsg = dbgMsg.Substring(0, 70) + "..." + Environment.NewLine;

            Console.ForegroundColor = color;
            Console.Write(dbgMsg);

            SendCmd(0xff, (byte)color);
            SendMsg(message);
        }

        public static void WriteLine(ConsoleColor color, object obj)
        {
            if (obj == null)
                return;

            WriteLine(color, obj.ToString());
        }

        public static void WriteLine(ConsoleColor color, string message)
        {
            Write(color, message + Environment.NewLine);
        }


        private static void SendCmd(byte cmdType, byte cmdInfo)
        {
            client.Send(new byte[] { cmdType, cmdInfo }, 2);
        }

        private static void SendMsg(string msg)
        {
            var data = Encoding.Default.GetBytes(msg);
            client.Send(data, data.Length);
        }
    }
}
