using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace RealBTC.Network
{
    public static class BinanceWebSocketClient
    {
        public static event Action<KlineInfo> OnPriceUpdate;

        private static ClientWebSocket _webSocket;
        private static CancellationTokenSource _cts;

        private static bool _isReconnecting = false;
        public static int CurrentPrice=-1;
        public static int CurrentPriceDivideBy5=-1;

        public static bool IsConnected
        {
            get
            {
                return _webSocket != null &&
                       (_webSocket.State == WebSocketState.Open ||
                        _webSocket.State == WebSocketState.CloseSent);
            }
        }

        public static void Init(string symbol = "btcusdt")
        {
            if (IsConnected)
            {
                Debug.LogWarning("WebSocket 已经连接！");
                return;
            }

            _cts = new CancellationTokenSource();
            //ConnectWebSocket(symbol, _cts.Token).Forget();
            ConnectWebSocketAsync(symbol, _cts.Token).Forget();
        }

        private static async UniTask ConnectWebSocket(string symbol, CancellationToken token)
        {
            _webSocket = new ClientWebSocket();
            string url = $"wss://stream.binance.me:9443/ws/{symbol.ToLower()}@kline_1m";

            try
            {
                await _webSocket.ConnectAsync(new Uri(url), token);
                Debug.Log($"WebSocket 已连接: {symbol}");

                await ReceiveLoop(token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket 连接失败: {ex}");
            }
        }
        private static readonly string[] Endpoints = new[]
        {
            "wss://stream.binance.me:9443/ws/",
            "wss://stream.binance.us:9443/ws/",
            "wss://stream.binance.com:9443/ws/",
        };

        public static  bool isUS=false;

        public static async UniTask ConnectWebSocketAsync(string symbol, CancellationToken token)
        {
            string endpoint = await GetAvailableEndpoint(symbol, token);
            if (endpoint == null)
            {
                Debug.LogError("无法连接到任何 Binance WebSocket 端点。");
                return;
            }

            string url = $"{endpoint}{symbol.ToLower()}@kline_1m";
            _webSocket = new ClientWebSocket();

            try
            {
                await _webSocket.ConnectAsync(new Uri(url), token);
                Debug.Log($"✅ WebSocket 已连接: {url}");
                await ReceiveLoop(token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ WebSocket 连接失败: {ex}");
            }
        }

        private static async UniTask TryReconnect(string symbol, CancellationToken token)
        {
            if (_isReconnecting) return;
            _isReconnecting = true;

            Debug.LogWarning("[BinanceWS] 尝试重连...");
            await UniTask.Delay(3000, cancellationToken: token);

            if (!token.IsCancellationRequested)
            {
                await ConnectWebSocketAsync(symbol, token);
            }

            _isReconnecting = false;
        }
        
        // 自动选择可连接的端点
        private static async UniTask<string> GetAvailableEndpoint(string symbol, CancellationToken token)
        {
            foreach (string endpoint in Endpoints)
            {
                string testUrl = $"{endpoint}{symbol.ToLower()}@kline_1m";
                bool ok = await TestEndpointAsync(testUrl, token);
                if (ok)
                {
                    Debug.Log($"🌐 使用可用节点: {endpoint}");
                    if (endpoint == Endpoints[1]) isUS = true;
                    else  isUS = false;
                    return endpoint;
                }
            }
            return null;
        }

        // 测试连接是否可用（超时3秒）
        private static async UniTask<bool> TestEndpointAsync(string url, CancellationToken token)
        {
            using var ws = new ClientWebSocket();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, token);
                await ws.ConnectAsync(new Uri(url), linked.Token);
                return ws.State == WebSocketState.Open;
            }
            catch
            {
                return false;
            }
        }

        private static async UniTask ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[1024 * 8];

            //Debug.Log(IsConnected);
           // // Debug.Log(token.IsCancellationRequested);

            while (IsConnected && !token.IsCancellationRequested)
            {
                 // Debug.Log("ReceiveLoop");

                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", token);
                        break;
                    }

                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessMessage(msg);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"WebSocket 接收异常: {ex}");
                    //IsConnected = false;   // 标记断开
                    break;                 // 跳出循环，让外层逻辑重连
                }
                
            }
            if (!_isReconnecting && !token.IsCancellationRequested)
            {
                await TryReconnect("btcusdt", token);
            }
        }
        

        private static void ProcessMessage(string msg)
        {
           // Debug.Log(msg);
            try
            {
                var wrapper = JsonConvert.DeserializeObject<KlineWrapper>(msg);
                if (wrapper?.k != null)
                {
                    var k = wrapper.k;
                    //Debug.Log($"Open: {k.o}, Close: {k.c}, F: {k.f}");
                    var info = new KlineInfo(
                        open: Mathf.FloorToInt(float.Parse(k.o)),
                        close: Mathf.FloorToInt(float.Parse(k.c)),
                        high: Mathf.FloorToInt(float.Parse(k.h)),
                        low: Mathf.FloorToInt(float.Parse(k.l)),
                        trades: (k.n),
                        flag: k.f==-1
                    );
                    
                 //   Debug.Log(CurrentPrice);
                    CurrentPrice = info.Close;
                    CurrentPriceDivideBy5 = info.Close / 5;
                    OnPriceUpdate?.Invoke(info);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }
        // private static void ProcessMessage(string msg)
        // {
        //     Debug.Log("ProcessMessage");
        //
        //     try
        //     {
        //         Debug.Log(msg);
        //
        //         var json = JsonUtility.FromJson<KlineWrapper>(msg);
        //         Debug.Log("JsonUtility");
        //         Debug.Log(json);
        //         Debug.Log(json.e);
        //
        //         Debug.Log(json.k);
        //
        //         if (json != null && json.k != null)
        //         {
        //             Debug.Log("json != null && json.k != null");
        //
        //             var k = json.k;
        //             // 构造 readonly struct 并触发事件
        //             var info = new KlineInfo(
        //                 open: Mathf.FloorToInt(float.Parse(k.o)),
        //                 close: Mathf.FloorToInt(float.Parse(k.c)),
        //                 high: Mathf.FloorToInt(float.Parse(k.h)),
        //                 low: Mathf.FloorToInt(float.Parse(k.l)),
        //                 trades: (k.n)
        //             );
        //
        //             Debug.Log(CurrentPrice);
        //             CurrentPrice = info.Close;
        //             CurrentPriceDivideBy5 = info.Close / 5;
        //             OnPriceUpdate?.Invoke(info);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogWarning($"解析 WebSocket 消息失败: {ex}");
        //     }
        // }

        public static async UniTask Close()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (_webSocket != null)
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing",
                            CancellationToken.None);
                    }
                    catch
                    {
                    }
                }

                _webSocket.Dispose();
                _webSocket = null;
            }
        }

        [Serializable]
        private class KlineWrapper
        {
            public string e;
            public KlineData k;
        }

        [Serializable]
        private class KlineData
        {
            public string o; // 开盘
            public string c; // 收盘
            public string h; // 最高
            public string l; // 最低
            public long f; // 成交数
            public int n; // 成交数
        }

        public readonly struct KlineInfo
        {
            public readonly int Open;
            public readonly int Close;
            public readonly int High;
            public readonly int Low;
            public readonly int Trades;
            public readonly bool flag;

            public KlineInfo(int open, int close, int high, int low, int trades,bool flag)
            {
                Open = open;
                Close = close;
                High = high;
                Low = low;
                Trades = trades;
                this.flag = flag;
            }
        }
    }
}

