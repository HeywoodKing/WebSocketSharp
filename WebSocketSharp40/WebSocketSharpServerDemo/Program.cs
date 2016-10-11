using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketSharpDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            WebSocketSharp.Tcp tcp = new WebSocketSharp.Tcp();
            tcp.Run(8900, false);
            Console.ReadKey(false);
        }
    }
}
