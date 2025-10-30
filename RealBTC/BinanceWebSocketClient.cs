using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RealBTC
{
    public static class BinanceWebSocketClient
    {
        private static ClientWebSocket _webSocket;
        private static CancellationTokenSource _cts;

        /// <summary>
        /// 最新的 BTC 价格
        /// </summary>
        public static int CurrentBitcoinPrice { get; private set; }

        /// <summary>
        /// 价格更新事件，UI 或其他系统可订阅
        /// </summary>

        /// <summary>
        /// 初始化 WebSocket 并开始监听
        /// </summary>
        public static void Init(string symbol = "btcusdt")
        {
            Debug.LogWarning("WebSocket 初始化！");

            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                Debug.LogWarning("WebSocket 已经初始化并连接！");
                return;
            }

            _cts = new CancellationTokenSource();
            ConnectWebSocket(symbol, _cts.Token).Forget();
        }

        private static async UniTask ConnectWebSocket(string symbol, CancellationToken token)
        {
            _webSocket = new ClientWebSocket();
            //string url = $"wss://stream.binance.com:9443/ws/{symbol.ToLower()}@trade";
            string url = $"wss://stream.binance.com:9443/ws/btcusdt@trade";

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

        private static async UniTask ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[1024 * 4];

            while (_webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("WebSocket 被服务器关闭");
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", token);
                        break;
                    }
                    else
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessMessage(msg);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"WebSocket 接收异常: {ex}");
                }
            }
        }

        private static void ProcessMessage(string msg)
        {
            try
            {
                var json = JsonUtility.FromJson<TradeMsg>(msg);
                if (json != null && !string.IsNullOrEmpty(json.p))
                {
                    if (float.TryParse(json.p, out float priceFloat))
                    {
                        CurrentBitcoinPrice = Mathf.FloorToInt(priceFloat);
                        Debug.Log(CurrentBitcoinPrice);
                        BitcoinPriceManager.RaisePriceUpdate(CurrentBitcoinPrice/5);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"解析 WebSocket 消息失败: {ex}");
            }
        }

        /// <summary>
        /// 关闭 WebSocket
        /// </summary>
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
        private class TradeMsg
        {
            public string e; // event type
            public long E; // event time
            public string s; // symbol
            public string p; // price
        }
    }
}