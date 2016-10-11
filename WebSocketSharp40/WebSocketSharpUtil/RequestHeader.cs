using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketSharpUtil
{
    public class RequestHeader
    {
        //握手的请求头信息
        //GET / HTTP/1.1

        //GET /chat HTTP/1.1
        //Sec-WebSocket-Protocol: chat, superchat
        //Host: 192.168.10.253:8900
        //User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0
        //Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
        //Accept-Language: zh-CN,zh;q=0.8,en-US;q=0.5,en;q=0.3
        //Accept-Encoding: gzip, deflate
        //DNT: 1
        //Sec-WebSocket-Version: 13
        //origin: null
        //Sec-WebSocket-Extensions: permessage-deflate
        //Sec-WebSocket-Key: eEsgUL2e7bf+b2yPCMEiHA==
        //Connection: keep-alive, Upgrade
        //Pragma: no-cache
        //Cache-Control: no-cache
        //Upgrade: websocket

        private string _firstLine;
        /// <summary>
        /// GET /chat HTTP/1.1
        /// </summary>
        public string FirstLine
        {
            get 
            {
                _firstLine = "GET /chat HTTP/1.1\r\n";  //GET /chat HTTP/1.1\r\n
                return _firstLine; 
            }
            set { _firstLine = value; }
        }

        private string _secWebSocketProtocol;
        /// <summary>
        /// Sec-WebSocket-Protocol
        /// </summary>
        public string SecWebSocketProtocol
        {
            get
            {
                _secWebSocketProtocol = "Sec-WebSocket-Protocol: chat, superchat\r\n";
                return _secWebSocketProtocol;
            }
            set { _secWebSocketProtocol = value; }
        }

        private string _host;
        /// <summary>
        /// Host
        /// </summary>
        public string Host
        {
            get 
            {
                _host = "Host: 192.168.10.253:8900\r\n";
                return _host; 
            }
            set { _host = value; }
        }

        private string _userAgent;
        /// <summary>
        /// User-Agent
        /// </summary>
        public string UserAgent
        {
            get 
            {
                _userAgent = "User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0\r\n";
                return _userAgent; 
            }
            set { _userAgent = value; }
        }

        private string _accept;
        /// <summary>
        /// Accept
        /// </summary>
        public string Accept
        {
            get 
            {
                _accept = "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\n";
                return _accept; 
            }
            set { _accept = value; }
        }

        private string _acceptLanguage;
        /// <summary>
        /// Accept-Language
        /// </summary>
        public string AcceptLanguage
        {
            get 
            {
                _acceptLanguage = "Accept-Language: zh-CN,zh;q=0.8,en-US;q=0.5,en;q=0.3\r\n";
                return _acceptLanguage; 
            }
            set { _acceptLanguage = value; }
        }

        private string _acceptEncoding;
        /// <summary>
        /// Accept-Encoding
        /// </summary>
        public string AcceptEncoding
        {
            get 
            {
                _acceptEncoding = "Accept-Encoding: gzip, deflate\r\n";
                return _acceptEncoding; 
            }
            set { _acceptEncoding = value; }
        }

        private string _dnt;
        /// <summary>
        /// DNT
        /// </summary>
        public string Dnt
        {
            get 
            {
                _dnt = "DNT: 1\r\n";
                return _dnt; 
            }
            set { _dnt = value; }
        }

        private string _secWebSocketVersion;
        /// <summary>
        /// Sec-WebSocket-Version
        /// </summary>
        public string SecWebSocketVersion
        {
            get
            {
                _secWebSocketVersion = "Sec-WebSocket-Version: 13\r\n";
                return _secWebSocketVersion;
            }
            set { _secWebSocketVersion = value; }
        }

        private string _origin;
        /// <summary>
        /// Origin
        /// </summary>
        public string Origin
        {
            get
            {
                _origin = "origin: http://192.168.10.253\r\n";
                return _origin;
            }
            set { _origin = value; }
        }

        private string _secWebSocketExtensions;
        /// <summary>
        /// Sec-WebSocket-Extensions
        /// </summary>
        public string SecWebSocketExtensions
        {
            get 
            {
                _secWebSocketExtensions = "Sec-WebSocket-Extensions: permessage-deflate\r\n";
                return _secWebSocketExtensions; 
            }
            set { _secWebSocketExtensions = value; }
        }

        private string _secWebSocketKey;
        /// <summary>
        /// Sec-WebSocket-Key
        /// </summary>
        public string SecWebSocketKey
        {
            get
            {
                _secWebSocketKey = "Sec-WebSocket-Key: " + GetSecWebSocketKey() + "\r\n";
                return _secWebSocketKey;
            }
            set { _secWebSocketKey = value; }
        }

        private string _connection;
        /// <summary>
        /// Connection
        /// </summary>
        public string Connection
        {
            get
            {
                _connection = "Connection: keep-alive, Upgrade\r\n";
                return _connection;
            }
            set { _connection = value; }
        }

        private string _pragma;
        /// <summary>
        /// Pragma
        /// </summary>
        public string Pragma
        {
            get 
            {
                _pragma = "Pragma: no-cache\r\n";
                return _pragma; 
            }
            set { _pragma = value; }
        }

        private string _cacheControl;
        /// <summary>
        /// Cache-Control
        /// </summary>
        public string CacheControl
        {
            get 
            {
                _cacheControl = "Cache-Control: no-cache\r\n";
                return _cacheControl; 
            }
            set { _cacheControl = value; }
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

        /// <summary>
        /// 组成握手的请求头信息
        /// </summary>
        /// <returns></returns>
        public string GetRequestHeader()
        {
            StringBuilder requestBuilder = new StringBuilder();
            requestBuilder.Append(FirstLine);
            requestBuilder.Append(SecWebSocketProtocol);
            requestBuilder.Append(Host);
            requestBuilder.Append(UserAgent);
            requestBuilder.Append(Accept);
            requestBuilder.Append(AcceptLanguage);
            requestBuilder.Append(AcceptEncoding);
            requestBuilder.Append(Dnt);
            requestBuilder.Append(SecWebSocketVersion);
            requestBuilder.Append(Origin);
            requestBuilder.Append(SecWebSocketExtensions);
            requestBuilder.Append(SecWebSocketKey);
            requestBuilder.Append(Connection);
            requestBuilder.Append(Pragma);
            requestBuilder.Append(CacheControl);
            requestBuilder.Append(Upgrade);

            return requestBuilder.ToString();
        }

        /// <summary>
        /// 生成客户端的Key进行校验
        /// </summary>
        /// <returns></returns>
        private string GetSecWebSocketKey()
        {
            string key = string.Empty;
            key = "eEsgUL2e7bf+b2yPCMEiHA==";
            return key;
        }
    }
}
