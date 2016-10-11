using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using WebSocketSharpUtil;

namespace WebSocketSharp
{
    /// <summary>
    /// Server Socket(WebSocket)连接，Tcp请求方式
    /// author:chaix
    /// date:2016-09-12
    /// </summary>
    public class Tcp
    {
        private Dictionary<Socket, ClientInfo> _clientPool = new Dictionary<Socket, ClientInfo>();
        private List<SocketMessage> _msgPool = new List<SocketMessage>();
        private bool _isClear = true;
        private object _lockAcceptObj = new object();
        private object _lockReceiveObj = new object();

        #region 属性
        //private int _port = 9000; //默认是9000
        ///// <summary>
        ///// 端口号
        ///// </summary>
        //public int Port
        //{
        //    get { return _port; }
        //    set { _port = value; }
        //}
        #endregion

        #region 公共方法
        ///// <summary>
        ///// 启动服务器，监听客户端请求
        ///// </summary>
        //public void Run()
        //{
        //    if (_port <= 0)
        //    {
        //        Console.WriteLine("请输入监听的端口号");
        //        return;
        //    }
        //    Run(_port, false);
        //}

        /// <summary>
        /// 启动服务器，监听客户端请求
        /// </summary>
        /// <param name="port"></param>
        /// <param name="isBroadcase">是否广播消息</param>
        public void Run(int port, bool isBroadcase = true)
        {
            Thread serverSocketThread = new Thread(() =>
            {
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(new IPEndPoint(IPAddress.Any, port));
                server.Listen(10);
                server.BeginAccept(new AsyncCallback(Accept), server);
            });

            serverSocketThread.Start();
            Console.WriteLine("Server is running...");

            if (isBroadcase)
            {
                //向客户端广播
                Broadcast();
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 处理客户端连接请求,成功后把客户端加入到clientPool中
        /// </summary>
        /// <param name="result"></param>
        private void Accept(IAsyncResult result)
        {
            //监视器
            Monitor.Enter(_lockAcceptObj);
            Socket server = result.AsyncState as Socket;
            Socket client = server.EndAccept(result);
            try
            {
                //处理下一个客户端连接
                server.BeginAccept(new AsyncCallback(Accept), server);
                byte[] buffer = new byte[1024];
                //接收客户端消息
                client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Receive), client);

                ClientInfo info = new ClientInfo();
                info.Id = client.RemoteEndPoint;
                info.Handle = client.Handle;
                info.Buffer = buffer;
                //把客户端存入clientPool
                _clientPool.Add(client, info);
                Console.WriteLine(string.Format("Client {0} connecting", client.RemoteEndPoint));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Accept Error :" + ex.ToString());
            }
            Monitor.Exit(_lockAcceptObj);
        }

        /// <summary>
        /// 接收客户端发送的消息，接收成功后加入到_msgPool，等待广播或者发送给指定对象
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
                        client.Send(GetHandShakeDataPackage(buffer, length));
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
        /// 在单独的线程中，向所有客户端广播消息
        /// </summary>
        private void Broadcast()
        {
            Thread broadcastThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        if (!_isClear)
                        {
                            byte[] msg = GetDataPackageToClient(_msgPool[0]);
                            foreach (KeyValuePair<Socket, ClientInfo> node in _clientPool)
                            {
                                Socket client = node.Key;
                                if (client.Poll(20, SelectMode.SelectWrite))
                                {
                                    client.Send(msg, msg.Length, SocketFlags.None);
                                    Console.WriteLine("广播消息已发送...");
                                }
                                Console.WriteLine("Broadcast socket：" + client.Connected);
                            }

                            _msgPool.RemoveAt(0);
                            _isClear = _msgPool.Count == 0 ? true : false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Broadcast Error：" + ex.Message);
                    }
                }
            });

            broadcastThread.Start();
        }

        /// <summary>
        /// 发送消息给指定对象
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
        /// 处理/获取服务器握手数据包
        /// </summary>
        /// <param name="handShakeBuffer">握手数据</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        private byte[] GetHandShakeDataPackage(byte[] handShakeBuffer, int length)
        {
            #region websocket 协议解释
            //GET /chat HTTP/1.1
            //Host: server.example.com
            //Upgrade: websocket  告诉服务器这个HTTP连接是升级的Websocket连接。
            //Connection: Upgrade  告知服务器当前请求连接是升级的。
            //Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==  为了表示服务器同意和客户端进行Socket连接, 服务器端需要使用客户端发送的这个Key进行校验 ，然后返回一个校验过的字符串给客户端，客户端验证通过后才能正式建立Socket连接。
            //Origin: http://example.com  该字段是用来防止客户端浏览器使用脚本进行未授权的跨源攻击，这个字段在WebSocket协议中非常重要。服务器要根据这个字段判断是否接受客户端的Socket连接。可以返回一个HTTP错误状态码来拒绝连接。
            //Sec-WebSocket-Protocol: chat, superchat  字段表示客户端可以接受的子协议类型，也就是在Websocket协议上的应用层协议类型。上面可以看到客户端支持chat和superchat两个应用层协议，当服务器接受到这个字段后要从中选出一个协议返回给客户端。
            //Sec-WebSocket-Version: 13
            //连接建立后，握手必须要是一个有效的HTTP请求
            //请求的方式必须是GET，HTTP协议的版本至少是1.1
            //Upgrade字段必须包含而且必须是"websocket"，Connection字段必须内容必须是“Upgrade”
            //Sec-Websocket-Version必须，而且必须是13
            #endregion

            string handShakeText = Encoding.UTF8.GetString(handShakeBuffer, 0, length);

            ResponseHeader header = new ResponseHeader();
            string result = header.GetResponseHeader(handShakeText);

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
