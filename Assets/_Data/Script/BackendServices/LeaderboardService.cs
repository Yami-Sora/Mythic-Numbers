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

    private List<LeaderboardEntry> _cachedLeaderboard = new List<LeaderboardEntry>();
    private bool _hasData = false;

    public LeaderboardService(PlayFabConfig config)
    {
        _config = config;
    }

    public List<LeaderboardEntry> GetCachedLeaderboard() => _cachedLeaderboard;
    public bool HasData() => _hasData;

    public event Action OnLeaderboardUpdated;

    public void ForceUpdateLeaderboard()
    {
        if (!_config.IsValid) return;

        var req = new PlayFab.ClientModels.GetLeaderboardRequest
        {
            StatisticName = PlayerDataService.EloStatisticName,
            StartPosition = 0,
            MaxResultsCount = 10
        };

        // Ép chạy Coroutine để cập nhật cache ngay lập tức
        GameServices.Instance.StartCoroutine(DoGetLeaderboard(req, 
            res => {
                Debug.Log("<color=green>[Leaderboard] Đã cập nhật bảng xếp hạng mới nhất!</color>");
                OnLeaderboardUpdated?.Invoke();
            }, 
            err => Debug.LogWarning("[Leaderboard] Cập nhật cưỡng bức thất bại: " + err)));
    }

    public IEnumerator AutoFetchRoutine()
    {
        if (!_config.IsValid) yield break;

        var req = new PlayFab.ClientModels.GetLeaderboardRequest
        {
            StatisticName = PlayerDataService.EloStatisticName,
            StartPosition = 0,
            MaxResultsCount = 10
        };

        while (true)
        {
            bool done = false;
            PlayFabClientAPI.GetLeaderboard(req,
                resp =>
                {
                    if (resp?.Leaderboard != null)
                    {
                        var list = new List<LeaderboardEntry>();
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
                        _cachedLeaderboard = list;
                        _hasData = true;
                        OnLeaderboardUpdated?.Invoke();
                    }
                    done = true;
                },
                error => { done = true; });

            while (!done) yield return null;

            // Nghỉ 5 phút = 300 giây rồi lấy tiếp
            yield return new WaitForSeconds(300f);
        }
    }

    public void GetTopPlayersAsync(int count, Action<List<LeaderboardEntry>> onResult, Action<string> onError)
    {
        if (!_config.IsValid)
        {
            onError?.Invoke("PlayFab chưa được cấu hình.");
            return;
        }

        // Nếu đã có cache ngầm từ AutoFetch thì trả về luôn cho mượt!
        if (_hasData)
        {
            onResult?.Invoke(_cachedLeaderboard);
            return;
        }

        // Nếu xui bấm đúng lúc game mới bật chưa kịp lấy, thì ép lấy ngay
        var req = new PlayFab.ClientModels.GetLeaderboardRequest
        {
            StatisticName = PlayerDataService.EloStatisticName,
            StartPosition = 0,
            MaxResultsCount = count
        };

        var runner = GameServices.Instance;
        if (runner == null)
        {
            onError?.Invoke("Không tìm thấy GameServices.");
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
        {
            onError?.Invoke(errorMsg);
        }
        else
        {
            if (list != null)
            {
                _cachedLeaderboard = list;
                _hasData = true;
            }
            onResult?.Invoke(list ?? new List<LeaderboardEntry>());
        }
    }

    private static string ShortenPlayFabId(string id)
    {
        if (string.IsNullOrEmpty(id)) return "???";
        return id.Length > 8 ? id.Substring(0, 8) + "..." : id;
    }
}
