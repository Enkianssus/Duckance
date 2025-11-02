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
using RealBTC.Compatibility.MakeBitcoinGreatAgain;
using RealBTC.Data;
using RealBTC.Network;
using RealBTC.UI;
using RealBTC.Utils;

namespace RealBTC
{

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        //private CandleManager candleManager;
        private void Awake()
        {
            VersionChecker.FetchVersionAsync().Forget();
        }

        void OnEnable()
        {
            //BitcoinPriceManager.Init();

            BlackMarketViewExtensionHelper.Instance.Init();
            //RuntimeUnityEditorCore.Instance.Show = true;
            BlackMarketViewExtensionHelper.Instance.QueueCreatePanel("DuckancePanel","鸭安Duckance", (t, p) =>
            {
                
                var duckancePanel =p.gameObject.AddComponent<DuckancePanel>();
                duckancePanel.Setup();
                if (CandleManager.Instance == null)
                    CandleManager.Instance = new CandleManager(20, duckancePanel.transform, new Vector2(0, 0),
                        new Vector2(1, 0.3f));
                else
                {
                    CandleManager.Instance.RebuildCandles(20, duckancePanel.transform, new Vector2(0, 0),
                        new Vector2(1, 0.3f));
                    CandleManager.useSilentUpdate = false;
                }
            });
            Debug.Log("Duckance Loaded");
        }

        private void Start()
        {
            BinanceWebSocketClient.Init();
            BitcoinHijacker.Activate().Forget();
            BtcBalanceManager.Load();
        }

        async void OnDestroy()
        {
           await BinanceWebSocketClient.Close();
           BlackMarketViewExtensionHelper.OnDestroy();
           CandleManager.OnDestroy();
        }
    }
}