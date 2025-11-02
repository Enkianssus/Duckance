using System;
using System.Net.Http;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RealBTC.Network
{
   public static class VersionChecker
    {
        public enum VersionStatus
        {
            FetchFailed = -1,  // 获取失败
            NotFetched = 0,    // 尚未获取
            UpToDate = 1,      // 当前为最新
            Outdated = 2,      // 有新版本
        }

        private static readonly string versionUrl = "https://raw.githubusercontent.com/Enkianssus/Duckance/master/version.txt";

        public static string CurrentVersion { get; private set; } = "0.0.6"; // 当前版本
        public static string LatestVersion { get; private set; } = "";
        public static VersionStatus Status { get; private set; } = VersionStatus.NotFetched;

        private static bool _isFetching = false;

        /// <summary>
        /// 获取并更新版本信息（使用 UnityWebRequest），支持重试与超时。
        /// </summary>
        public static async UniTask FetchVersionAsync(int retryDelaySeconds = 10, int maxRetries = 5, CancellationToken token = default)
        {
            if (_isFetching)
                return;

            _isFetching = true;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var webRequest = UnityWebRequest.Get(versionUrl);
                webRequest.timeout = 8; // 超时 8 秒

                try
                {
                    var asyncOp = webRequest.SendWebRequest();
                    while (!asyncOp.isDone)
                    {
                        token.ThrowIfCancellationRequested();
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        string fetched = webRequest.downloadHandler.text.Trim();
                        LatestVersion = fetched;

                        if (string.IsNullOrEmpty(LatestVersion))
                        {
                            Status = VersionStatus.FetchFailed;
                            Debug.LogWarning("[VersionChecker] 获取失败：返回空内容");
                        }
                        else
                        {
                            bool isLatest = CompareVersions(CurrentVersion, LatestVersion);
                            Status = isLatest ? VersionStatus.UpToDate : VersionStatus.Outdated;
                            Debug.Log($"[VersionChecker] 当前版本: {CurrentVersion}, 最新版本: {LatestVersion}, 状态: {Status}");
                            _isFetching = false;
                            return;
                        }
                    }
                    else
                    {
                        Status = VersionStatus.FetchFailed;
                        Debug.LogWarning($"[VersionChecker] 请求失败: {webRequest.error}");
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning("[VersionChecker] 请求被取消");
                    break;
                }
                catch (Exception ex)
                {
                    Status = VersionStatus.FetchFailed;
                    Debug.LogWarning($"[VersionChecker] 异常: {ex.Message}");
                }

                if (attempt < maxRetries)
                {
                    Debug.Log($"[VersionChecker] {retryDelaySeconds}s 后重试 ({attempt}/{maxRetries})...");
                    await UniTask.Delay(retryDelaySeconds * 1000, cancellationToken: token);
                }
            }

            Status = VersionStatus.FetchFailed;
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
                return false;
            }
        }
    }
}