using HarmonyLib;
using RealBTC.Network;
using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RealBTC.Compatibility.MakeBitcoinGreatAgain
{
    /// <summary>
    /// 劫持 MakeBitcoinGreatAgain 的 BitcoinPriceManager，
    /// 替换其价格更新为 Binance 实时价格。
    /// </summary>
    public static class BitcoinHijacker
    {
        private static Harmony _harmony;
        private static Type _managerType;
        private static object _managerInstance;

        private static FieldInfo _currentPriceField;
        private static FieldInfo _previousPriceField;
        private static FieldInfo _trendField;
        private static FieldInfo _lastUpdateTimeField;

        private static bool _initialized;

        public static async UniTask Activate()
        {
            if (_initialized) return;
            _initialized = true;

            await UniTask.Delay(1000);
            
            _managerType = AccessTools.TypeByName("MakeBitcoinGreatAgain.Core.BitcoinPriceManager");
            if (_managerType == null)
            {
                Debug.Log("[RealBTC] 未找到 BitcoinPriceManager 类型，跳过联动。");
                return;
            }

            //CacheInstance();
            await  WaitForInstance();
            CacheFields();

            _harmony = new Harmony("RealBTC.MakeBitcoinGreatAgain.Hijack");
            TryPatch("PriceFluctuationLoop", prefix: true); // 阻止价格波动逻辑
            TryPatch("UpdateBitcoinPrice", prefix: true);   // 阻止手动更新逻辑
           // TryPatch("InitializeBitcoinManager", postfix: true); // 初始化确认

            Debug.Log("[RealBTC] BitcoinHijacker 已激活，等待 Binance 数据。");

            // 当 Binance 有新数据时同步
            BinanceWebSocketClient.OnPriceUpdate += info => ApplyPrice(info.Close);
        }

        private static void CacheInstance()
        {
            try
            {
                var instField = AccessTools.Field(_managerType, "Instance");
                if (instField != null)
                    _managerInstance = instField.GetValue(null);

                if (_managerInstance == null)
                {
                    var obj = GameObject.FindObjectOfType(_managerType);
                    if (obj != null)
                        _managerInstance = obj;
                }

                Debug.Log($"[RealBTC] 捕获 BitcoinPriceManager 实例: {_managerInstance != null}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RealBTC] CacheInstance 失败: {ex}");
            }
        }

        private static void CacheFields()
        {
            // 正确：使用静态字段的 backing field 名称
            _currentPriceField = AccessTools.Field(_managerType, "<CurrentBitcoinPrice>k__BackingField");
            _previousPriceField = AccessTools.Field(_managerType, "<PreviousBitcoinPrice>k__BackingField");
            _trendField = AccessTools.Field(_managerType, "<CurrentTrendValue>k__BackingField");
            _lastUpdateTimeField = AccessTools.Field(_managerType, "<LastUpdateTime>k__BackingField");

            if (_currentPriceField == null)
                Debug.LogError("[RealBTC] 无法找到 CurrentBitcoinPrice 的 backing field！");
        }

        private static void TryPatch(string methodName, bool prefix = false, bool postfix = false)
        {
            var method = AccessTools.Method(_managerType, methodName);
            if (method == null)
            {
                Debug.LogWarning($"[RealBTC] 未找到方法：{methodName}");
                return;
            }

            var harmonyMethod = new HarmonyMethod(typeof(BitcoinHijacker),
                prefix ? nameof(PrefixBlock) : postfix ? nameof(PostfixNotice) : null);

            if (harmonyMethod == null) return;

            _harmony.Patch(method,
                prefix ? harmonyMethod : null,
                postfix ? harmonyMethod : null);
        }

        private static bool PrefixBlock()
        {
            // 阻止原始逻辑执行
            return false;
        }

        private static void PostfixNotice()
        {
            
        }

        private static async UniTask WaitForInstance()
        {
            for (int i = 0; i < 100; i++)
            {
                _managerInstance = GameObject.FindObjectOfType(_managerType);
                if (_managerInstance != null) return;
                await System.Threading.Tasks.Task.Delay(100);
            }
        }
        
        private static void ApplyPrice(int newPrice)
        {
            if (_currentPriceField == null)
            {
                Debug.LogError("[RealBTC] _currentPriceField 未初始化！");
                return;
            }

            try
            {
                // 正确：直接操作静态字段（不需要实例）
                int prev = (int)(_currentPriceField.GetValue(null) ?? 0);

                _previousPriceField?.SetValue(null, prev);
                _currentPriceField?.SetValue(null, newPrice);
                _trendField?.SetValue(null, 0f);
                _lastUpdateTimeField?.SetValue(null, DateTime.Now);

                // 触发事件（需要实例）
                if (_managerInstance != null)
                    ForceInvokeEvent(_managerInstance, "OnPriceChanged");

                //Debug.Log($"[RealBTC] 已同步 Binance 实时价格：{prev} → {newPrice}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealBTC] 写入价格失败：{ex}");
            }
        }

        private static void ForceInvokeEvent(object instance, string eventName)
        {
            try
            {
                var type = instance.GetType();
                var evtField = type.GetField(eventName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                               ?? type.GetField($"<{eventName}>k__BackingField",
                                   BindingFlags.Instance | BindingFlags.NonPublic);

                if (evtField == null)
                {
                    Debug.LogWarning($"[RealBTC] 找不到事件字段：{eventName}");
                    return;
                }

                var del = evtField.GetValue(instance) as MulticastDelegate;
                if (del == null)
                {
                   // Debug.Log("[RealBTC] OnPriceChanged 没有订阅者");
                    return;
                }

                foreach (var handler in del.GetInvocationList())
                {
                    try { handler.DynamicInvoke(); }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RealBTC] 调用 OnPriceChanged 失败：{ex}");
                    }
                }

                //Debug.Log("[RealBTC] 成功触发 OnPriceChanged 事件。");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RealBTC] ForceInvokeEvent 异常：{ex}");
            }
        }
    }
}
