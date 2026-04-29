using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// Quản lý ELO và thống kê người chơi qua PlayFab.
/// </summary>
public class PlayerDataService
{
    private readonly PlayFabConfig _config;
    public const string EloStatisticName = "elo";
    public const string WinsStatisticName = "wins";
    public const string LossesStatisticName = "losses";
    public const string TotalGamesStatisticName = "totalGames";

    public PlayerDataService(PlayFabConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Lấy ELO hiện tại. Mặc định 0 nếu chưa có.
    /// </summary>
    public void GetEloAsync(Action<int, int, int, int> onResult, Action<string> onError)
    {
        if (!_config.IsValid)
        {
            onError?.Invoke("PlayFab chưa được cấu hình.");
            return;
        }

        var req = new PlayFab.ClientModels.GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string> { EloStatisticName, WinsStatisticName, LossesStatisticName, TotalGamesStatisticName }
        };

        var runner = MonoBehaviour.FindFirstObjectByType<MonoBehaviour>();
        if (runner == null)
        {
            onError?.Invoke("Không tìm thấy MonoBehaviour.");
            return;
        }

        runner.StartCoroutine(DoGetStats(req, onResult, onError));
    }

    private IEnumerator DoGetStats(PlayFab.ClientModels.GetPlayerStatisticsRequest req, Action<int, int, int, int> onResult, Action<string> onError)
    {
        bool done = false;
        string errorMsg = null;
        int elo = 0, wins = 0, losses = 0, totalGames = 0;

        // Use PlayFab SDK to get player statistics
        PlayFabClientAPI.GetPlayerStatistics(req,
            resp =>
            {
                try
                {
                    if (resp?.Statistics != null)
                    {
                        foreach (var s in resp.Statistics)
                        {
                            switch (s.StatisticName)
                            {
                                case EloStatisticName: elo = s.Value; break;
                                case WinsStatisticName: wins = s.Value; break;
                                case LossesStatisticName: losses = s.Value; break;
                                case TotalGamesStatisticName: totalGames = s.Value; break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    errorMsg = e.Message;
                }
                done = true;
            },
            error =>
            {
                errorMsg = error.GenerateErrorReport();
                done = true;
            });

        while (!done) yield return null;

        if (errorMsg != null)
            onError?.Invoke(errorMsg);
        else
            onResult?.Invoke(elo, wins, losses, totalGames);
    }

    /// <summary>
    /// Cập nhật ELO và thống kê sau trận đấu. Mỗi client tự gọi với kết quả của mình.
    /// </summary>
    public void UpdateEloAfterMatchAsync(int newElo, int wins, int losses, int totalGames, Action onSuccess, Action<string> onError)
    {
        if (!_config.IsValid)
        {
            onError?.Invoke("PlayFab chưa được cấu hình.");
            return;
        }

        var req = new PlayFab.ClientModels.UpdatePlayerStatisticsRequest
        {
            Statistics = new List<PlayFab.ClientModels.StatisticUpdate>
            {
                new PlayFab.ClientModels.StatisticUpdate { StatisticName = EloStatisticName, Value = newElo },
                new PlayFab.ClientModels.StatisticUpdate { StatisticName = WinsStatisticName, Value = wins },
                new PlayFab.ClientModels.StatisticUpdate { StatisticName = LossesStatisticName, Value = losses },
                new PlayFab.ClientModels.StatisticUpdate { StatisticName = TotalGamesStatisticName, Value = totalGames }
            }
        };

        var runner = MonoBehaviour.FindFirstObjectByType<MonoBehaviour>();
        if (runner == null)
        {
            onError?.Invoke("Không tìm thấy MonoBehaviour.");
            return;
        }

        runner.StartCoroutine(DoUpdateStats(req, onSuccess, onError));
    }

    private IEnumerator DoUpdateStats(PlayFab.ClientModels.UpdatePlayerStatisticsRequest req, Action onSuccess, Action<string> onError)
    {
        const int maxAttempts = 3;
        int attempt = 0;
        bool success = false;
        string errorMsg = null;

        while (attempt < maxAttempts && !success)
        {
            attempt++;
            bool done = false;
            errorMsg = null;

            PlayFabClientAPI.UpdatePlayerStatistics(req,
                resp =>
                {
                    success = true;
                    done = true;
                },
                error =>
                {
                    errorMsg = error.GenerateErrorReport();
                    done = true;
                });

            while (!done) yield return null;

            // If failed due to concurrent conflict, retry with backoff
            if (!success && !string.IsNullOrEmpty(errorMsg) && errorMsg.ToLowerInvariant().Contains("conflict"))
            {
                if (attempt < maxAttempts)
                {
                    float backoff = 0.5f * attempt; // 0.5s, 1s, ...
                    yield return new WaitForSeconds(backoff);
                    continue; // retry
                }
            }
            break;
        }

        if (success)
        {
            // Ép cập nhật bảng xếp hạng ngay sau khi đổi Elo
            if (GameServices.Instance?.Leaderboard != null)
                GameServices.Instance.Leaderboard.ForceUpdateLeaderboard();

            onSuccess?.Invoke();
        }
        else
            onError?.Invoke(errorMsg ?? "Cập nhật ELO thất bại.");
    }
}

public static class PlayFabEventSender
{
    private static float _lastSent = 0f;
    private const float MinInterval = 1f; // giây

    public static bool TrySend(Action send)
    {
        if (Time.realtimeSinceStartup - _lastSent < MinInterval) return false;
        _lastSent = Time.realtimeSinceStartup;
        send();
        return true;
      }
  }
