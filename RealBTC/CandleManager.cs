using System;
using System.Collections.Generic;
using Duckov.BlackMarkets.UI;
using RealBTC.Network;
using UnityEngine;
using UnityEngine.UI;

namespace RealBTC.UI
{
    
    public class CandleManager
    {
        public static CandleManager? Instance;
        public static Color upColor = new Color(0.26f, 0.83f, 0.48f);
        public static Color downColor = new Color(0.95f, 0.33f, 0.32f);
        private FixedCircleQueue<BinanceWebSocketClient.KlineInfo> recentPrices;
        private CandleCirclePool candlePool;
        private int capacity;

        private int viewMaxPrice = 0;
        private int viewMinPrice = Int32.MaxValue;

        public int Count => recentPrices.Count;

        public static void OnDestroy()
        {
            Instance = null;
        }

        public CandleManager(int capacity, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            //Debug.Log("new CandleManagernew CandleManagernew CandleManagernew CandleManagernew CandleManager");
            GameObject imageGO = new GameObject("Kline", typeof(RectTransform), typeof(Image),
                typeof(HorizontalLayoutGroup));
            imageGO.transform.SetParent(parent, false); // 设为当前物体的子对象，不改变局部坐标

            var image = imageGO.GetComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);
            
            RectTransform rect = imageGO.GetComponent<RectTransform>();

            // 设置锚点在父物体的四个角
            rect.anchorMin = anchorMin; // 左下角 (0,0)
            rect.anchorMax = anchorMax; // 右上角 (1,1)

            // 清除偏移（这样四角就完全贴合父物体）
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var horizontalLayout = imageGO.GetComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childForceExpandHeight = true;
            horizontalLayout.childControlHeight = true;

            this.capacity = capacity;
            recentPrices = new FixedCircleQueue<BinanceWebSocketClient.KlineInfo>(capacity);
            candlePool = new CandleCirclePool(capacity, rect);
            useSilentUpdate = false;
        }

        public void RefreshUI()
        {
            
        }
        public void RebuildCandles(int capacity, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            //Debug.Log("RebuildCandlesRebuildCandlesRebuildCandlesRebuildCandlesRebuildCandles");

            // 安全清空 UI 池
            // if (candlePool != null)
            // {
            //     foreach (var (candle, extremum) in candlePool.GetAll())
            //     {
            //         try
            //         {
            //             if (candle != null)
            //                 UnityEngine.Object.Destroy(candle.gameObject);
            //             if (extremum != null)
            //                 UnityEngine.Object.Destroy(extremum.gameObject);
            //             if (candle != null && candle.transform.parent != null)
            //                 UnityEngine.Object.Destroy(candle.transform.parent.gameObject);
            //         }
            //         catch { /* 忽略 null 异常 */ }
            //     }
            // }
            
            GameObject imageGO = new GameObject("Kline", typeof(RectTransform), typeof(Image),
                typeof(HorizontalLayoutGroup));
            imageGO.transform.SetParent(parent, false); // 设为当前物体的子对象，不改变局部坐标

            var image = imageGO.GetComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);
            
            RectTransform rect = imageGO.GetComponent<RectTransform>();

            // 设置锚点在父物体的四个角
            rect.anchorMin = anchorMin; // 左下角 (0,0)
            rect.anchorMax = anchorMax; // 右上角 (1,1)

            // 清除偏移（这样四角就完全贴合父物体）
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var horizontalLayout = imageGO.GetComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childForceExpandHeight = true;
            horizontalLayout.childControlHeight = true;

            this.capacity = capacity;
            //recentPrices = new FixedCircleQueue<BinanceWebSocketClient.KlineInfo>(capacity);
            //candlePool = new CandleCirclePool(capacity, rect);

            // 重新创建池
            candlePool = new CandleCirclePool(capacity, rect);

            // 重新创建 UI
            for (int i = 0; i < recentPrices.Count; i++)
            {
                var candleUI = candlePool.GetNewCandle();
                UpdateCandleVisual(i);
            }
        }

        public bool UseSilentUpdate
        {
            get
            {
                return useSilentUpdate;
            }
            set
            {
                if (value)
                {
                    
                }

                useSilentUpdate = value;
            }
        }
        public static bool useSilentUpdate = false;
        public void UpdateThis(BinanceWebSocketClient.KlineInfo newPrice)
        {
           // Debug.Log("useSilentUpdate"+useSilentUpdate);
            if (useSilentUpdate||BlackMarketView.Instance==null) SilentUpdate(newPrice);
            else UpdatePriceAndUI(newPrice);
        }
        public void SilentUpdate(BinanceWebSocketClient.KlineInfo newPrice)
        {
            if (newPrice.flag || Count == 0)
            {
                // if (recentPrices.Count > 0 && Math.Abs(recentPrices.PeekLast().Close - newPrice.Open )> 1)
                // {
                //     recentPrices.Enqueue(new BinanceWebSocketClient.KlineInfo()
                //     );
                // }
                recentPrices.Enqueue(newPrice);
            }
            else
            {
                recentPrices.SetLast(newPrice);
            }

            UpdateViewMaxMinPrice(newPrice);
        }

        public void UpdatePriceAndUI(BinanceWebSocketClient.KlineInfo obj)
        {
            if (obj.flag || Count == 0)
            {
                AddPrice(obj);
            }
            else
            {
                UpdateLastPrice(obj);
            }

        }

        /// <summary>
        /// 添加新的价格
        /// </summary>
        public void AddPrice(BinanceWebSocketClient.KlineInfo price)
        {
            recentPrices.Enqueue(price);
            candlePool.GetNewCandle(); // 如果满了会循环使用旧的

            if (UpdateViewMaxMinPrice(price))
            {
                UpdateAllCandles();
            }
            else
            {
                UpdateCandleVisual(recentPrices.Count - 1); // 直接更新最后一个

            }
        }

        /// <summary>
        /// 更新某个索引的 candle
        /// </summary>
        private void UpdateCandleVisual(int index)
        {
            var price = recentPrices[index];
            var candle = candlePool[index]; // 直接索引访问，不用 ToArray 或 ElementAt

            float start = NormalizePrice(price.Open, viewMinPrice, viewMaxPrice);
            float end = NormalizePrice(price.Close, viewMinPrice, viewMaxPrice);
            float min = NormalizePrice(price.Low, viewMinPrice, viewMaxPrice);
            float max = NormalizePrice(price.High, viewMinPrice, viewMaxPrice);

            if (price.Open == price.Close) end = start + 0.01f;

            UpdateCandleUI(start, end, candle.candle, candle.extremum, min, max);
        }

        /// <summary>
        /// 更新全部 candle（例如视窗最大最小值变化时）
        /// </summary>
        public void UpdateAllCandles()
        {
            int count = recentPrices.Count;
            for (int i = 0; i < count; i++)
            {
                UpdateCandleVisual(i);
            }
        }

        public void UpdateLastPrice(BinanceWebSocketClient.KlineInfo newPrice)
        {
            if (recentPrices.Count == 0) return;



            recentPrices.SetLast(newPrice); // 更新队列中最后一个元素
            if (UpdateViewMaxMinPrice(newPrice))
            {
                UpdateAllCandles();
            }
            else
            {
                UpdateCandleVisual(recentPrices.Count - 1); // 更新显示
            }
        }

        private bool UpdateViewMaxMinPrice(BinanceWebSocketClient.KlineInfo obj)
        {
            bool changed = false;

            if (obj.High > viewMaxPrice)
            {
                viewMaxPrice = obj.High;
                changed = true;
            }

            if (obj.Low < viewMinPrice)
            {
                viewMinPrice = obj.Low;
                changed = true;
            }

            return changed;
        }

        void UpdateCandleUI(float start, float end, Image candle1, Image candle2, float min, float max)
        {
            if(candle1==null||candle2==null) return;

            var color = GetCandleColor(start, end);
            UpdateCandle(start, end, candle1, color);
            UpdateCandle(min, max, candle2, color, 0.45f, 0.55f);
        }

        public static Color GetCandleColor(float start, float end)
        {
            return end > start ? upColor : downColor;
        }

        public static void UpdateCandle(float start, float end, Image candleFill, Color color, float anchorMin = 0f,
            float anchorMax = 1f)
        {
            // 把 [-1,1] 映射到 [0,1]（UI 的锚点坐标系）
            float nStart = (start + 1f) * 0.5f;
            float nEnd = (end + 1f) * 0.5f;


            // 判断颜色（上涨绿色，下跌红色）
            // candleFill.color = end > start ? upColor : downColor;
            candleFill.color = color;


            var fillRect = (RectTransform)candleFill.transform;

            // 设定锚点范围（自动处理上下关系）
            fillRect.anchorMin = new Vector2(anchorMin, Mathf.Min(nStart, nEnd));
            fillRect.anchorMax = new Vector2(anchorMax, Mathf.Max(nStart, nEnd));

            // 确保填充铺满锚定区域
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        public static float NormalizePrice(int price, int minPrice, int maxPrice)
        {
            int range = maxPrice - minPrice;
            if (range == 0) return 0f;

            // 用浮点除法映射到 [-1,1]
            return ((price - minPrice) / (float)range) * 2f - 1f;
        }
    }


    public class FixedCircleQueue<T>
    {
        private T[] buffer;
        private int start = 0;
        private int count = 0;

        public int Count => count;
        public int Capacity => buffer.Length;

        public FixedCircleQueue(int capacity)
        {
            if (capacity <= 0) throw new ArgumentException("Capacity must be positive");
            buffer = new T[capacity];
        }

        // 入队
        public void Enqueue(T item)
        {
            if (count < buffer.Length)
            {
                buffer[(start + count) % buffer.Length] = item;
                count++;
            }
            else
            {
                // 覆盖最旧元素
                buffer[start] = item;
                start = (start + 1) % buffer.Length;
            }
        }

        // 出队（移除最旧元素）
        public T Dequeue()
        {
            if (count == 0) throw new InvalidOperationException("Queue is empty");
            T item = buffer[start];
            start = (start + 1) % buffer.Length;
            count--;
            return item;
        }

        // 获取最近入队元素
        public T PeekLast()
        {
            if (count == 0) throw new InvalidOperationException("Queue is empty");
            return buffer[(start + count - 1) % buffer.Length];
        }

        public void SetLast(T value)
        {
            if (count == 0) throw new InvalidOperationException();
            int idx = (start + count - 1) % Capacity;
            buffer[idx] = value;
        }

        // 支持索引访问（0 最旧，Count-1 最近）
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count) throw new IndexOutOfRangeException();
                return buffer[(start + index) % buffer.Length];
            }
            set
            {
                if (index < 0 || index >= count) throw new IndexOutOfRangeException();
                buffer[(start + index) % buffer.Length] = value;
            }
        }


        // 转数组
        public T[] ToArray()
        {
            T[] arr = new T[count];
            for (int i = 0; i < count; i++)
                arr[i] = buffer[(start + i) % buffer.Length];
            return arr;
        }
    }

    public class CandleCirclePool
    {
        private (Image candle, Image extremum)[] pool;
        private int start = 0;
        private int count = 0;
        private int capacity;
        private Transform parent;

        public CandleCirclePool(int capacity, Transform parent)
        {
            this.capacity = capacity;
            this.parent = parent;
            pool = new (Image, Image)[capacity];
        }

        /// <summary>
        /// 获取一个新的 Candle，如果满了就循环使用最旧的
        /// </summary>
        public (Image candle, Image extremum) GetNewCandle()
        {
            (Image candle, Image extremum) item;

            if (count < capacity)
            {
                // 创建新的
                item = CreateCandle();
                pool[(start + count) % capacity] = item;
                count++;
            }
            else
            {
                // 循环使用最旧元素
                item = pool[start];

                // 隐藏旧父物体
                item.candle.transform.parent.gameObject.SetActive(false);

                // 移动 start 指针
                start = (start + 1) % capacity;
            }

            // 激活父物体并移动到最后
            item.candle.transform.parent.gameObject.SetActive(true);
            item.candle.transform.parent.SetAsLastSibling();

            return item;
        }

        private (Image, Image) CreateCandle()
        {
            GameObject go = new GameObject("CandleParent", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta *= new Vector2(0.5f, 1f);

            Image candle = new GameObject("Candle", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            candle.transform.SetParent(go.transform, false);

            Image extremum = new GameObject("Extremum", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            extremum.transform.SetParent(go.transform, false);

            return (candle, extremum);
        }

        public int Count => count;

        /// <summary>
        /// 支持索引访问（0 最旧，Count-1 最近）
        /// </summary>
        public (Image candle, Image extremum) this[int index]
        {
            get
            {
                if (index < 0 || index >= count) throw new IndexOutOfRangeException();
                return pool[(start + index) % capacity];
            }
            set
            {
                if (index < 0 || index >= count) throw new IndexOutOfRangeException();
                pool[(start + index) % capacity] = value;
            }
        }

        /// <summary>
        /// 遍历所有元素，从最旧到最近
        /// </summary>
        public IEnumerable<(Image, Image)> GetAll()
        {
            for (int i = 0; i < count; i++)
                yield return pool[(start + i) % capacity];
        }



    }

}
    

