using System;
using System.Net.Http;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RealBTC.Network
{
    public class VersionChecker
    {
        public enum VersionStatus
    {
        FetchFailed=-1,    // 获取失败
        NotFetched=0,    // 尚未获取
        UpToDate=1,      // 当前为最新
        Outdated=2,      // 有新版本
    }

    private static readonly string versionUrl = "https://raw.githubusercontent.com/Enkianssus/Duckance/master/version.txt";
    private static readonly HttpClient httpClient = new HttpClient();

    public static string CurrentVersion { get; private set; } = "0.0.5"; // 当前版本
    public static string LatestVersion { get; private set; } = "";
    public static VersionStatus Status { get; private set; } = VersionStatus.NotFetched;

    private static bool _isFetching = false;

    /// <summary>
    /// 获取并更新版本信息，带自动重试。
    /// </summary>
    public static async UniTask FetchVersionAsync(int retryDelaySeconds = 10, int maxRetries = 5, CancellationToken token = default)
    {
        if (_isFetching)
            return;

        _isFetching = true;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                string fetched = await httpClient.GetStringAsync(versionUrl);
                LatestVersion = fetched.Trim();

                if (string.IsNullOrEmpty(LatestVersion))
                {
                    Status = VersionStatus.FetchFailed;
                    Debug.LogWarning("[VersionChecker] 获取失败：空响应");
                }
                else
                {
                    Status = CompareVersions(CurrentVersion, LatestVersion)
                        ? VersionStatus.UpToDate
                        : VersionStatus.Outdated;

                    Debug.Log($"[VersionChecker] 当前版本: {CurrentVersion}, 最新版本: {LatestVersion}, 状态: {Status}");
                    _isFetching = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                Status = VersionStatus.FetchFailed;
                Debug.LogWarning($"[VersionChecker] 第 {attempt} 次获取失败: {ex.Message}");
            }

            if (attempt < maxRetries)
            {
                Debug.Log($"[VersionChecker] {retryDelaySeconds}s 后重试...");
                await UniTask.Delay(retryDelaySeconds * 1000, cancellationToken: token);
            }
        }

        _isFetching = false;
    }

    /// <summary>
    /// 简单版本比较逻辑，例如 "1.2.3"。
    /// </summary>
    private static bool CompareVersions(string current, string latest)
    {
        try
        {
            Version cur = new Version(current);
            Version lat = new Version(latest);
            return cur >= lat;
        }
        catch
        {
            // 如果解析失败，保守认为旧版
            return false;
        }
    }
    }
}