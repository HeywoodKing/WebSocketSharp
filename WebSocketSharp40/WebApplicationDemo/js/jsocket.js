//这是一个标准的demo
(function ($) {
    var $j = $;
    var ws;
    var msgObj = $j("#result");
    var port = "8900";
    var http = "192.168.10.253";
    var url = "ws://" + http + ":" + port;

    window.JSocket = {
        socket: null,
        userObject: null,

        init: function () {
            var support = "MozWebSocket" in window ? "MozWebSocket" : ('WebSocket' in window ? 'WebSocket' : null);
            if (support == null) {
                alert("您的浏览器不支持WebSocket!");
                msgObj.text("您的浏览器不支持WebSocket!");
                return false;
            }

            // if(!window.WebSocket){
            // 	alert("您的浏览器不支持WebSocket!");
            // 	msgObj.text("您的浏览器不支持WebSocket!");
            // 	return false;
            // }

            this.connect();

        },

        connect: function () {
            try {
                ws = new WebSocket(url);  //, 'subprotocol'
                // ws.binaryType = "blob";
                if (ws.readyState === WebSocket.CONNECTING) {
                    console.log("正在连接WebSocket服务器...");
                    msgObj.text("正在连接WebSocket服务器...");
                }

                ws.onopen = this.onopen;
                ws.onmessage = this.onmessage;
                ws.onclose = this.onclose;
                ws.onerror = this.onerror;
            } catch (e) {
                console.log("Error：" + e);
                msgObj.text("Error：" + e.Message);
            }
        },

        disconnect: function () {
            if (ws != null && ws.readyState === WebSocket.OPEN) {
                ws.close();  //关闭TCP连接
            }
        },

        onopen: function (e) {
            //JSocket.sendMessage("Test!");
            console.log("open");
            msgObj.text("open");
            if (ws.readyState === WebSocket.OPEN) {
                console.log("已连接到WebSocket服务器");
                msgObj.text("已连接到WebSocket服务器");
            }

            // var THRESHOLD = 10240;
            // setInterval(function(){
            // 	if(ws.bufferedAmount < THRESHOLD){
            // 		JSocket.sendMessage("测试");
            // 	}
            // }, 1000);
        },

        onmessage: function (e) {
            console.log("message");
            msgObj.text("message");
        },

        onclose: function (e) {
            var result = JSocket.getWebSocketState(ws);
            // console.log("close事件wasClean：" + e.wasClean + ",code：" + e.code + ",error：" + e.error + ",reason：" + e.reason + "," + result);
            msgObj.text("close事件wasClean：" + e.wasClean + ",code：" + e.code + ",error：" + e.error + ",reason：" + e.reason + "," + result);

            // 断开后重新连接
            // if(ws.readyState !== WebSocket.OPEN){
            // 	setTimeout(function(){
            // 		JSocket.connect();
            // 	}, 1000 * 3);
            // }
        },

        onerror: function (e) {
            console.log("error:" + e);
            // msgObj.text("error");
        },

        // WebSocket可以收发消息的类型有String、Blob和ArrayBuffer
        // readyState、bufferedAmount 和protocol。
        // bufferedAmount 特性检查已经进入队列，但是尚未发送到服务器的字节数
        sendMessage: function (msg) {
            if (ws != null && ws.readyState === WebSocket.OPEN) {
                if (msg == "" || msg == null || msg == "undefined") {
                    return false;
                }
                ws.send(msg);
                console.log(msg);
            } else {
                console.log("发送失败！原因：可能是WebSocket未能建立连接！");
                msgObj.text("发送失败！原因：可能是WebSocket未能建立连接！");
            }
        },

        getWebSocketState: function (ws) {
            var result = "";
            switch (ws.readyState) {
                case 0:
                    result = "连接正在进行中，但还未建立";
                    break;
                case 1:
                    result = "连接已经建立。消息可以在客户端和服务器之间传递";
                    break;
                case 2:
                    result = "连接正在进行关闭握手";
                    break;
                case 3:
                    result = "连接已经关闭，不能打开";
                    break;
            }

            return result;
        },

        log: function (s) {
            if (document.readyState !== "complete") {
                // log.buffer.push(s);
            } else {
                msgObj.html(s + "\n");
            }
        },

        jsonToString: function (json) {
            return JSON.stringify(json);
        },

        stringToJson: function (str) {
            try {
                str = str.replace(/\'/g, "\"");
                return JSON.parse(str);
            } catch (error) {
                console.log(error);
            }
        }
    };

})(jQuery);