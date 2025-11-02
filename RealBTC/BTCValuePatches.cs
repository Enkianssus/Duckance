using HarmonyLib;
using ItemStatsSystem;

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace RealBTC.Patches
{
    
    [HarmonyPatch]
    public static class BTCValuePatches
    {
        [HarmonyPatch(typeof (Item), "Value", MethodType.Getter)]
        [HarmonyPostfix]
        public static void Item_Value_Postfix(Item __instance, ref int __result)
        {
            if ((__instance != null ? (__instance.TypeID != 388 ? 1 : 0) : 1) != 0)
            {
                //PerformanceMonitor.IncrementCounter("Value_NonBitcoin");
            }
            else
            {
                //PerformanceMonitor.IncrementCounter("Bitcoin_Value_Access");
                try
                {
                  // __result = BitcoinPriceManager.CurrentBitcoinPrice;
                }
                catch (Exception ex)
                {
                    Debug.LogError((object) ("[MakeBitcoinGreatAgain] Error in Item.Value patch: " + ex.Message));
                   // PerformanceMonitor.IncrementCounter("Bitcoin_Value_Error");
                }
            }
        }
    }
    
}