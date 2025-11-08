using System;
using Saves;
using UnityEngine;

namespace RealBTC.Data
{
    public class BtcBalanceManager
    {
        private const string SaveKey = "RealBTC_BtcBalanceManager"; // 存档用唯一键
        private static double _btcBalance = 0;
        private const int BTC_ID = 388;                     // Bitcoin 物品 ID
        public static event Action OnBalanceChanged;
        public static double Balance
        {
            get => _btcBalance;
            set
            {
                _btcBalance = value;
                OnBalanceChanged?.Invoke();
            }
        }
        public static int InventoryBtcCount => ItemUtilities.GetItemCount(BTC_ID);

        // 加载余额
        public static void Load()
        {
            if (SavesSystem.KeyExisits(SaveKey))
            {
                _btcBalance = SavesSystem.Load<double>(SaveKey);
                OnBalanceChanged?.Invoke();
                Debug.Log($"[RealBTC] 已加载交易所余额：{_btcBalance} BTC");
            }
            else
            {
                _btcBalance = 0;
                SavesSystem.Save(SaveKey, _btcBalance);
                Debug.Log("[RealBTC] 未发现余额记录，已初始化为 0 BTC");
            }
        }

        // 保存余额
        public static void Save()
        {
            Debug.Log($"[RealBTC] 保存交易所余额：{_btcBalance} BTC");

            SavesSystem.Save(SaveKey, _btcBalance);
            SavesSystem.SaveFile(); // 触发存档写入
            Debug.Log($"[RealBTC] 已保存交易所余额：{_btcBalance} BTC");
        }

        // 修改余额（买入 / 卖出）
        public static void AddBalance(double amount)
        {
            _btcBalance += amount;
            if (_btcBalance < 0) _btcBalance = 0; // 防止负值
            OnBalanceChanged?.Invoke();
            Save();
        }

        public static void SetBalance(double value)
        {
            _btcBalance = Math.Max(0.0, value);
            OnBalanceChanged?.Invoke();
            Save();
        }
    }
    
}