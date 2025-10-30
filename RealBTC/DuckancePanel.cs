
using Cysharp.Threading.Tasks;
using Duckov.Economy;
using Duckov.UI;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace RealBTC
{
    public class DuckancePanel : MonoBehaviour
    {
       private const int BTC_ID = 388;                     // Bitcoin 物品 ID
        private const float REFRESH_INTERVAL = 5f;          // 价格刷新间隔（秒）

        private TextMeshProUGUI _priceText;
        private TextMeshProUGUI _holdingText;
        private GameObject _buyEntry;
        private GameObject _sellEntry;
        private Button _buyButton;
        private Button _sellButton;
        private TextMeshProUGUI _buyPriceLabel;
        private TextMeshProUGUI _sellPriceLabel;

        private bool _active;

        private void OnEnable()
        {
            BitcoinPriceManager.Instance.StartUpdateLoop();
        }

        private void OnDisable()
        {
            BitcoinPriceManager.Instance.StopUpdateLoop();
        }

        public void Setup()
        {
            //GameManager
            BuildUI();
            _active = true;
            //RunPriceWatcher().Forget();
        }

        #region UI 构建

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
            _buyEntry = CreateTradeEntry(listRoot.transform, "买入 0.2 BTC", new Color(0.2f, 0.7f, 1f, 0.85f),
                out _buyButton, out _buyPriceLabel);
            _sellEntry = CreateTradeEntry(listRoot.transform, "卖出 0.2 BTC", new Color(1f, 0.4f, 0.4f, 0.85f),
                out _sellButton, out _sellPriceLabel);

            // ---------- 打开文件夹按钮（调试用） ----------
           // var openBtn = CreateOpenFolderButton(root.transform);
           // openBtn.onClick.AddListener(OpenModFolder);
           _buyButton.onClick.AddListener(()=>ExecuteBuy(BitcoinPriceManager.CurrentBitcoinPriceDivideBy5));
           _sellButton.onClick.AddListener(()=>ExecuteSell(BitcoinPriceManager.CurrentBitcoinPriceDivideBy5));

           UpdateThis(BitcoinPriceManager.CurrentBitcoinPrice);
           BitcoinPriceManager.OnPriceUpdate += UpdateThis;
        }
 
        
        private GameObject CreateTradeEntry(Transform parent, string title, Color bgColor,
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

            return go;
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

        public void UpdateThis(int rawPrice)
        {
            //int rawPrice = BitcoinPriceManager.CurrentBitcoinPriceDivideBy5; // 0.2 BTC 价格
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
                    changeText = change > 0 ? $"(+{change:F2}%)" : $"({change:F2}%)";
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
            _sellPriceLabel.text = priceValid ? $"可获得：{priceStr}" : "——";

            bool canBuy = priceValid && EconomyManager.Money >= rawPrice;
            bool canSell = priceValid && holding > 0;

            _buyButton.interactable = canBuy;
            _sellButton.interactable = canSell;
        }
        private void RefreshAll()
        {
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
                        changeText = change > 0 ? $"(+{change:F2}%)" : $"({change:F2}%)";
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
                _sellPriceLabel.text = priceValid ? $"可获得：{priceStr}" : "——";

                bool canBuy = priceValid && EconomyManager.Money >= rawPrice;
                bool canSell = priceValid && holding > 0;

                SetButton(_buyButton, canBuy, () => ExecuteBuy(rawPrice));
                SetButton(_sellButton, canSell, () => ExecuteSell(rawPrice));

               
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

        private void ExecuteBuy(int cost)
        {
            if (EconomyManager.Money < cost)
            {
                NotificationText.Push("现金不足，无法买入 BTC");
                return;
            }

            EconomyManager.Pay(new Cost((long)cost), true, true);
            var item = ItemAssetsCollection.InstantiateSync(BTC_ID);
            ItemUtilities.SendToPlayer(item, true);
            NotificationText.Push($"买入成功！花费 ${cost:N0} 获得 1 BTC");
            RefreshAll();
        }

        private void ExecuteSell(int income)
        {
            if (ItemUtilities.GetItemCount(BTC_ID) <= 0)
            {
                NotificationText.Push("没有 BTC 可卖");
                return;
            }

            // 扣除 1 个 BTC
            if (!new Cost(0L, new[] { (388, 1L) }).Pay(false, false))
            {
                NotificationText.Push("扣除 BTC 失败");
                return;
            }

            EconomyManager.Add((long)income);
            NotificationText.Push($"卖出成功！获得 ${income:N0}");
            RefreshAll();
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
            
            
                BitcoinPriceManager.OnPriceUpdate -= UpdateThis;
            
        }
    }
    }
    

