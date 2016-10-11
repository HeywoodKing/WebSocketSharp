using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace WebSocketSharpUtil
{
    /// <summary>
    /// 客户端信息
    /// author:chaix
    /// date:2016-09-12
    /// </summary>
    public class ClientInfo
    {
        public byte[] Buffer;
        public string Nickname { get; set; }
        public EndPoint Id { get; set; }
        public IntPtr Handle { get; set; }
        public string Name
        {
            get 
            {
                if (!string.IsNullOrEmpty(Nickname))
                    return Nickname;
                else
                    return string.Format("{0}#{1}", Id, Handle);
            }
        }
        public bool IsHandShaked { get; set; }
    }
}
