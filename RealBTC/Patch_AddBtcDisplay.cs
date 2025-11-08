 using System;
 using System.Collections.Generic;
 using System.Reflection;
 using Duckov.BlackMarkets.UI;
 using Duckov.UI;
 using HarmonyLib;
 using ItemStatsSystem;
 using RealBTC.Data;
 using TMPro;
 using UnityEngine;
 using UnityEngine.UI;
 using Object = UnityEngine.Object;

 namespace RealBTC.Patches
{

  [HarmonyPatch(typeof(MoneyDisplay), "Awake")]
    public class PatchMoneyDisplayAwake
    {
        private struct DisplayData
        {
            public TextMeshProUGUI text;
            public int index;
        }

        public static void Prefix(MoneyDisplay __instance)
        {
            // 获取 MoneyDisplay 的 text
            var textField = typeof(MoneyDisplay).GetField("text", BindingFlags.Instance | BindingFlags.NonPublic);
            var originalText = (TextMeshProUGUI)textField?.GetValue(__instance);
            if (originalText == null) return;

            var parent = originalText.transform.parent;
            int baseIndex = originalText.transform.GetSiblingIndex();

            // 复制文本
            var btcText = Object.Instantiate(originalText, parent);
            btcText.name = "BtcBalanceText";
            btcText.text = $"{BtcBalanceManager.Balance:0.#}";
            btcText.color = new Color(1f, 0.65f, 0f); // BTC 橙色
            btcText.fontSize = originalText.fontSize * 0.9f;
            btcText.transform.SetSiblingIndex(baseIndex + 1);

            // 添加间距
            CreateSpace(parent, baseIndex + 1);

            // 创建 BTC 图标
            var btcItem = ItemAssetsCollection.GetPrefab(388); // 假设 388 是 BTC 图标
            if (btcItem?.Icon != null)
            {
                var iconObj = new GameObject("BtcBalanceIcon");
                iconObj.transform.SetParent(parent);
                iconObj.transform.SetSiblingIndex(baseIndex + 2);

                var image = iconObj.AddComponent<Image>();
                image.sprite = btcItem.Icon;
                image.preserveAspect = true;

                var layout = iconObj.AddComponent<LayoutElement>();
                layout.preferredWidth = 45f;
                layout.preferredHeight = 45f;

                CreateSpace(parent, baseIndex + 3);
            }
        }

        private static void CreateSpace(Transform parent, int index)
        {
            var spacer = new GameObject("Spacer_BTC");
            spacer.transform.SetParent(parent);
            spacer.transform.SetSiblingIndex(index);
            var le = spacer.AddComponent<LayoutElement>();
            le.preferredWidth = 10f;
        }
    }

    // 事件驱动刷新 BTC 文本
    [HarmonyPatch(typeof(MoneyDisplay), "OnEnable")]
    public class PatchMoneyDisplayOnEnable
    {
        private static Dictionary<MoneyDisplay, Action> refreshCallbacks = new Dictionary<MoneyDisplay, Action>();

        public static void Unregister(MoneyDisplay __instance)
        {
            if (refreshCallbacks.TryGetValue(__instance, out var callback))
            {
                BtcBalanceManager.OnBalanceChanged -= callback;
                refreshCallbacks.Remove(__instance);
            }
        }

        public static void Prefix(MoneyDisplay __instance)
        {
            //Debug.Log("PrefixPrefixPrefixPrefixPrefixPrefixPrefixPrefixPrefixPrefix");

            Unregister(__instance);

            //Debug.Log("UnregisterUnregisterUnregisterUnregisterUnregisterUnregisterUnregisterUnregisterUnregisterUnregister");

            //Debug.Log("btcTextTransform == null"+btcTextTransform == null);
            
            //Debug.Log("btcTextTransform == btcTextTransform == nullbtcTextTransform == nullbtcTextTransform == nullbtcTextTransform == nullbtcTextTransform == nullbtcTextTransform == null");

            //if(__instance.transform.parent.parent.name=="BlackMarketView")
                if (__instance.GetComponentInParent<BlackMarketView>() != null)
                {
                    var v = __instance.GetComponent<RectTransform>();
                    v.sizeDelta = new Vector2(530, 80);
                }
            var btcTextTransform = __instance.transform.Find("BtcBalanceText");
            //Debug.Log(__instance.name);
            //Debug.Log(btcTextTransform == null);
            if (btcTextTransform == null) return;

            var btcText = btcTextTransform.GetComponent<TextMeshProUGUI>();
            //Debug.Log("btcText == null"+btcText == null);

            if (btcText == null) return;

            void Refresh()
            {
                //Debug.Log("Refresh"+BtcBalanceManager.Balance);
                if (!btcText.gameObject.activeInHierarchy) return;
                double balance = BtcBalanceManager.Balance;
                btcText.text = balance >= 0.1 ? $"{balance:0.#}" : "0";
            }

            // 注册事件回调
            BtcBalanceManager.OnBalanceChanged += Refresh;
            refreshCallbacks.Add(__instance, Refresh);

            // 立即刷新一次
            Refresh();
        }
    }

    // 注销事件
    [HarmonyPatch(typeof(MoneyDisplay), "OnDestroy")]
    public class PatchMoneyDisplayOnDestroy
    {
        public static void Prefix(MoneyDisplay __instance)
        {
            PatchMoneyDisplayOnEnable.Unregister(__instance);
        }
    }


   
}