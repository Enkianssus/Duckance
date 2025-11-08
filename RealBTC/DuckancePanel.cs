
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
using RealBTC.Data;
using RealBTC.Network;
using RealBTC.UI;
using Shapes;
using SodaCraft.Localizations;
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
        private TextMeshProUGUI _buyButtonText;
        private TextMeshProUGUI _sellButtonText;
        private TextMeshProUGUI _buyPriceLabel;
        private TextMeshProUGUI _sellPriceLabel;
        private TextMeshProUGUI _versionLabel;
        private TextMeshProUGUI depositBtnLabel;
        private TextMeshProUGUI depositBtnAllLabel;
        private TextMeshProUGUI withdrawBtnLabel;
        public TextMeshProUGUI tab;

        private Image _connectionIndicator;
        private TMP_Text _connectionLabel;
        
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
        private int maxAmount=50;
        private void BuildUI()
        {
            var root = this;

            
            // ---------- 连接状态指示 ----------
            var indicatorGO = new GameObject("ConnectionIndicator", typeof(RectTransform), typeof(Image));
            indicatorGO.transform.SetParent(root.transform, false);
            var indicatorRect = indicatorGO.GetComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0, 1);
            indicatorRect.anchorMax = new Vector2(0, 1);
            indicatorRect.pivot = new Vector2(0, 1);
            indicatorRect.anchoredPosition = new Vector2(20, -20);
            indicatorRect.sizeDelta = new Vector2(20, 20);

            _connectionIndicator = indicatorGO.GetComponent<Image>();
            _connectionIndicator.color = new Color(1f, 0.3f, 0.3f); // 默认红色（未连接）

            // 文字
            _connectionLabel = CreateTMP(root.transform, "ConnectionLabel", 20, TextAlignmentOptions.Left);
            _connectionLabel.rectTransform.anchorMin = new Vector2(0, 1);
            _connectionLabel.rectTransform.anchorMax = new Vector2(0, 1);
            _connectionLabel.rectTransform.pivot = new Vector2(0, 1);
            _connectionLabel.rectTransform.anchoredPosition = new Vector2(50, -12);
            _connectionLabel.text = "未连接";
            _connectionLabel.color = Color.white;
            
            // ---------- 版本状态指示 ----------
            _versionLabel = CreateTMP(root.transform, "VersionLabel", 18, TextAlignmentOptions.Right);
            _versionLabel.rectTransform.anchorMin = new Vector2(1, 1);
            _versionLabel.rectTransform.anchorMax = new Vector2(1, 1);
            _versionLabel.rectTransform.pivot = new Vector2(1, 1);
            _versionLabel.rectTransform.anchoredPosition = new Vector2(-50, -12); // 在连接状态右侧
            _versionLabel.text = "版本：加载中…";
            _versionLabel.color = Color.gray;

            var textTradeNum = new GameObject("TradeNum", typeof(RectTransform), typeof(TextMeshProUGUI));
            textTradeNum.transform.SetParent(root.transform, false);
            var textTradeNuRect = textTradeNum.GetComponent<RectTransform>();
            textTradeNuRect.sizeDelta = new Vector2(70, 25);
            textTradeNuRect.anchoredPosition = new Vector2(-550, 400); // 原本对齐位置
            tradeNumText=textTradeNuRect.GetComponent<TextMeshProUGUI>();
            
            // 启动异步检查
            CheckVersionAsync().Forget();
            
            // ---------- 根容器 ----------
            //var root = //new GameObject("Duckance_Panel", typeof(RectTransform));
            //root.transform.SetParent(this.transform, false);
            // var rootRect = root.GetComponent<RectTransform>();
            // var rootRect = root.GetComponent<RectTransform>();
            // rootRect.anchorMin = Vector2.zero;
            // rootRect.anchorMax = Vector2.one;
            // rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            // ---------- 标题 ----------
            var title = CreateTMP(root.transform, "Title", 30, TextAlignmentOptions.Center);
            title.text = IsChinese?"Duckance 交易所":"Duckance";
            title.rectTransform.anchoredPosition = new Vector2(0, 420);

            // ---------- 价格 & 持仓 ----------
            _priceText = CreateTMP(root.transform, "Price", 24, TextAlignmentOptions.Center);
            _priceText.rectTransform.anchoredPosition = new Vector2(0, 80);
            
            
            var changeText = IsChinese?"(加载中)":"loading";
            var priceColor = new Color(0.8f, 0.8f, 0.8f);
            
            _priceText.text = IsChinese? $"当前BTC 价格加载中 " +
                              $"<color=#{ColorUtility.ToHtmlStringRGB(priceColor)}>{changeText}</color>":
                $"Current 0.2 BTC Price: loading " +
                $"<color=#{ColorUtility.ToHtmlStringRGB(priceColor)}>{changeText}</color>";;
           
            
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
                out _buyButton, out _buyPriceLabel,out _buyButtonText);
            _sellEntry = CreateTradeEntry(listRoot.transform, $"卖出 {0.2*tradeAmount} BTC", new Color(1f, 0.4f, 0.4f, 0.85f),
                out _sellButton, out _sellPriceLabel,out _sellButtonText);

            // ---------- 打开文件夹按钮（调试用） ----------
           // var openBtn = CreateOpenFolderButton(root.transform);
           // openBtn.onClick.AddListener(OpenModFolder);
           _buyButton.onClick.AddListener(()=>ExecuteBuy());
           _sellButton.onClick.AddListener(()=>ExecuteSell());


          // candleManager = new CandleManager(20, this.transform, new Vector2(0, 0), new Vector2(1, 0.3f));
           //BuildGraph(this.transform);
           // ---------- 资金与现货显示 ----------
var panelGO = new GameObject("HoldingPanel", typeof(RectTransform));
panelGO.transform.SetParent(root.transform, false);
var panelRect = panelGO.GetComponent<RectTransform>();
panelRect.anchorMin = new Vector2(0.5f, 1);
panelRect.anchorMax = new Vector2(0.5f, 1);
panelRect.pivot = new Vector2(0.5f, 1);
panelRect.anchoredPosition = new Vector2(0, -545);
panelRect.sizeDelta = new Vector2(600, 50);

// === 文字部分 ===
_holdingText = CreateTMP(panelGO.transform, "HoldingText", 22, TextAlignmentOptions.Center);
_holdingText.rectTransform.anchorMin = new Vector2(0, 0.5f);
_holdingText.rectTransform.anchorMax = new Vector2(1, 0.5f);
_holdingText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
_holdingText.rectTransform.anchoredPosition = new Vector2(0, 0);
_holdingText.text = "账户余额：加载中… | 背包持仓：加载中…";

// === 存入按钮 ===
var depositBtn = CreateUIButton(panelGO.transform, "DepositButton", "存入", new Vector2(-60+60, -50),out depositBtnLabel);
depositBtn.onClick.AddListener(() =>
{
    double walletBTC = BtcBalanceManager.Balance; // 当前账户 BTC 资金
    double inventoryBTC = ItemUtilities.GetItemCount(BTC_ID); // 玩家持有现货
    double amount = Math.Min(tradeAmount, inventoryBTC); // 不能超过现货
    if (amount <= 0)
    {
        Debug.Log("[RealBTC] 没有足够的现货存入。");
        NotificationText.Push(IsChinese?"存入 BTC 失败":"Failed to deposit BTC.");
        return;
    }
    
    Debug.Log($"[RealBTC] 存入 {amount*0.2d} BTC");
    if (!new Cost(0L, new[] { (388, (long)amount) }).Pay(false, false))
    {
        NotificationText.Push(IsChinese?"存入 BTC 失败":"Failed to deposit BTC.");
        return;
    }
    BtcBalanceManager.AddBalance(amount*0.2d);
    //EconomyManager.Add((long)income);
    NotificationText.Push(IsChinese?$"成功存入 {amount*0.2d:F1}btc":$" successfully deposit{amount*0.2d:F1}btc ");
});

var depositBtnAll = CreateUIButton(panelGO.transform, "DepositButton", "存入所有", new Vector2(-180+60, -50),out depositBtnAllLabel);
depositBtnAll.onClick.AddListener(() =>
{
    double walletBTC = BtcBalanceManager.Balance; // 当前账户 BTC 资金
    double inventoryBTC = ItemUtilities.GetItemCount(BTC_ID); // 玩家持有现货
    double amount = inventoryBTC; // 不能超过现货
    if (amount <= 0)
    {
        Debug.Log("[RealBTC] 没有足够的现货存入。");
        NotificationText.Push(IsChinese?"存入 BTC 失败":"Failed to deposit BTC.");
        return;
    }
    
    Debug.Log($"[RealBTC] 存入 {amount*0.2d} BTC");
    if (!new Cost(0L, new[] { (388, (long)amount) }).Pay(false, false))
    {
        NotificationText.Push(IsChinese?"存入 BTC 失败":"Failed to deposit BTC.");
        return;
    }
    BtcBalanceManager.AddBalance(inventoryBTC*0.2d);
    //EconomyManager.Add((long)income);
    NotificationText.Push(IsChinese?$"成功存入 {amount*0.2d:F1}btc":$" successfully deposit{amount*0.2d:F1}btc ");
});

// === 取出按钮 ===
var withdrawBtn = CreateUIButton(panelGO.transform, "WithdrawButton", "取出", new Vector2(60+60, -50),out withdrawBtnLabel);
withdrawBtn.onClick.AddListener(() =>
{
    ExecuteWithdrawAsync().Forget();
    
});


           CreateAmountPanel(this.transform);

           LazyUpdate(LocalizationManager.CurrentLanguage);
           UpdateThis(-1,0);
           //BitcoinPriceManager.OnPriceUpdate += UpdateThis;
           BinanceWebSocketClient.OnPriceUpdate += UpdateThis;
           LocalizationManager.OnSetLanguage += LazyUpdate;
        }

        private Button CreateUIButton(Transform parent, string name, string text, Vector2 pos,out TextMeshProUGUI label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 36);
            rect.anchoredPosition = pos;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            var btn = go.GetComponent<Button>();

            label = CreateTMP(go.transform, name + "_Label", 20, TextAlignmentOptions.Center);
            label.text = text;
            label.rectTransform.anchoredPosition = Vector2.zero;

            return btn;
        }
        private void LazyUpdate(SystemLanguage obj)
        {
            
            UpdateVersionLabel();
            


            withdrawBtnLabel.text = IsChinese ? "取出" : "Withdraw";
            depositBtnLabel.text = IsChinese ? "存入" : "Deposit";
            depositBtnAllLabel.text = IsChinese ? "存入所有" : "Deposit All";
            balanceLabel.text = IsChinese ? "所有资金" : "Max Balance";
            //tab.text=IsChinese?"鸭安Duckance":"Duckance";
        }

        void UpdateVersionLabel()
        {
            switch (VersionChecker.Status)
            {
                case VersionChecker.VersionStatus.UpToDate:
                    _versionLabel.text = IsChinese
                        ? $"版本：{VersionChecker.CurrentVersion}（最新）"
                        : $"Version: {VersionChecker.CurrentVersion} (Up to date)";
                    _versionLabel.color = new Color(0.3f, 1f, 0.3f); // 绿色
                    break;

                case VersionChecker.VersionStatus.Outdated:
                    _versionLabel.text = IsChinese
                        ? $"版本：{VersionChecker.CurrentVersion}（有更新：{VersionChecker.LatestVersion}）"
                        : $"Version: {VersionChecker.CurrentVersion} (Update available: {VersionChecker.LatestVersion})";
                    _versionLabel.color = new Color(1f, 0.8f, 0.3f); // 黄色
                    break;

                case VersionChecker.VersionStatus.FetchFailed:
                    _versionLabel.text = IsChinese
                        ? $"版本：{VersionChecker.CurrentVersion}（获取失败）"
                        : $"Version: {VersionChecker.CurrentVersion} (Fetch failed)";
                    _versionLabel.color = new Color(1f, 0.4f, 0.4f); // 红色
                    break;

                default:
                    _versionLabel.text = IsChinese
                        ? $"版本：{VersionChecker.CurrentVersion}"
                        : $"Version: {VersionChecker.CurrentVersion}";
                    _versionLabel.color = Color.white;
                    break;
            }
        }

        private async UniTaskVoid CheckVersionAsync()
        {
            await VersionChecker.FetchVersionAsync();

            UpdateVersionLabel();
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

    
    
    // 数字显示
    var textGO = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
    textGO.transform.SetParent(panelGO.transform, false);
    var textRect = textGO.GetComponent<RectTransform>();
    textRect.sizeDelta = new Vector2(100, 40);
    //extRect.anchoredPosition = new Vector2(80, 0); // 原本对齐位置

    amountText = textGO.GetComponent<TextMeshProUGUI>();
    amountText.fontSize = 24;
    amountText.alignment = TextAlignmentOptions.Center;
    amountText.text =  $"{tradeAmount*0.2d:F1}";;
    amountText.raycastTarget = false;

    // 滑动条变化时更新数字
    amountSlider.onValueChanged.AddListener(val =>
    {
        tradeAmount = Mathf.RoundToInt(val);
        amountText.text =  $"{tradeAmount*0.2d:F1}";;
    });
          

    

    // x1, x10, x100 按钮
    //CreateMultiplierButton(panelGO.transform, "x1", 1, new Vector2(120 + 150, 0));
    CreateMultiplierButton(panelGO.transform, "x5", 5*5, new Vector2(180 + 200, 0),new Vector2(50, 40));
    CreateMultiplierButton(panelGO.transform, "x10", 10*5, new Vector2(240 + 200, 0),new Vector2(50, 40));
    CreateMultiplierButton(panelGO.transform, "x100", 100*5, new Vector2(300 + 200, 0),new Vector2(50, 40));
    CreateMultiplierButton(panelGO.transform, "x1000", 1000*5, new Vector2(370 + 200, 0),new Vector2(70, 40));
    CreateMultiplierButton(panelGO.transform, "x10000", 10000*5, new Vector2(470 + 200, 0),new Vector2(90, 40));

    CreateMultiplierButton(panelGO.transform, IsChinese?"所有资金":"Max Balance",
        () => { return Math.Max((int)(BtcBalanceManager.Balance / 0.2d),1);}, new Vector2(-120, 0),new Vector2(130, 40),out balanceLabel);
   // CreateMultiplierButton(panelGO.transform, "Holdings", BtcBalanceManager.InventoryBtcCount, new Vector2(470 + 200, 0),new Vector2(90, 40),out holdingsLabel);

        }

        private TextMeshProUGUI balanceLabel;
        private TextMeshProUGUI holdingsLabel;

        private void CreateMultiplierButton(Transform parent, string label, Func<int> multiplier, Vector2 pos,Vector2 size,out TextMeshProUGUI text)
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
            text = textGO.AddComponent<TextMeshProUGUI>();
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
                tradeAmount = multiplier.Invoke();
                amountText.text =  $"{tradeAmount*0.2d:F1}";
                maxAmount = multiplier.Invoke();
                amountSlider.maxValue = Math.Max(tradeAmount,50) ;
                amountSlider.value = tradeAmount;
            });
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
                amountText.text =  $"{tradeAmount*0.2d:F1}";
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
            out Button button, out TextMeshProUGUI priceLabel,out TextMeshProUGUI buttonText)
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

            buttonText = btnText;

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
        

        private void UpdateThis(BinanceWebSocketClient.KlineInfo obj)
        {
            //Debug.Log("UpdateThis");
            CandleManager.Instance.UpdateThis(obj);
            //candleManager.UpdatePrice(obj);
            UpdateThis(obj.Close,((float)(obj.Close - obj.Open) / obj.Open)*100);
            
            
            if (BlackMarketViewExtensionHelper.IsChinese())
            {
                tradeNumText.text = $"当前一分钟交易量{obj.Trades}";


            }
            else
            {
                tradeNumText.text = $"1min Trade Volume : {obj.Trades}";
                tradeNumText.fontSize = 30;
                

            }

        }

        public void UpdateThis(int rawPrice,float change)
        {
            //Debug.Log(rawPrice);

            //int rawPrice = BitcoinPriceManager.CurrentBitcoinPriceDivideBy5; // 0.2 BTC 价格
           // int holding = ItemUtilities.GetItemCount(BTC_ID);

           if (BlackMarketViewExtensionHelper.IsChinese())
           {
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
               // _priceText.text = $"当前 0.2 BTC 价格：{priceStr} " +
               //                   $"<color=#{ColorUtility.ToHtmlStringRGB(priceColor)}>{changeText}</color>";
               _priceText.text =
                   $"当前 1 BTC 价格：{priceStr}  |  0.2 BTC：{(priceValid ? $"${rawPrice * 0.2:N0}" : "加载中")} " +
                   $"<color=#{ColorUtility.ToHtmlStringRGB(priceColor)}>{changeText}</color>";
                   
               _priceText.color = priceColor;
           }

           else
           {
               bool priceValid = rawPrice > 0;
               string priceStr = priceValid ? $"${rawPrice:N0}" : "Loading…";

// === Calculate change percentage + color ===
               string changeText = "";
               Color priceColor = Color.white;

               if (priceValid)
               {
                   // float change = BitcoinPriceManager.Growth;
                   if (Mathf.Abs(change) > 0.0001f) // Avoid tiny fluctuations
                   {
                       changeText = change > 0 ? $"(+{change:F4}%)" : $"({change:F4}%)";
                       priceColor = change > 0 ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f); // green/red
                   }
               }
               else if (!priceValid)
               {
                   changeText = "(Loading)";
                   priceColor = new Color(0.8f, 0.8f, 0.8f);
               }

// === Update UI ===
               _priceText.text =  $"Current 1 BTC Price: {priceStr}  |  0.2 BTC: {(priceValid ? $"${rawPrice * 0.2:N0}" : "loading")} " +
                                  $"<color=#{ColorUtility.ToHtmlStringRGB(priceColor)}>{changeText}</color>";
               _priceText.color = priceColor;
           }
            

           
            
            

            
            
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

           _connectionIndicator.color =
               BinanceWebSocketClient.IsConnected ? CandleManager.upColor : CandleManager.downColor;
           
            _buyButton.interactable = CanBuy();
            _sellButton.interactable = CanSell();
            if (BlackMarketViewExtensionHelper.IsChinese())
            {
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
                _buyButtonText.text = "买入";
                _sellButtonText.text = "卖出";
                
                _connectionLabel.text =  BinanceWebSocketClient.IsConnected ?"已连接":"未连接";

                if (BinanceWebSocketClient.isUS)
                {
                    _connectionLabel.text += "  检测到美国ip 连接可能不稳定";
                }


            }
            else
            {
                _holdingText.text = $"Holding: {ItemUtilities.GetItemCount(BTC_ID)} pcs";
                UpdateMaxAmount();

                var rawPrice = BinanceWebSocketClient.CurrentPriceDivideBy5;
                bool priceValid = rawPrice > 0;
                string priceStr = priceValid ? $"${rawPrice * tradeAmount:N0}" : "Loading…";

                int finalSell = rawPrice * tradeAmount - feeSell;
                string finalSellStr = $"${finalSell:N0}";

                _buyPriceLabel.text = priceValid ? $"Cost: {priceStr}" : "——";
                _sellPriceLabel.text = priceValid ? $"Receive: {finalSellStr} (after 50 fee)" : "——";

                _buyEntry.text = $"Buy {0.2 * tradeAmount} BTC";
                _sellEntry.text = $"Sell {0.2 * tradeAmount} BTC";
                
                _buyButtonText.text = "buy";
                _sellButtonText.text = "sell";
                
                _connectionLabel.text =  BinanceWebSocketClient.IsConnected ?"Connected":"Disconnect";
                
                if (BinanceWebSocketClient.isUS)
                {
                    _connectionLabel.text += "  U.S. IP detected. connection may be unstable";
                }
            }
            
            double funds = BtcBalanceManager.Balance;
            double inventory = ItemUtilities.GetItemCount(BTC_ID);
            _holdingText.text = IsChinese
                ? $"账户资金：{funds:F2} BTC | 现货持有：{inventory * 0.2:F2} BTC"
                : $"Account Balance: {funds:F2} BTC | Spot Holdings: {inventory * 0.2:F2} BTC";
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
            return BinanceWebSocketClient.CurrentPriceDivideBy5>0&&tradeAmount>0&&BtcBalanceManager.Balance >= tradeAmount*0.2d;
        }
        private void ExecuteBuy()
        {
            long pricePerBTC = BinanceWebSocketClient.CurrentPriceDivideBy5; // 当前单价
            double buyAmount = tradeAmount; // 购买数量（单位 BTC）
            long totalCost = pricePerBTC * tradeAmount;

            // if (!CanBuy())
            // {
            //     NotificationText.Push(IsChinese ? "现金不足，无法买入 BTC" : "Insufficient cash to buy BTC");
            //     return;
            // }

            // 检查现金是否足够
            if (!EconomyManager.IsEnough(new Cost(totalCost)))
            {
                NotificationText.Push(IsChinese ? "现金不足" : "Not enough cash");
                return;
            }

            // 扣除现金
            EconomyManager.Pay(new Cost((long)totalCost), true, true);

            // 增加账户资金（BTC）
            BtcBalanceManager.AddBalance(buyAmount*0.2d);

            NotificationText.Push(
                IsChinese
                    ? $"买入成功！花费 ${totalCost:N0} 获得 {buyAmount*0.2d:F2} BTC"
                    : $"Purchase successful! Spent ${totalCost:N0} for {buyAmount*0.2d:F2} BTC"
            );
        }

        private bool IsChinese => BlackMarketViewExtensionHelper.IsChinese();
        private async UniTask ExecuteWithdrawAsync()
        {
            double walletBTC = BtcBalanceManager.Balance;
            double amount = Math.Min(tradeAmount*0.2d, walletBTC); // 不能超过账户余额
            if (amount <= 0)
            {
                NotificationText.Push(IsChinese?"账户余额不足，无法取出 BTC":"insufficient btc");
                Debug.Log("[RealBTC] 没有足够的账户余额取出。");
                return;
            }

            BtcBalanceManager.AddBalance(-amount);
            
            Debug.Log($"[RealBTC] 取出 {amount} BTC");
            
            
            
            // 并行生成所有 BTC
            var tasks = new UniTask<Item>[tradeAmount];
            for (int i = 0; i < tradeAmount; i++)
                tasks[i] = ItemAssetsCollection.InstantiateAsync(BTC_ID);

            Item[] items = await UniTask.WhenAll(tasks);

            foreach (var item in items)
                ItemUtilities.SendToPlayer(item, true);

            NotificationText.Push(IsChinese?$"取出成功 获得 {tradeAmount} 个0.2BTC":$"WithDraw successfully! get {tradeAmount} 0.2 BTC");
        }
        private async UniTask ExecuteBuyAsync()
        {
            long cost = BinanceWebSocketClient.CurrentPriceDivideBy5 * tradeAmount;
            if (!CanBuy())
            {
                NotificationText.Push(IsChinese?"现金不足，无法买入 BTC":"insufficient money");
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

            NotificationText.Push(IsChinese?$"买入成功！花费 ${cost:N0} 获得 {tradeAmount} BTC":$"Buy successfully! Spend ${cost:N0} to get {tradeAmount} BTC");
        }

        private void ExecuteSell()
        {
            long pricePerBTC = BinanceWebSocketClient.CurrentPriceDivideBy5; // 每个BTC价格
            double sellAmount = tradeAmount*0.2d; // 卖出的BTC数量（对应一个物品0.2BTC）
            long totalIncome = pricePerBTC * tradeAmount - feeSell;

            if (BtcBalanceManager.Balance < sellAmount)
            {
                NotificationText.Push(IsChinese ? "账户资金不足" : "Insufficient BTC balance");
                return;
            }

            // 扣除 BTC 账户资金
            BtcBalanceManager.AddBalance(-sellAmount);
            EconomyManager.Add((long)totalIncome);

            NotificationText.Push(
                IsChinese
                    ? $"卖出成功！获得 ${totalIncome:N0}（含50手续费）"
                    : $"Sold successfully! Received ${totalIncome:N0} (including 50 fee)"
            );

            // 可选：保存余额
            //BtcBalanceManager.SaveBalance();
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
            LocalizationManager.OnSetLanguage -= LazyUpdate;

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
    

