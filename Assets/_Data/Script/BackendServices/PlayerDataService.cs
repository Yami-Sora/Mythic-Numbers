using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    // Sự kiện được bắn ra khi ELO hoặc thống kê vừa được cập nhật thành công lên server.
    public event Action OnPlayerStatsUpdated;

    public PlayerDataService(PlayFabConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Lấy ELO hiện tại. Mặc định 0 nếu chưa có.
    /// Có thể dùng kiểu cũ (Callback) hoặc kiểu mới (Task).
    /// </summary>
    public async void GetEloAsync(Action<int, int, int, int> onResult, Action<string> onError)
    {
        try
        {
            var result = await GetEloTaskAsync();
            onResult?.Invoke(result.elo, result.wins, result.losses, result.totalGames);
        }
        catch (Exception e)
        {
            onError?.Invoke(e.Message);
        }
    }

    /// <summary>
    /// Hàm lấy Elo trả về Task (chuẩn Async).
    /// </summary>
    public Task<(int elo, int wins, int losses, int totalGames)> GetEloTaskAsync()
    {
        var tcs = new TaskCompletionSource<(int, int, int, int)>();

        if (!_config.IsValid)
        {
            tcs.TrySetException(new Exception("PlayFab chưa được cấu hình."));
            return tcs.Task;
        }

        var req = new PlayFab.ClientModels.GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string> { EloStatisticName, WinsStatisticName, LossesStatisticName, TotalGamesStatisticName }
        };

        PlayFabClientAPI.GetPlayerStatistics(req,
            resp =>
            {
                int elo = 0, wins = 0, losses = 0, totalGames = 0;
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
                tcs.TrySetResult((elo, wins, losses, totalGames));
            },
            error =>
            {
                tcs.TrySetException(new Exception(error.GenerateErrorReport()));
            });

        return tcs.Task;
    }

    /// <summary>
    /// Cập nhật ELO và thống kê sau trận đấu.
    /// </summary>
    public async void UpdateEloAfterMatchAsync(int newElo, int wins, int losses, int totalGames, Action onSuccess, Action<string> onError)
    {
        try
        {
            await UpdateEloTaskAsync(newElo, wins, losses, totalGames);
            onSuccess?.Invoke();
        }
        catch (Exception e)
        {
            onError?.Invoke(e.Message);
        }
    }

    /// <summary>
    /// Cập nhật Elo chuẩn Async Task (có retry).
    /// </summary>
    public async Task UpdateEloTaskAsync(int newElo, int wins, int losses, int totalGames)
    {
        if (!_config.IsValid)
            throw new Exception("PlayFab chưa được cấu hình.");

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

        const int maxAttempts = 3;
        int attempt = 0;

        while (attempt < maxAttempts)
        {
            attempt++;
            var tcs = new TaskCompletionSource<bool>();

            PlayFabClientAPI.UpdatePlayerStatistics(req,
                resp => tcs.TrySetResult(true),
                error => tcs.TrySetException(new Exception(error.GenerateErrorReport())));

            try
            {
                await tcs.Task;
                // Nếu update thành công, bắn sự kiện ra ngoài thay vì gọi trực tiếp UI manager
                OnPlayerStatsUpdated?.Invoke();
                return;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message.ToLowerInvariant();
                if (errorMsg.Contains("conflict") && attempt < maxAttempts)
                {
                    int backoffMs = 500 * attempt;
                    await Task.Delay(backoffMs);
                }
                else
                {
                    throw;
                }
            }
        }
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
