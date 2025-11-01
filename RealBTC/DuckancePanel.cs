
using Cysharp.Threading.Tasks;
using Duckov.Economy;
using Duckov.UI;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RealBTC.Network;
using RealBTC.UI;
using Shapes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace RealBTC.UI
{
    public class DuckancePanel : MonoBehaviour
    {
       private const int BTC_ID = 388;                     // Bitcoin 物品 ID
        private const float REFRESH_INTERVAL = 5f;          // 价格刷新间隔（秒）
        private const  int feeSell = 50;          

        private TextMeshProUGUI _priceText;
        private TextMeshProUGUI _holdingText;
        private TextMeshProUGUI _buyEntry;
        private TextMeshProUGUI _sellEntry;
        private Button _buyButton;
        private Button _sellButton;
        private TextMeshProUGUI _buyPriceLabel;
        private TextMeshProUGUI _sellPriceLabel;

        private bool _active;

       // private CandleManager candleManager;
        private void OnEnable()
        {
           // BitcoinPriceManager.Instance.StartUpdateLoop();
        }

        private void OnDisable()
        {
            //BitcoinPriceManager.Instance.StopUpdateLoop();
        }

        public void Setup()
        {
            //GameManager
            BuildUI();
            _active = true;
            //RunPriceWatcher().Forget();
        }

        #region UI 构建

        private Slider amountSlider;
        private int maxAmount=10;
        private void BuildUI()
        {
            // ---------- 根容器 ----------
            //var root = //new GameObject("Duckance_Panel", typeof(RectTransform));
            var root = this;
            //root.transform.SetParent(this.transform, false);
            // var rootRect = root.GetComponent<RectTransform>();
            // var rootRect = root.GetComponent<RectTransform>();
            // rootRect.anchorMin = Vector2.zero;
            // rootRect.anchorMax = Vector2.one;
            // rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            // ---------- 标题 ----------
            var title = CreateTMP(root.transform, "Title", 30, TextAlignmentOptions.Center);
            title.text = "Duckance 交易所";
            title.rectTransform.anchoredPosition = new Vector2(0, -60);

            // ---------- 价格 & 持仓 ----------
            _priceText = CreateTMP(root.transform, "Price", 24, TextAlignmentOptions.Center);
            _priceText.rectTransform.anchoredPosition = new Vector2(0, -110);
            
            
            var changeText = "(加载中)";
            var priceColor = new Color(0.8f, 0.8f, 0.8f);
            
            _priceText.text = $"当前 0.2 BTC 价格：加载中 " +
                              $"<color=#{ColorUtility.ToHtmlStringRGB(priceColor)}>{changeText}</color>";
            _priceText.color = priceColor;
            
        

        
            _holdingText = CreateTMP(root.transform, "Holding", 22, TextAlignmentOptions.Center);
            _holdingText.rectTransform.anchoredPosition = new Vector2(0, -145);

            // ---------- 交易条目容器 ----------
            var listRoot = new GameObject("Entries", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listRoot.transform.SetParent(root.transform, false);
            var listRect = listRoot.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 1);
            listRect.anchorMax = new Vector2(1, 1);
            listRect.pivot = new Vector2(0.5f, 1);
            listRect.anchoredPosition = new Vector2(0, -185);
            listRect.sizeDelta = new Vector2(-40, 0);   // 左右留白

            var layout = listRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 128;                             // 上下条目间距加大
            layout.padding = new RectOffset(24, 24, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;

            // ---------- 买卖条目 ----------
            _buyEntry = CreateTradeEntry(listRoot.transform, $"买入 {0.2*tradeAmount} BTC", new Color(0.2f, 0.7f, 1f, 0.85f),
                out _buyButton, out _buyPriceLabel);
            _sellEntry = CreateTradeEntry(listRoot.transform, $"卖出 {0.2*tradeAmount} BTC", new Color(1f, 0.4f, 0.4f, 0.85f),
                out _sellButton, out _sellPriceLabel);

            // ---------- 打开文件夹按钮（调试用） ----------
           // var openBtn = CreateOpenFolderButton(root.transform);
           // openBtn.onClick.AddListener(OpenModFolder);
           _buyButton.onClick.AddListener(()=>ExecuteBuyAsync().Forget());
           _sellButton.onClick.AddListener(()=>ExecuteSell());


          // candleManager = new CandleManager(20, this.transform, new Vector2(0, 0), new Vector2(1, 0.3f));
           //BuildGraph(this.transform);

           CreateAmountPanel(this.transform);

           UpdateThis(-1,0);
           //BitcoinPriceManager.OnPriceUpdate += UpdateThis;
           BinanceWebSocketClient.OnPriceUpdate += UpdateThis;
        }


        private TextMeshProUGUI amountText;
        private TextMeshProUGUI tradeNumText;
        private int tradeAmount = 1; // 默认交易数量

        private void CreateAmountPanel(Transform parent)
        {
             var panelGO = new GameObject("AmountPanel", typeof(RectTransform));
    panelGO.transform.SetParent(parent, false);
    var panelRect = panelGO.GetComponent<RectTransform>();
    panelRect.sizeDelta = new Vector2(300, 40);

    // 左边滑动条
    var sliderGO = new GameObject("AmountSlider", typeof(RectTransform), typeof(Slider), typeof(Image));
    sliderGO.transform.SetParent(panelGO.transform, false);
    var sliderRect = sliderGO.GetComponent<RectTransform>();
    sliderRect.sizeDelta = new Vector2(300, 40);
    sliderRect.anchoredPosition = new Vector2(0, 0);

    var sliderBg = sliderGO.GetComponent<Image>();
    sliderBg.color = new Color(1f, 1f, 1f, 0.2f);

    // 添加填充条
    var fillArea = new GameObject("FillArea", typeof(RectTransform));
    fillArea.transform.SetParent(sliderGO.transform, false);
    var fillAreaRect = fillArea.GetComponent<RectTransform>();
    fillAreaRect.anchorMin = new Vector2(0, 0);
    fillAreaRect.anchorMax = new Vector2(1, 1);
    fillAreaRect.offsetMin = Vector2.zero;
    fillAreaRect.offsetMax = Vector2.zero;

    var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
    fill.transform.SetParent(fillArea.transform, false);
    var fillRect = fill.GetComponent<RectTransform>();
    fillRect.anchorMin = new Vector2(0, 0);
    fillRect.anchorMax = new Vector2(1, 1);
    fillRect.offsetMin = Vector2.zero;
    fillRect.offsetMax = Vector2.zero;

    var fillImage = fill.GetComponent<Image>();
    fillImage.color = new Color(0.4f, 0.8f, 1f, 0.8f);

    amountSlider = sliderGO.GetComponent<Slider>();
    amountSlider.fillRect = fillRect;
    amountSlider.targetGraphic = fillImage;
    amountSlider.minValue = 1;
    amountSlider.maxValue = maxAmount;
    amountSlider.value = tradeAmount;
    amountSlider.transition = Selectable.Transition.None;
    amountSlider.direction = Slider.Direction.LeftToRight;

    
    var textTradeNum = new GameObject("TradeNum", typeof(RectTransform), typeof(TextMeshProUGUI));
    textTradeNum.transform.SetParent(panelGO.transform, false);
    var textTradeNuRect = textTradeNum.GetComponent<RectTransform>();
    textTradeNuRect.sizeDelta = new Vector2(100, 40);
    textTradeNuRect.anchoredPosition = new Vector2(-520, 8); // 原本对齐位置
    tradeNumText=textTradeNuRect.GetComponent<TextMeshProUGUI>();
    // 数字显示
    var textGO = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
    textGO.transform.SetParent(panelGO.transform, false);
    var textRect = textGO.GetComponent<RectTransform>();
    textRect.sizeDelta = new Vector2(100, 40);
    //extRect.anchoredPosition = new Vector2(80, 0); // 原本对齐位置

    amountText = textGO.GetComponent<TextMeshProUGUI>();
    amountText.fontSize = 24;
    amountText.alignment = TextAlignmentOptions.Center;
    amountText.text = tradeAmount.ToString();
    amountText.raycastTarget = false;

    // 滑动条变化时更新数字
    amountSlider.onValueChanged.AddListener(val =>
    {
        tradeAmount = Mathf.RoundToInt(val);
        amountText.text = tradeAmount.ToString();
    });
          

    

    // x1, x10, x100 按钮
    //CreateMultiplierButton(panelGO.transform, "x1", 1, new Vector2(120 + 150, 0));
    CreateMultiplierButton(panelGO.transform, "x10", 10, new Vector2(180 + 200, 0),new Vector2(50, 40));
    CreateMultiplierButton(panelGO.transform, "x50", 50, new Vector2(240 + 200, 0),new Vector2(50, 40));
    CreateMultiplierButton(panelGO.transform, "x100", 100, new Vector2(300 + 200, 0),new Vector2(50, 40));
    CreateMultiplierButton(panelGO.transform, "x1000", 1000, new Vector2(370 + 200, 0),new Vector2(70, 40));
        }

        private void CreateMultiplierButton(Transform parent, string label, int multiplier, Vector2 pos,Vector2 size)
        {
            var btnGO = new GameObject(label, typeof(RectTransform), typeof(Button), typeof(Image));
            btnGO.transform.SetParent(parent, false);

            var rect = btnGO.GetComponent<RectTransform>();
            //rect.sizeDelta = new Vector2(50, 40);
            rect.sizeDelta = size;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.anchoredPosition = pos;

            var img = btnGO.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0.2f);
            img.raycastTarget = true;

            // 文本
            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(btnGO.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            var txtRect = textGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            var btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                tradeAmount = multiplier;
                amountText.text = tradeAmount.ToString();
                maxAmount = multiplier;
                amountSlider.maxValue = maxAmount;
                amountSlider.value = tradeAmount;
            });
        }
        private void UpdateMaxAmount()
        {
            return;
            maxAmount = Math.Max(ItemUtilities.GetItemCount(BTC_ID),10);
            if (amountSlider != null)
            {
                amountSlider.maxValue = maxAmount;
                if (tradeAmount > maxAmount)
                {
                    tradeAmount = maxAmount;
                    amountSlider.value = tradeAmount;
                    amountText.text = tradeAmount.ToString();
                }
            }
        }



        private TextMeshProUGUI CreateTradeEntry(Transform parent, string title, Color bgColor,
            out Button button, out TextMeshProUGUI priceLabel)
        {
            // 卡片背景
            var go = new GameObject("Entry", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bgColor;
            img.type = Image.Type.Sliced;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 110);   // 高度适中

            // BTC 图标
            var icon = NewImage(go.transform, "Icon");
            icon.rectTransform.sizeDelta = new Vector2(70, 70);
            icon.rectTransform.anchoredPosition = new Vector2(-rect.rect.width * 0.5f + 55, 0);
            var meta = ItemAssetsCollection.GetMetaData(BTC_ID);
            icon.sprite = GetItemIconSprite(meta);
            icon.enabled = icon.sprite != null;

            // 标题
            var titleTmp = CreateTMP(go.transform, "Title", 24, TextAlignmentOptions.Left);
            titleTmp.text = title;
            titleTmp.rectTransform.anchoredPosition = new Vector2(10, 20);

            // 价格文字
            priceLabel = CreateTMP(go.transform, "Price", 20, TextAlignmentOptions.Left);
            priceLabel.rectTransform.anchoredPosition = new Vector2(+10, -12);
            priceLabel.color = new Color(0.9f, 0.9f, 0.7f);

            // 按钮（右侧大按钮）
            var btnObj = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(go.transform, false);
            var btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(1f, 1f, 1f, 0.2f);
            button = btnObj.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            var btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-30, 0);
            btnRect.sizeDelta = new Vector2(130, 50);   // 按钮更大

            var btnText = CreateTMP(btnObj.transform, "Text", 22, TextAlignmentOptions.Center);
            btnText.text = title.Contains("买入") ? "买入" : "卖出";
            btnText.rectTransform.anchorMin = Vector2.zero;
            btnText.rectTransform.anchorMax = Vector2.one;

            return titleTmp;
        }

        private Button CreateOpenFolderButton(Transform parent)
        {
            
            var go = new GameObject("OpenFolder", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.7f, 0.85f, 1f, 0.8f);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-16, -60);
            rect.sizeDelta = new Vector2(180, 36);

           // var txt = CreateTMP(go.transform, "Label", 18, TextAlignmentOptions.Center, new Color(0.1f, 0.2f, 0.3f));
            //txt.text = "打开配置文件夹";

            return go.GetComponent<Button>();
        }

        private void OpenModFolder()
        {
            try
            {
                var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                Process.Start("explorer.exe", dir);
                NotificationText.Push("已打开 RealBTC 文件夹");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealBTC] 打开文件夹失败: {ex}");
                NotificationText.Push("打开失败，请手动前往 mod 目录");
            }
        }

        #endregion

        #region 价格刷新 & 按钮交互

        private async UniTaskVoid RunPriceWatcher()
        {
            while (_active && gameObject != null)
            {
                RefreshAll();
                await UniTask.Delay(TimeSpan.FromSeconds(REFRESH_INTERVAL));
            }
        }

        private void UpdateThis(BinanceWebSocketClient.KlineInfo obj)
        {
            //Debug.Log("UpdateThis");
            CandleManager.Instance.UpdateThis(obj);
            //candleManager.UpdatePrice(obj);
            UpdateThis(obj.Close/5,((float)(obj.Close - obj.Open) / obj.Open)*100);
            tradeNumText.text = $"当前一分钟交易量{obj.Trades}";

        }

        public void UpdateThis(int rawPrice,float change)
        {
            //Debug.Log(rawPrice);

            //int rawPrice = BitcoinPriceManager.CurrentBitcoinPriceDivideBy5; // 0.2 BTC 价格
            int holding = ItemUtilities.GetItemCount(BTC_ID);

            bool priceValid = rawPrice > 0;
            string priceStr = priceValid ? $"${rawPrice:N0}" : "获取中…";

           // int finalSell = rawPrice*tradeAmount - feeSell;
            //string finalSellStr = $"${finalSell:N0}";
            
            // === 计算涨跌幅 + 颜色 ===
            string changeText = "";
            Color priceColor = Color.white;

            if (priceValid )
            {
                //float change = BitcoinPriceManager.Growth;
                if (Mathf.Abs(change) > 0.0001f) // 避免微小浮动
                {
                    changeText = change > 0 ? $"(+{change:F4}%)" : $"({change:F4}%)";
                    priceColor = change > 0 ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f); // 绿/红
                }
            }
            else if (!priceValid)
            {
                changeText = "(加载中)";
                priceColor = new Color(0.8f, 0.8f, 0.8f);
            }

            // === 更新 UI ===
            _priceText.text = $"当前 0.2 BTC 价格：{priceStr} " +
                              $"<color=#{ColorUtility.ToHtmlStringRGB(priceColor)}>{changeText}</color>";
            _priceText.color = priceColor;

           
            
            

            
            
            // if (rawPrice > 0)
            // {
            //     if (priceHistory.Count >= MAX_POINTS)
            //         priceHistory.RemoveAt(0);
            //     priceHistory.Add(rawPrice);
            //
            //     maxPrice = Mathf.Max(priceHistory.ToArray());
            //     minPrice = Mathf.Min(priceHistory.ToArray());
            //
            //     DrawPriceGraph();
            // }
        }

        private void Update()
        {
           // bool canBuy = BinanceWebSocketClient.CurrentPriceDivideBy5>0 && EconomyManager.Money >= BinanceWebSocketClient.CurrentPriceDivideBy5*tradeAmount;
           // bool canSell =  BinanceWebSocketClient.CurrentPriceDivideBy5>0 && ItemUtilities.GetItemCount(BTC_ID) > tradeAmount-1;

            _buyButton.interactable = CanBuy();
            _sellButton.interactable = CanSell();
            _holdingText.text = $"持有数量：{ItemUtilities.GetItemCount(BTC_ID)} 枚";
            UpdateMaxAmount();
            
            var rawPrice = BinanceWebSocketClient.CurrentPriceDivideBy5;
            bool priceValid = rawPrice > 0;
            string priceStr = priceValid ? $"${rawPrice*tradeAmount:N0}" : "获取中…";

            int finalSell = rawPrice*tradeAmount - feeSell;
            string finalSellStr = $"${finalSell:N0}";
            
            _buyPriceLabel.text = priceValid ? $"需支付：{priceStr}" : "——";
            _sellPriceLabel.text = priceValid ? $"可获得：{finalSellStr}（含50手续费）" : "——";

            _buyEntry.text = $"买入 {0.2 * tradeAmount} BTC";
            _sellEntry.text = $"卖出 {0.2*tradeAmount} BTC";
        }

        private void RefreshAll()
        {
            return;
           // Debug.Log("刷新UI");
            try
            {
                int rawPrice = BitcoinPriceManager.CurrentBitcoinPriceDivideBy5; // 0.2 BTC 价格
                int holding = ItemUtilities.GetItemCount(BTC_ID);

                bool priceValid = rawPrice > 0;
                string priceStr = priceValid ? $"${rawPrice:N0}" : "获取中…";

                // === 计算涨跌幅 + 颜色 ===
                string changeText = "";
                Color priceColor = Color.white;

                if (priceValid )
                {
                    float change = BitcoinPriceManager.Growth;
                    if (Mathf.Abs(change) > 0.01f) // 避免微小浮动
                    {
                        changeText = change > 0 ? $"(+{change:F4}%)" : $"({change:F4}%)";
                        priceColor = change > 0 ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f); // 绿/红
                    }
                }
                else if (!priceValid)
                {
                    changeText = "(加载中)";
                    priceColor = new Color(0.8f, 0.8f, 0.8f);
                }

                // === 更新 UI ===
                _priceText.text = $"当前 0.2 BTC 价格：{priceStr} " +
                                  $"<color=#{ColorUtility.ToHtmlStringRGB(priceColor)}>{changeText}</color>";
                _priceText.color = priceColor;

                _holdingText.text = $"持有数量：{holding} 枚";

                _buyPriceLabel.text = priceValid ? $"需支付：{priceStr}" : "——";
                _sellPriceLabel.text = priceValid ? $"可获得：{rawPrice-feeSell}（含50手续费）" : "——";

                bool canBuy = priceValid && EconomyManager.Money >= rawPrice;
                bool canSell = priceValid && holding > 0;

                //SetButton(_buyButton, canBuy, () => ExecuteBuy(rawPrice));
                //SetButton(_sellButton, canSell, () => ExecuteSell(rawPrice));

               
            }
            catch (Exception ex)
            {
                _priceText.text = "价格获取失败";
                _priceText.color = Color.white;
                Debug.LogError($"[RealBTC] RefreshAll error: {ex}");
            }
        }

        private void SetButton(Button btn, bool interactable, UnityAction action)
        {
            btn.interactable = interactable;
            btn.onClick.RemoveAllListeners();
            if (interactable) btn.onClick.AddListener(action);
        }

        bool CanBuy()
        {
           return BinanceWebSocketClient.CurrentPriceDivideBy5>0&&tradeAmount>0&&EconomyManager.Money >=BinanceWebSocketClient.CurrentPriceDivideBy5*tradeAmount;
        }

        bool CanSell()
        {
            return BinanceWebSocketClient.CurrentPriceDivideBy5>0&&tradeAmount>0&&ItemUtilities.GetItemCount(BTC_ID) >= tradeAmount;
        }
        private void ExecuteBuy()
        {
            long cost = BinanceWebSocketClient.CurrentPriceDivideBy5*tradeAmount;
            if (!CanBuy())
            {
                NotificationText.Push("现金不足，无法买入 BTC");
                return;
            }

            EconomyManager.Pay(new Cost((long)cost), true, true);
            for (int i = 0; i < tradeAmount; i++)
            {
                var item = ItemAssetsCollection.InstantiateSync(BTC_ID);
                ItemUtilities.SendToPlayer(item, true);
            }
            NotificationText.Push($"买入成功！花费 ${cost:N0} 获得 {tradeAmount} BTC");
            //RefreshAll();
        }
        private async UniTask ExecuteBuyAsync()
        {
            long cost = BinanceWebSocketClient.CurrentPriceDivideBy5 * tradeAmount;
            if (!CanBuy())
            {
                NotificationText.Push("现金不足，无法买入 BTC");
                return;
            }

            EconomyManager.Pay(new Cost(cost), true, true);

            // 并行生成所有 BTC
            var tasks = new UniTask<Item>[tradeAmount];
            for (int i = 0; i < tradeAmount; i++)
                tasks[i] = ItemAssetsCollection.InstantiateAsync(BTC_ID);

            Item[] items = await UniTask.WhenAll(tasks);

            foreach (var item in items)
                ItemUtilities.SendToPlayer(item, true);

            NotificationText.Push($"买入成功！花费 ${cost:N0} 获得 {tradeAmount} BTC");
        }

        private void ExecuteSell()
        {
            long income = BinanceWebSocketClient.CurrentPriceDivideBy5*tradeAmount - feeSell;
            if (!CanSell())
            {
                NotificationText.Push(" BTC 不足");
                return;
            }

            // 扣除 1 个 BTC
            if (!new Cost(0L, new[] { (388, (long)tradeAmount) }).Pay(false, false))
            {
                NotificationText.Push("扣除 BTC 失败");
                return;
            }

            EconomyManager.Add((long)income*tradeAmount);
            NotificationText.Push($"卖出成功！获得 ${income:N0}(含50手续费)");
           // RefreshAll();
        }

        #endregion

        #region 工具方法

        private TextMeshProUGUI CreateTMP(Transform parent, string name, int fontSize,
            TextAlignmentOptions align, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color ?? Color.white;
            tmp.raycastTarget = false;

            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(0, 30);
            return tmp;
        }

        private static Image NewImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

        private static Sprite GetItemIconSprite(ItemMetaData meta)
        {
            var t = meta.GetType();
            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (p.PropertyType == typeof(Sprite) && p.GetValue(meta) is Sprite s && s) return s;
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (f.FieldType == typeof(Sprite) && f.GetValue(meta) is Sprite s && s) return s;
            return null;
        }

        #endregion

       

        private void OnDestroy()
        {
            
            
            BinanceWebSocketClient.OnPriceUpdate -= UpdateThis;
            CandleManager.useSilentUpdate=true;

                //BitcoinPriceManager.OnPriceUpdate -= UpdateThis;
            
        }
        //private const int MAX_POINTS = 60; // 显示最近 60 个价格
        //private List<float> priceHistory = new List<float>();

        private LineRenderer priceLine;
        private float graphWidth = 600f;
        private float graphHeight = 120f;
        private Vector2 graphOffset = new Vector2(0, -220f); // 曲线相对价格文字偏移
        private float maxPrice = 0;
        private float minPrice = 0;
        
        private void BuildGraph(Transform parent)
        {
            var lrObj = new GameObject("PriceGraph");
            lrObj.transform.SetParent(parent, false);

            // 如果是 Canvas UI，最好用 WorldSpace Canvas 或者把 RectTransform 转换成位置
            priceLine = lrObj.AddComponent<LineRenderer>();
            priceLine.positionCount = 0;
            priceLine.material = new Material(Shader.Find("Sprites/Default"));
            priceLine.widthCurve = AnimationCurve.Constant(0, 1, 3f);
            priceLine.useWorldSpace = false;
            priceLine.numCapVertices = 5;
            priceLine.startColor = priceLine.endColor = new Color(0.2f, 0.8f, 1f);
        }
        
        // private void DrawPriceGraph()
        // {
        //     if (priceLine == null || priceHistory.Count < 2) return;
        //
        //     priceLine.positionCount = priceHistory.Count;
        //     float xStep = graphWidth / (MAX_POINTS - 1);
        //
        //     for (int i = 0; i < priceHistory.Count; i++)
        //     {
        //         float norm = (maxPrice == minPrice) ? 0.5f : Mathf.InverseLerp(minPrice, maxPrice, priceHistory[i]);
        //         Vector3 pos = new Vector3(i * xStep, norm * graphHeight, 0) + (Vector3)graphOffset;
        //         priceLine.SetPosition(i, pos);
        //     }
        // }
        

    }
    }
    

