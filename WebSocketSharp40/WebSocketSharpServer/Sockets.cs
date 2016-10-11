using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace WebSocketSharp
{
    /// <summary>
    /// Socket连接，Socket请求方式
    /// author:chaix
    /// </summary>
    public class Sockets
    {
        private Dictionary<Socket, byte[]> _clientPool = new Dictionary<Socket, byte[]>();
        private List<string> _message = new List<string>();
        private bool _isClear = true;

        #region 属性
        #endregion

        #region 公共方法
        /// <summary>
        /// 启动服务器，监听客户端请求
        /// </summary>
        /// <param name="port">端口号</param>
        public void Run(int port, bool isBroadcast = true)
        {
            Thread serverSocketThread = new Thread(() =>
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint iep = new IPEndPoint(IPAddress.Any, port);
                socket.Bind(iep);
                socket.Listen(10);
                socket.BeginAccept(new AsyncCallback(Accept), socket);
            });

            serverSocketThread.Start();
            Console.WriteLine("Server is running...");

            if (isBroadcast)
                Broadcast();
        }
        #endregion

        #region 私有方法
        private void Accept(IAsyncResult result)
        {
            Socket socket = result.AsyncState as Socket;
            Socket client = socket.EndAccept(result);

            try
            {
                //处理下一个客户端连接
                socket.BeginAccept(new AsyncCallback(Accept), socket);
                byte[] buffer = new byte[1024];
                _clientPool.Add(client, buffer);

                client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Receive), client);
                string sessionId = client.RemoteEndPoint.ToString() + " - " + client.Handle.ToString();
                Console.WriteLine("Client ({0}) connected", sessionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:" + ex.Message);
            }
        }

        private void Receive(IAsyncResult result)
        {
            Socket client = result.AsyncState as Socket;
            if (client == null || !_clientPool.ContainsKey(client))
                return;

            int length = client.EndReceive(result);
            byte[] buffer = _clientPool[client];

            if (length > 0)
            {
                try
                {
                    client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Receive), client);
                    string content = Encoding.UTF8.GetString(buffer, 0, length);
                    if (content.Contains("Sec-WebSocket-Key"))
                    {
                        client.Send(PackHandShakeData(buffer, length));
                    }
                    else
                    {
                        string sessionId = client.RemoteEndPoint.ToString() + " - " + client.Handle.ToString();
                        PushMessage(string.Format("{0} {1}   {2}", sessionId, DateTime.Now.ToShortTimeString(), ParseClientData(buffer, length)));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Receive Error :{0}", ex.ToString());
                }
            }
            else
            {
                try
                {
                    string sessionId = client.RemoteEndPoint.ToString() + " - " + client.Handle.ToString();
                    client.Disconnect(true);
                    _clientPool.Remove(client);
                    Console.WriteLine("Client ({0}) Disconnet", sessionId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.ToString());
                }
            }
        }

        private void Broadcast()
        {
            Thread broadcastThread = new Thread(() =>
            {
                while (true)
                {
                    if (!_isClear)
                    {
                        byte[] buffer = PackageData(_message[0]);
                        foreach (KeyValuePair<Socket, byte[]> node in _clientPool)
                        {
                            Socket client = node.Key;
                            client.Send(buffer, buffer.Length, SocketFlags.None);
                        }

                        _message.RemoveAt(0);
                        _isClear = _message.Count > 0 ? true : false;
                    }
                }
            });

            broadcastThread.Start();
        }

        private byte[] PackageData(string msg)
        {
            byte[] contentBuffer = null;
            byte[] tempBuffer = Encoding.UTF8.GetBytes(msg);

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
                //处理超长内容
            }

            return contentBuffer;
        }

        private byte[] PackHandShakeData(byte[] handShakeBuffer, int length)
        {
            string handShakeText = Encoding.UTF8.GetString(handShakeBuffer, 0, length);
            string key = string.Empty;
            Regex reg = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
            Match match = reg.Match(handShakeText);
            if (match.Groups.Count != 0)
            {
                key = Regex.Replace(match.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
            }

            byte[] secKeyBuffer = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            string secKey = Convert.ToBase64String(secKeyBuffer);

            StringBuilder responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols\r\n");
            responseBuilder.Append("Upgrade: websocket\r\n");
            responseBuilder.Append("Connection: Upgrade\r\n");
            responseBuilder.Append("Sec-WebSocket-Accept: " + secKey + "\r\n\r\n");

            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }

        private void PushMessage(string context)
        {
            Console.WriteLine("Get : {0}", context);
            _message.Add(context);
            _isClear = false;
        }

        private string ParseClientData(byte[] buffer, int length)
        {
            if (length < 2)
                return string.Empty;

            //1bit(表示最后一帧)
            bool flag = (buffer[0] & 0x80) == 0x80;
            if (!flag)
                return string.Empty;  //超过一帧暂时不做处理

            bool maskFlag = (buffer[1] & 0x80) == 0x80;  //是否包含掩码
            if (!maskFlag)
                return string.Empty;  //不包含掩码的暂不处理

            //数据长度
            int dataLength = buffer[1] & 0x7F;
            byte[] masksBuffer = new byte[4];
            byte[] dataBuffer;

            if (dataLength == 126)
            {
                Array.Copy(buffer, 4, masksBuffer, 0, 4);
                dataLength = (UInt16)(buffer[2] << 8 | buffer[3]);
                dataBuffer = new byte[dataLength];
                Array.Copy(buffer, 8, dataBuffer, 0, dataLength);
            }
            else if (dataLength == 127)
            {
                Array.Copy(buffer, 10, masksBuffer, 0, 4);
                byte[] uint64Buffer = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    uint64Buffer[i] = buffer[9 - i];
                }
                UInt64 len = BitConverter.ToUInt64(uint64Buffer, 0);

                dataBuffer = new byte[len];
                for (UInt64 i = 0; i < len; i++)
                {
                    dataBuffer[i] = buffer[i + 14];
                }
            }
            else
            {
                Array.Copy(buffer, 2, masksBuffer, 0, 4);
                dataBuffer = new byte[dataLength];
                Array.Copy(buffer, 0, dataBuffer, 0, dataLength);
            }

            for (int i = 0; i < dataLength; i++)
            {
                dataBuffer[i] = (byte)(dataBuffer[i] ^ masksBuffer[i % 4]);
            }

            return Encoding.UTF8.GetString(dataBuffer);
        }
        #endregion
    }
}
