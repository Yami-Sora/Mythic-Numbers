using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using UnityEngine;
using PlayFab; // Thêm dòng này để truy cập PlayFab Settings nếu cần

public class MatchmakingService
{
    private const int EloBucketSize = 100;

    public async Task<bool> FindMatchAsync(NetworkRunner runner,
        FusionAppSettings photonAppSettings,
        INetworkSceneManager sceneManager,
        INetworkObjectProvider objectProvider,
        SceneRef scene,
        int playerTtl)
    {
        if (GameServices.Instance == null)
        {
            Debug.LogError("[Matchmaking] GameServices chưa có. Thêm GameServices vào scene.");
            return false;
        }

        // 1. Đăng nhập nếu chưa
        if (!GameServices.Instance.Auth.IsLoggedIn)
        {
            var loginSuccess = await GameServices.Instance.Auth.LoginAnonymousAsync();
            if (!loginSuccess)
            {
                Debug.LogWarning("[Matchmaking] Không đăng nhập được, dùng ELO 0.");
            }
        }

        // 2. Lấy ELO
        var elo = 0;
        var wins = 0;
        var losses = 0;
        var totalGames = 0;

        try
        {
            var statsResult = await GameServices.Instance.PlayerData.GetEloTaskAsync();
            elo = statsResult.elo;
            wins = statsResult.wins;
            losses = statsResult.losses;
            totalGames = statsResult.totalGames;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Matchmaking] Lấy thống kê thất bại: {e.Message}");
        }

        // 3. Lưu vào LauncherSessionSettings
        var settings = runner.gameObject.GetComponent<LauncherSessionSettings>();
        if (settings == null) settings = runner.gameObject.AddComponent<LauncherSessionSettings>();

        settings.PlayerTtl = Mathf.Clamp(playerTtl, 60, 120);
        settings.LocalPlayerData = new MatchPlayerData
        {
            // THAY ĐỔI Ở ĐÂY: Lấy PlayFabId từ AuthService hoặc biến lưu trữ của SDK
            PlayFabId = GameServices.Instance.Auth.UserId ?? "",
            Elo = elo,
            Wins = wins,
            Losses = losses,
            TotalGames = totalGames
        };

        // 4. SessionProperties
        int eloBucket = EloCalculator.GetEloBucket(elo, EloBucketSize);
        var sessionProps = new Dictionary<string, SessionProperty>
        {
            { "eloBucket", eloBucket },
            { "hostELO", elo }
        };

        var args = new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "MythicRoom",
            Scene = scene,
            SceneManager = sceneManager,
            CustomPhotonAppSettings = photonAppSettings,
            ObjectProvider = objectProvider,
            SessionProperties = sessionProps,
            MatchmakingMode = MatchmakingMode.FillRoom,
            PlayerCount = 2,
            IsOpen = true,
            IsVisible = true
        };

        var result = await runner.StartGame(args);
        return result.Ok;
    }
}