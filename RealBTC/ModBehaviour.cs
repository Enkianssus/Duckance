//namespace RealBTC;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Duckov.BlackMarkets;
using Duckov.BlackMarkets.UI;
using Duckov.Economy;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using ItemStatsSystem;

using RealBTC.Utils;

namespace RealBTC
{

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public DuckancePanel duckancePanel;
        public Transform duckanceTap;

        public Transform MainContent;
        public Transform Taps;

        public Color tapSelectOn;
        public Color tapSelectOff;
        //public event Action OnUpdate;
        void Awake()
        {
            Debug.Log("RealBTC Loaded");
            BitcoinPriceManager.Init();
            //RuntimeUnityEditorCore.Instance.Show = true;
            LevelManager.OnAfterLevelInitialized += Init;



            //RuntimeUnityEditorCore.Instance.Show = true;
        }
        void OnDestroy()
        {
           BitcoinPriceManager.Instance.StopUpdateLoop();
        }
        void OnEnable()
        {

        }
        void OnDisable()
        {
          
        }
        
        
        private void Start()
        {
            // 启动循环任务
            //BitcoinPriceManager.Instance.StartUpdateLoop();
            //BinanceWebSocketClient.Init();

            //InvokeRepeating(nameof(TryInjectOnce), 1f, 1f);
            //Item glick = ItemAssetsCollection.InstantiateAsync(254);
            //ItemUtilities.SendToPlayer(glick);
            
            //EconomyManager
            //LevelManager
            //UniTask.Delay(System.TimeSpan.FromSeconds(1)).ContinueWith(() => Init()).Forget();
            // UniTask.WaitUntil(() => BlackMarketView.Instance != null)
            //     .ContinueWith(() => Init())
            //     .Forget();
        }

        private void Update()
        {
            //OnUpdate?.Invoke();
        }

        private void Init()
        {
            var instance = BlackMarketView.Instance;
            if (instance == null || BlackMarket.Instance == null)
                return;
            var finding=instance.transform.FindChildrenByNames("MainContent","Tabs","Suppy","Demand").ToArray();
             MainContent = finding[0];
             Taps = finding[1];
             var supply= finding[3];
             supply.name = "Supply";
            var Tab_Supply = Taps.GetChild(1);
            var demand = finding[2];

            Debug.Log("finding[0]"+finding[0].name);
            Debug.Log("finding[1]"+finding[1].name);
           Debug.Log("finding[2]"+finding[2].name);
           Debug.Log("finding[3]"+finding[3].name);

           var go = Instantiate(demand,demand.parent);
           go.name = "DuckancePanel";
           go.transform.SetSiblingIndex(Taps.GetSiblingIndex()+1);
           go.gameObject.SetActive(false);

           var childs = go.GetComponentsInChildren<Transform>();
           foreach (var v in childs)
           {
               if(v==go)continue;
               Destroy(v.gameObject);
           }
           Destroy(go.GetComponent<VerticalLayoutGroup>());
           Destroy(go.GetComponent<DemandPanel>());

           

            // === 创建 Bitcoin 面板 ===
            // var go = new GameObject("DuckancePanel", typeof(RectTransform));
            // go.transform.SetParent(MainContent, false);
            //
            // var rt = go.GetComponent<RectTransform>();
            // var refRect = MainContent.GetChild(0).GetComponent<RectTransform>();
            // rt.anchorMin = refRect.anchorMin;
            // rt.anchorMax = refRect.anchorMax;
            // rt.pivot = refRect.pivot;
            // rt.sizeDelta = refRect.sizeDelta;
            // rt.anchoredPosition = refRect.anchoredPosition;
            // go.transform.SetSiblingIndex(Taps.GetSiblingIndex()+1);
            // go.SetActive(false);

            duckancePanel = go.gameObject.AddComponent<DuckancePanel>();
            duckancePanel.Setup();
           // OnUpdate += duckancePanel.UpdateThis;

            // === 复制 Supply 按钮创建新标签 ===
            duckanceTap = Instantiate(Tab_Supply, Taps);
            duckanceTap.name = "Tap_DuckancePanel";
            duckanceTap.GetChild(0).KeepThisMonoDisableOthers<TMP_Text>().text="鸭安Duckance";

            var allTaps = Taps.GetComponentsInChildren<Button>();

            foreach (var b in allTaps)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(()=>OnTabClick(b));
            }
            

           
            Debug.Log("[Duckance] Duckance 交易面板注入完成。");
        }
        
        

        public void OnTabClick(Button clickedButton)
        {
             var allButtons=Taps.GetComponentsInChildren<Button>();
             foreach (var v in allButtons)
             {
                 if (v == clickedButton)
                 {
                     v.image.color = new Color(0.5137f,0.6314f ,0.6588f,1);
                 }
                 else
                 {
                     v.image.color=new Color(0.2039f, 0.3843f,0.4627f, 1);
                 }
             }
            // 找到按钮的父物体
            Transform parent = Taps;
            

            // 当前按钮名字处理后（_后面的部分）
            string currentName = GetNameAfterUnderscore(clickedButton.name);

            // 找到 MainContent
           // Transform mainContent = parent.Find("MainContent");
            

            // 先关闭所有匹配的内容
            foreach (Transform child in parent)
            {
                string tabName = GetNameAfterUnderscore(child.name);
                if (string.IsNullOrEmpty(tabName)) continue;

                // 在 MainContent 下找对应的子物体
                foreach (Transform contentChild in MainContent)
                {
                    if (string.Equals(contentChild.name, tabName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        contentChild.gameObject.SetActive(false);
                    }
                }
            }

            // 再开启与当前按钮匹配的那一个
            foreach (Transform contentChild in MainContent)
            {
                if (string.Equals(contentChild.name, currentName, System.StringComparison.OrdinalIgnoreCase))
                {
                    contentChild.gameObject.SetActive(true);
                    break;
                }
            }
        }

        // 提取下划线后的文字，比如 "Tab_Home" -> "Home"
        private string GetNameAfterUnderscore(string name)
        {
            int index = name.IndexOf('_');
            if (index >= 0 && index < name.Length - 1)
                return name.Substring(index + 1);
            return string.Empty;
        }
        
        private async UniTask FetchBitcoinPriceAsync()
        {
            string url = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                var asyncOp = webRequest.SendWebRequest();

                // 等待请求完成
                while (!asyncOp.isDone)
                    await UniTask.Yield();  // 让出线程，不阻塞主线程

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string response = webRequest.downloadHandler.text;

                    // 回到主线程更新 UI 或变量
                    //UnityMainThreadDispatcher.Enqueue(() =>
                   // {
                        Debug.Log("Bitcoin Price: " + response);
                    //});
                }
            }
        }
    }
}