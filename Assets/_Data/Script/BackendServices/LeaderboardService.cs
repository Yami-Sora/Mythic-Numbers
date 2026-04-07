using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public struct LeaderboardEntry
{
    public int Position;
    public string PlayFabId;
    public string DisplayName;
    public int Elo;
}

/// <summary>
/// Lấy bảng xếp hạng ELO từ PlayFab.
/// </summary>
public class LeaderboardService
{
    private readonly PlayFabConfig _config;

    public LeaderboardService(PlayFabConfig config)
    {
        _config = config;
    }

    public void GetTopPlayersAsync(int count, Action<List<LeaderboardEntry>> onResult, Action<string> onError)
    {
        if (!_config.IsValid)
        {
            onError?.Invoke("PlayFab chưa được cấu hình.");
            return;
        }

        var req = new PlayFab.ClientModels.GetLeaderboardRequest
        {
            StatisticName = PlayerDataService.EloStatisticName,
            StartPosition = 0,
            MaxResultsCount = count
        };

        var runner = MonoBehaviour.FindFirstObjectByType<MonoBehaviour>();
        if (runner == null)
        {
            onError?.Invoke("Không tìm thấy MonoBehaviour.");
            return;
        }

        runner.StartCoroutine(DoGetLeaderboard(req, onResult, onError));
    }

    private IEnumerator DoGetLeaderboard(PlayFab.ClientModels.GetLeaderboardRequest req, Action<List<LeaderboardEntry>> onResult, Action<string> onError)
    {
        bool done = false;
        string errorMsg = null;
        List<LeaderboardEntry> list = null;

        PlayFabClientAPI.GetLeaderboard(req,
            resp =>
            {
                try
                {
                    if (resp?.Leaderboard != null)
                    {
                        list = new List<LeaderboardEntry>();
                        foreach (var e in resp.Leaderboard)
                        {
                            list.Add(new LeaderboardEntry
                            {
                                Position = e.Position,
                                PlayFabId = e.PlayFabId ?? "",
                                DisplayName = !string.IsNullOrEmpty(e.DisplayName) ? e.DisplayName : ShortenPlayFabId(e.PlayFabId),
                                Elo = e.StatValue
                            });
                        }
                    }
                    else
                        list = new List<LeaderboardEntry>();
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
            onResult?.Invoke(list ?? new List<LeaderboardEntry>());
    }

    private static string ShortenPlayFabId(string id)
    {
        if (string.IsNullOrEmpty(id)) return "???";
        return id.Length > 8 ? id.Substring(0, 8) + "..." : id;
    }
}
