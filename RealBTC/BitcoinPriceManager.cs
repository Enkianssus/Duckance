using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RealBTC
{
    public class BitcoinPriceManager
    {
        public static event Action<int> OnPriceUpdate;

        public static void RaisePriceUpdate(int price)
        {
            OnPriceUpdate?.Invoke(price);
        }

        public static BitcoinPriceManager Instance { get; private set; }

        public static void Init()
        {
            if (Instance== null)
            {
                Instance = new BitcoinPriceManager();
            }
        }

        public static int CurrentBitcoinPrice { get; private set; } = -1;

        private static int _lastPrice = -1;
        public static float Growth = 0;

        public static int CurrentBitcoinPriceDivideBy5
        {
            get
            {
                if (CurrentBitcoinPrice == -1) return -1;
                return CurrentBitcoinPrice / 5;
            }
            private set
            {
                
            }
        }


        public float priceUpdateInterval = 10f; // 默认 5 分钟

        private CancellationTokenSource _cts;

        

       

        public void StartUpdateLoop()
        {
            StopUpdateLoop(); // 确保不会重复启动
            _cts = new CancellationTokenSource();
            UpdateLoopAsync(_cts.Token).Forget();
        }

        public void StopUpdateLoop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async UniTaskVoid UpdateLoopAsync(CancellationToken token)
        {
            Debug.Log("[BitcoinPriceManager] 更新循环启动");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await FetchBitcoinPriceAsync(token);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BitcoinPriceManager] 更新价格失败: {ex.Message}");
                }

                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(priceUpdateInterval), cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            Debug.Log("[BitcoinPriceManager] 更新循环停止");
        }

        private async UniTask FetchBitcoinPriceAsync(CancellationToken token)
        {
            string url = "https://api.binance.me/api/v3/ticker/price?symbol=BTCUSDT";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                var asyncOp = webRequest.SendWebRequest();

                while (!asyncOp.isDone)
                {
                    token.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string json = webRequest.downloadHandler.text;

                    // 解析 price
                    int parsedPrice = ParsePriceFromBinance(json);

                    CurrentBitcoinPrice = parsedPrice;

                    if (_lastPrice <= 0)
                    {
                        _lastPrice = CurrentBitcoinPrice;
                        Growth = 0f;
                    }
                    else
                    {
                        float change = (CurrentBitcoinPrice - _lastPrice) / (float)_lastPrice;
                        Growth = change * 100f;
                        _lastPrice = CurrentBitcoinPrice;
                    }
                    OnPriceUpdate?.Invoke(CurrentBitcoinPrice/5);

                    Debug.Log($"[BitcoinPriceManager] 当前BTC价格: ${CurrentBitcoinPrice:N0} | 涨跌: {Growth:+0.##;-0.##;0}%");
                }
                else
                {
                    Debug.LogWarning($"[BitcoinPriceManager] 网络请求失败: {webRequest.error}");
                }
            }
        }

        // 解析 Binance 返回的 price 字符串
        private int ParsePriceFromBinance(string json)
        {
            try
            {
                // Binance 返回格式: {"symbol":"BTCUSDT","price":"110336.75000000"}
                int start = json.IndexOf("\"price\":\"") + 9; // 9 是 "price":"
                int end = json.IndexOf("\"", start);         // 找到结束的引号
                string value = json.Substring(start, end - start);
                return Mathf.RoundToInt(float.Parse(value));
            }
            catch
            {
                return -1;
            }
        }
        private async UniTask FetchBitcoinPriceAsync1(CancellationToken token)
        {
            string url = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                var asyncOp = webRequest.SendWebRequest();

                while (!asyncOp.isDone)
                {
                    token.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string json = webRequest.downloadHandler.text;
                    int parsedPrice = ParsePrice(json);

                    CurrentBitcoinPrice = parsedPrice;

                    if (_lastPrice <= 0)
                    {
                        // 首次有效价格
                        _lastPrice = CurrentBitcoinPrice;
                        Growth = 0f;
                    }
                    else
                    {
                        // 总是计算最新涨跌幅
                        float change = (CurrentBitcoinPrice - _lastPrice) / (float)_lastPrice;
                        Growth = change * 100f;
                        _lastPrice = CurrentBitcoinPrice;
                    }

                    Debug.Log($"[BitcoinPriceManager] 当前BTC价格: ${CurrentBitcoinPrice:N0} | 涨跌: {Growth:+0.##;-0.##;0}%");
                }
                else
                {
                    Debug.LogWarning($"[BitcoinPriceManager] 网络请求失败: {webRequest.error}");
                }
            }
        }

        private int ParsePrice(string json)
        {
            try
            {
                int start = json.IndexOf("usd") + 5;
                int end = json.IndexOf('}', start);
                string value = json.Substring(start, end - start);
                return Mathf.RoundToInt(float.Parse(value));
            }
            catch
            {
                return 0;
            }
        }

        private void OnDestroy()
        {
            StopUpdateLoop();
        }
    }
    
}