using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using WebSocketSharpUtil;
using System.Threading;
using System.Net;
using System.Text.RegularExpressions;

namespace WebSocketSharpClient
{
    /// <summary>
    /// Client Socket(WebSocket)连接，Tcp请求方式
    /// author:chaix
    /// date:2016-09-14
    /// </summary>
    public class SocketClient
    {
        private Dictionary<Socket, ClientInfo> _clientPool = new Dictionary<Socket, ClientInfo>();
        private List<SocketMessage> _msgPool = new List<SocketMessage>();
        private bool _isClear = true;
        private object _lockReceiveObj = new object();

        #region 属性
        #endregion

        #region 公共方法
        /// <summary>
        /// 启动服务器，监听客户端请求
        /// </summary>
        /// <param name="port"></param>
        /// <param name="isBroadcase">是否广播消息</param>
        public void Run(string ip, int port)
        {
            Thread clientSocketThread = new Thread(() =>
            {
                try
                {
                    IPAddress ipAddress = IPAddress.Parse(ip);
                    IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);
                    Connect(ipEndPoint);
                }
                catch (Exception ex)
                {
                    //启动客户端Run异常
                    Console.WriteLine("Client Run Error");
                }
            });

            clientSocketThread.Start();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="ipEndPoint"></param>
        /// <returns></returns>
        private void Connect(IPEndPoint ipEndPoint)
        {
            try
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(ipEndPoint);
                Console.WriteLine(string.Format("Client {0} connecting", client.RemoteEndPoint));

                byte[] buffer = new byte[1024];
                client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Receive), client);
                ClientInfo info = new ClientInfo();
                info.Id = client.RemoteEndPoint;
                info.Handle = client.Handle;
                info.Buffer = buffer;
                //把客户端存入clientPool
                _clientPool.Add(client, info);

                //发送一个握手数据包
                SendHandShakeDataPackage(client);

                //return true;
            }
            catch (Exception ex)
            {
                //return false;
            }
        }

        /// <summary>
        /// 第一次发送握手数据包
        /// </summary>
        private void SendHandShakeDataPackage(Socket client)
        {
            //发送握手数据包
            try
            {
                byte[] buffer = GetHandShakeDataPackage();
                if (client.Poll(20, SelectMode.SelectWrite))
                {
                    client.Send(buffer, buffer.Length, SocketFlags.None);
                    Console.WriteLine("发送握手数据包给服务器...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendHandShakeDataPackage Error：" + ex.Message);
            }
        }

        /// <summary>
        /// 接收服务端回发的消息，接收成功后加入到_msgPool，等待发送给指定对象
        /// </summary>
        /// <param name="result"></param>
        private void Receive(IAsyncResult result)
        {
            Monitor.Enter(_lockReceiveObj);
            Socket client = result.AsyncState as Socket;
            if (result == null || !_clientPool.ContainsKey(client))
                return;

            try
            {
                int length = client.EndReceive(result);

                byte[] buffer = _clientPool[client].Buffer;

                //接收消息
                client.BeginReceive(buffer, 0, length, SocketFlags.None, new AsyncCallback(Receive), client);
                if (length <= 0)
                    return;

                string msg = string.Empty;
                //判断是否已经握过手
                if (!_clientPool[client].IsHandShaked)
                {
                    msg = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                    if (msg.Contains("Sec-WebSocket-Key"))
                    {
                        //发送握手数据包
                        //client.Send(GetHandShakeDataPackage(buffer, length));
                        _clientPool[client].IsHandShaked = true;

                        Console.WriteLine(string.Format("Client {0} connected", client.RemoteEndPoint));
                    }
                }
                else
                {
                    //解析接收到客户端的数据
                    msg = ParseClientData(buffer, length);

                    SocketMessage sm = new SocketMessage();
                    sm.Client = _clientPool[client];
                    sm.Time = DateTime.Now;

                    string pattern = @"{<(.*?)>}";
                    Regex reg = new Regex(pattern);
                    Match match = reg.Match(msg);
                    if (!string.IsNullOrEmpty(match.Value))
                    {
                        //处理客户端传来的用户名
                        _clientPool[client].Nickname = Regex.Replace(match.Value, pattern, "$1");
                        sm.IsLoginMessage = true;
                        sm.Message = "login";
                        Console.WriteLine("{0} login @ {1}", client.RemoteEndPoint, DateTime.Now);

                        _msgPool.Add(sm);
                        _isClear = false;
                    }
                    else
                    {
                        //处理客户端传来的普通消息
                        sm.IsLoginMessage = false;
                        sm.Message = msg;
                        Console.WriteLine("客户端消息：{0} @ {1}\r\n消息：{2}", client.RemoteEndPoint, DateTime.Now, sm.Message);

                        //给指定对象发送消息
                        //Emit(client, sm);
                    }
                }
            }
            catch (Exception ex)
            {
                //将客户端标记为关闭，并在clientPool中清除
                client.Disconnect(true);
                Console.WriteLine("Client {0} disconnect", _clientPool[client].Name);
                Console.WriteLine("Receive Error:" + ex.Message);
                _clientPool.Remove(client);
            }
            Monitor.Exit(_lockReceiveObj);
        }        

        /// <summary>
        /// 发送消息给指定对象（此过程是先发送给服务器然后又服务器转发给指定的客户端）
        /// </summary>
        private void Emit(Socket client, SocketMessage sm)
        {
            Thread emitThread = new Thread(() =>
            {
                try
                {
                    byte[] buffer = GetDataPackageToClient(sm);
                    if (client.Poll(20, SelectMode.SelectWrite))
                    {
                        client.Send(buffer, buffer.Length, SocketFlags.None);
                        Console.WriteLine("发送消息给指定用户...");
                    }

                    //这里因为没有在消息池中存储
                    //_msgPool.Remove(sm);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Emit Error：" + ex.Message);
                }
            });

            emitThread.Start();
        }

        /// <summary>
        /// 处理/获取发送给客户端消息,打包处理（加上对象的名称和时间）
        /// </summary>
        /// <param name="sm"></param>
        /// <returns></returns>
        private byte[] GetDataPackageToClient(SocketMessage sm)
        {
            StringBuilder msg = new StringBuilder();
            if (!sm.IsLoginMessage)
            {
                //login信息
                msg.AppendFormat("{0} @ {1}:\r\n", sm.Client.Name, sm.Time.ToShortTimeString());
                msg.Append(sm.Message);
            }
            else
            {
                //普通消息
                msg.AppendFormat("{0} login @ {1}", sm.Client.Name, sm.Time.ToShortTimeString());
            }

            byte[] contentBuffer = null;
            byte[] tempBuffer = Encoding.UTF8.GetBytes(msg.ToString());
            if (tempBuffer.Length < 126)
            {
                contentBuffer = new byte[tempBuffer.Length + 2];
                contentBuffer[0] = 0x81;
                contentBuffer[1] = (byte)tempBuffer.Length;

                Array.Copy(tempBuffer, 0, contentBuffer, 2, tempBuffer.Length);
            }
            else if (tempBuffer.Length < 0xFFFF)
            {
                contentBuffer = new byte[tempBuffer.Length + 4];
                contentBuffer[0] = 0x81;
                contentBuffer[1] = 126;
                contentBuffer[2] = (byte)(tempBuffer.Length & 0xFF);
                contentBuffer[3] = (byte)(tempBuffer.Length >> 8 & 0xFF);

                Array.Copy(tempBuffer, 0, contentBuffer, 4, tempBuffer.Length);
            }
            else
            {
                //超长内容(大于65535)  0xFFFF = 65535
            }

            return contentBuffer;
        }

        /// <summary>
        /// 发送握手数据包
        /// </summary>
        /// <param name="handShakeBuffer">握手数据</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        private byte[] GetHandShakeDataPackage()
        {
            RequestHeader header = new RequestHeader();
            string result = header.GetRequestHeader();
            return Encoding.UTF8.GetBytes(result);
        }

        /// <summary>
        /// 解析客户端发送来的数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private string ParseClientData(byte[] buffer, int length)
        {
            try
            {
                if (length < 2)
                    return string.Empty;

                //1bit 表示信息的最后一帧，flag，也就是标记符
                bool fin = (buffer[0] & 0x80) == 0x80;
                if (!fin)
                    return string.Empty;  //超过一帧暂时不做处理

                //RSV 1-3        1bit each 以后备用的 默认都为 0
                //Opcode         4bit 帧类型，
                //Mask           1bit 掩码，是否加密数据，默认必须置为1
                bool maskFlag = (buffer[1] & 0x80) == 0x80;  //是否包含掩码
                if (!maskFlag)
                    return string.Empty;  //不包含掩码的暂不处理

                //Payload len   7bit 数据的长度，当这个7 bit的数据 == 126 时，后面的2 个字节也是表示数据长度，
                //当它 == 127 时，后面的 8 个字节表示数据长度Masking-key      
                //1 or 4 bit 掩码Payload data  playload len  bytes 数据
                //数据长度
                int payloadLength = buffer[1] & 0x7F;
                byte[] masksBuffer = new byte[4];
                byte[] payloadBuffer;

                if (payloadLength == 126)
                {
                    Array.Copy(buffer, 4, masksBuffer, 0, 4);
                    payloadLength = (UInt16)(buffer[2] << 8 | buffer[3]);
                    payloadBuffer = new byte[payloadLength];
                    Array.Copy(buffer, 8, payloadBuffer, 0, payloadLength);
                }
                else if (payloadLength == 127)
                {
                    Array.Copy(buffer, 10, masksBuffer, 0, 4);
                    byte[] uint64Buffer = new byte[8];
                    for (int i = 0; i < 8; i++)
                    {
                        uint64Buffer[i] = buffer[9 - i];
                    }
                    UInt64 len = BitConverter.ToUInt64(uint64Buffer, 0);

                    payloadBuffer = new byte[len];
                    for (UInt64 i = 0; i < len; i++)
                    {
                        payloadBuffer[i] = buffer[i + 14];
                    }
                }
                else
                {
                    Array.Copy(buffer, 2, masksBuffer, 0, 4);
                    payloadBuffer = new byte[payloadLength];
                    Array.Copy(buffer, 6, payloadBuffer, 0, payloadLength);
                }

                //根据掩码解析数据
                for (int i = 0; i < payloadLength; i++)
                {
                    payloadBuffer[i] = (byte)(payloadBuffer[i] ^ masksBuffer[i % 4]);
                }

                return Encoding.UTF8.GetString(payloadBuffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ParseClientData Error:" + ex.Message);
                return string.Empty;
            }
        }
        #endregion
    }
}
