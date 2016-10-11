using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace WebSocketSharpUtil
{
    public class ResponseHeader
    {
        private string _handShakeText;
        private string _wsUrl;

        #region 构造函数
        //public ResponseHeader(string handShakeText)
        //{
        //    this._handShakeText = handShakeText;
        //}

        //public ResponseHeader(string handShakeText, string ws)
        //{
        //    this._handShakeText = handShakeText;
        //    this._wsUrl = ws;
        //}
        #endregion


        //应答包中冒号后面有一个空格
        private string _firstLine;
        /// <summary>
        /// 首行(HTTP/1.1 101 Switching Protocols)
        /// </summary>
        public string FirstLine
        {
            get
            {
                _firstLine = "HTTP/1.1 101 Switching Protocols\r\n";
                return _firstLine;
            }
            set { _firstLine = value; }
        }

        private string _upgrade;
        /// <summary>
        /// Upgrade
        /// </summary>
        public string Upgrade
        {
            get
            {
                _upgrade = "Upgrade: websocket\r\n";
                return _upgrade;
            }
            set { _upgrade = value; }
        }

        private string _connection;
        /// <summary>
        /// Connection
        /// </summary>
        public string Connection
        {
            get
            {
                _connection = "Connection: Upgrade\r\n";
                return _connection;
            }
            set { _connection = value; }
        }

        private string _secWebSocketAccept;
        /// <summary>
        /// Sec-WebSocket-Accept
        /// </summary>
        public string SecWebSocketAccept
        {
            get
            {
                //最后需要两个空行作为应答包结束
                _secWebSocketAccept = "Sec-WebSocket-Accept: " + GetSecWebSocketAccept(this._handShakeText) + "\r\n\r\n";
                return _secWebSocketAccept;
            }
            set { _secWebSocketAccept = value; }
        }

        private string _secWebSocketProtocol;
        /// <summary>
        /// Sec-WebSocket-Protocol
        /// </summary>
        public string SecWebSocketProtocol
        {
            get
            {
                _secWebSocketProtocol = "Sec-WebSocket-Protocol: chat\r\n";
                return _secWebSocketProtocol;
            }
            set { _secWebSocketProtocol = value; }
        }

        private string _server;
        /// <summary>
        /// Server
        /// </summary>
        public string Server
        {
            get
            {
                _server = "Server: \r\n";
                return _server;
            }
            set { _server = value; }
        }

        private string _date;
        /// <summary>
        /// Date
        /// </summary>
        public string Date
        {
            get
            {
                _date = "Date: " + DateTime.Now.ToLongDateString() + "\r\n";
                return _date;
            }
            set { _date = value; }
        }

        private string _accessControlAllowCredentials;
        /// <summary>
        /// Access-Control-Allow-Credentials
        /// </summary>
        public string AccessControlAllowCredentials
        {
            get
            {
                _accessControlAllowCredentials = "Access-Control-Allow-Credentials: true\r\n";
                return _accessControlAllowCredentials;
            }
            set { _accessControlAllowCredentials = value; }
        }

        private string _accessControlAllowHeaders;
        /// <summary>
        /// Access-Control-Allow-Headers
        /// </summary>
        public string AccessControlAllowHeaders
        {
            get
            {
                _accessControlAllowHeaders = "Access-Control-Allow-Headers: content-type\r\n";
                return _accessControlAllowHeaders;
            }
            set { _accessControlAllowHeaders = value; }
        }

        private string _secWebSocketOrigin;
        /// <summary>
        /// Sec-WebSocket-Origin
        /// </summary>
        public string SecWebSocketOrigin
        {
            get
            {
                _secWebSocketOrigin = "Sec-WebSocket-Origin: file://" + "\r\n";
                return _secWebSocketOrigin;
            }
            set { _secWebSocketOrigin = value; }
        }

        private string _secWebSocketLocation;
        /// <summary>
        /// Sec-WebSocket-Location
        /// </summary>
        public string SecWebSocketLocation
        {
            get
            {
                _secWebSocketLocation = "Sec-WebSocket-Location: " + _wsUrl + "\r\n";
                return _secWebSocketLocation;
            }
            set { _secWebSocketLocation = value; }
        }

        /// <summary>
        /// 组成握手的返回头信息
        /// </summary>
        /// <returns></returns>
        public string GetResponseHeader(string handShakeText = "", string webSocket = "")
        {
            StringBuilder responseBuilder = new StringBuilder();
            responseBuilder.Append(FirstLine);  //表示变换协议
            //这两个字段是服务器返回的告知客户端同意使用升级并使用websocket协议，用来完善HTTP升级响应
            responseBuilder.Append(Upgrade);
            responseBuilder.Append(Connection);

            if (!string.IsNullOrEmpty(handShakeText))
            {
                this._handShakeText = handShakeText;
            }

            responseBuilder.Append(SecWebSocketAccept);

            if (!string.IsNullOrEmpty(webSocket))
            {
                this._wsUrl = webSocket;
                responseBuilder.Append(SecWebSocketLocation);
            }

            responseBuilder.Append(SecWebSocketProtocol);

            return responseBuilder.ToString();
        }


        /// <summary>
        /// 获取对应的Sec-WebSocket-Accept
        /// </summary>
        /// <param name="handShakeText"></param>
        /// <returns></returns>
        private string GetSecWebSocketAccept(string handShakeText)
        {
            string secKey = string.Empty;
            try
            {
                //服务器端需要使用客户端发送的这个Key进行校验
                //然后返回一个校验过的字符串给客户端，客户端验证通过后才能正式建立Socket连接
                //服务器验证方法是： 首先进行 Key + 全局唯一标示符（GUID）“258EAFA5-E914-47DA-95CA-C5AB0DC85B11”连接起来
                //然后将连接起来的字符串使用SHA-1哈希加密，再进行base64加密，将得到的字符串返回给客户端作为握手依据。
                //其中GUID是一个对于不识别WebSocket的网络端点不可能使用的字符串 
                string key = string.Empty;
                Regex reg = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
                Match match = reg.Match(handShakeText);
                if (!string.IsNullOrEmpty(match.Value))
                    key = Regex.Replace(match.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();

                byte[] secKeyBuffer = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));

                //服务器返回正确的应答要求：
                //HTTP/1.1 101 Switching Protocols
                //Upgrade: websocket
                //Connection: Upgrade
                //Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=
                //Sec-WebSocket-Protocol: chat

                //这里必须要遵循巴科斯范式（ABNF）
                secKey = Convert.ToBase64String(secKeyBuffer);
            }
            catch (Exception ex)
            { }

            return secKey;
        }
    }
}
