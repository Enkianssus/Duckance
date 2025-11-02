using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

using Duckov.BlackMarkets;
using Duckov.BlackMarkets.UI;
using RealBTC.Utils;
using TMPro;
using UnityEngine.UI;
using Object = UnityEngine.Object;


namespace RealBTC.UI
{
    public class BlackMarketViewExtensionHelper
    {
        private static BlackMarketViewExtensionHelper? instance;

        // static BlackMarketViewExtensionHelper()
        // {
        //     instance = new BlackMarketViewExtensionHelper();
        //     LevelManager.OnAfterLevelInitialized += BlackMarketViewExtensionHelper.Instance.InitThis;
        // }
        public static BlackMarketViewExtensionHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BlackMarketViewExtensionHelper();
                    //LevelManager.OnAfterLevelInitialized += instance.Init;
                } 
                return instance;
            }
            private set
            {}
            
        }

        public static void OnDestroy()
        {
            LevelManager.OnAfterLevelInitialized -= BlackMarketViewExtensionHelper.Instance.InitThis;
            instance = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsChinese()
        {
            return SodaCraft.Localizations.LocalizationManager.CurrentLanguage == SystemLanguage.Chinese ||
                   SodaCraft.Localizations.LocalizationManager.CurrentLanguage == SystemLanguage.ChineseSimplified ||
                   SodaCraft.Localizations.LocalizationManager.CurrentLanguage == SystemLanguage.ChineseTraditional;

        }

        public Transform MainContent;
        public Transform Taps;
        public Transform Tab_Supply;
        public Transform Tab_Demand;
        public Transform Supply;
        public Transform Demand;

        public void Init()
        {
            LevelManager.OnAfterLevelInitialized -= BlackMarketViewExtensionHelper.Instance.InitThis;
            LevelManager.OnAfterLevelInitialized += BlackMarketViewExtensionHelper.Instance.InitThis;
        }
        
        
        private void InitThis()
        {
            //Debug.Log("InitThisInitThisInitThisInitThisInitThisInitThisInitThisInitThisInitThisInitThisInitThisInitThisInitThisInitThisInitThisInitThis");
            var blackMarketView = BlackMarketView.Instance;
            //Debug.Log("blackMarketView == null" + blackMarketView == null);
            //Debug.Log("BlackMarket.Instance" +BlackMarket.Instance == null);
            if (blackMarketView == null || BlackMarket.Instance == null)
                return;
            
            var finding=blackMarketView.transform.FindChildrenByNames("MainContent","Tabs","Suppy","Demand").ToArray();
            if(MainContent==null) MainContent = finding[0];
            if(Taps==null)   Taps = finding[1];
            if(Supply==null)
            {
                
                Supply = finding[3];
                Supply.name = "Supply";
            }
            if(Tab_Supply==null)Tab_Supply = Taps.GetChild(1);
            if(Tab_Demand==null)  Tab_Demand=Taps.GetChild(0);
            if(Demand==null)            Demand = finding[2];

            ProcessPendingPanels();
            SetupAllTabs();
        }
        
        public class PanelRequest
        {
            public string Id;
            public string TabName;
            public Action<Transform, Transform> Callback;

            public PanelRequest(string id, string tabName, Action<Transform, Transform> callback)
            {
                Id = id;
                TabName = tabName;
                Callback = callback;
            }
        }

// 存储请求的列表
        private List<PanelRequest> pendingPanels = new List<PanelRequest>();

// 别的地方调用时，把请求存起来
        public void QueueCreatePanel(string id, string tabName, Action<Transform, Transform> callback)
        {
            pendingPanels.Add(new PanelRequest(id, tabName, callback));
        }

// 在合适的时候统一调用，比如某个初始化阶段
        private void ProcessPendingPanels()
        {
            foreach (var req in pendingPanels)
            {
                CreatePanel(req.Id, req.TabName, req.Callback);
            }

            //pendingPanels.Clear(); // 调用后清空
        }


        Action<string, string, Action<Transform, Transform>> createPanelDelegate;
        

        private void CreatePanel(string id, string tabName, Action<Transform, Transform> callback)
        {
            // 调用原方法
            CreatePanel(id, tabName, out var tabTransform, out var panelTransform);

            // 执行回调
            callback?.Invoke(tabTransform, panelTransform);
        }
        
        public void CreatePanel(string id,string tabName,out Transform tab,out Transform panel)
        {
            var go = UnityEngine.Object.Instantiate(Demand,Demand.parent);
            go.name = id;
            go.transform.SetSiblingIndex(Taps.GetSiblingIndex()+1);
            go.gameObject.SetActive(false);

            var childs = go.GetComponentsInChildren<Transform>();
            foreach (var v in childs)
            {
                if(v==go)continue;
                UnityEngine.Object.Destroy(v.gameObject);
            }
            Object.Destroy(go.GetComponent<VerticalLayoutGroup>());
            Object.Destroy(go.GetComponent<DemandPanel>());
            panel = go.transform;
            
            
            tab = Object. Instantiate(Tab_Supply, Taps);
            tab.name = "Tap_"+id;
            tab.GetChild(0).KeepThisMonoDestroyOthers<TMP_Text>().text=tabName;
        }


        private void SetupAllTabs()
        {
            var allTaps = Taps.GetComponentsInChildren<Button>();

            foreach (var b in allTaps)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(()=>OnTabClick(b));
            }
        }
        
        private void OnTabClick(Button clickedButton)
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
    }
    
}