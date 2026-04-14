using UnityEngine;
using PlayFab; // Phải có cái này

public class GameServices : MonoBehaviour
{
    public static GameServices Instance { get; private set; }
    [SerializeField] private PlayFabConfig playFabConfig;

    public AuthService Auth { get; private set; }
    public PlayerDataService PlayerData { get; private set; }
    public LeaderboardService Leaderboard { get; private set; } 

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (playFabConfig == null)
            playFabConfig = Resources.Load<PlayFabConfig>("PlayFabConfig");

        // QUAN TRỌNG: Thiết lập TitleId cho toàn bộ SDK
        if (playFabConfig != null)
            PlayFabSettings.staticSettings.TitleId = playFabConfig.titleId;

        Auth = new AuthService(playFabConfig);
        PlayerData = new PlayerDataService(playFabConfig);
        Leaderboard = new LeaderboardService(playFabConfig);

        if (playFabConfig != null && playFabConfig.IsValid)
            TestConnection();
    }

    private void TestConnection()
    {
        Auth.LoginAnonymous(
            () =>
            {
                Debug.Log("<color=green>[PlayFab] Kết nối thành công!</color>");

                // === THÊM ĐÚNG 2 DÒNG NÀY VÀO ĐÂY ===
                if (PlayFabDataManager.Instance != null)
                {
                    PlayFabDataManager.Instance.FetchVirtualCurrencies(); // Kéo tiền về UI
                    PlayFabDataManager.Instance.LoadGameData();           // Kéo túi đồ và bài về UI
                }
                // ===================================

                // Lấy ELO hiện tại và in ra để debug (Đoạn code cũ của sếp giữ nguyên)
                PlayerData.GetEloAsync(
                    (elo, wins, losses, totalGames) =>
                    {
                        Debug.Log($"[PlayFab] Current stats - ELO: {elo}, Wins: {wins}, Losses: {losses}, TotalGames: {totalGames}");
                    },
                    err => Debug.LogWarning("[PlayFab] Lỗi khi lấy thống kê người chơi: " + err)
                );
            },
            err => Debug.LogError("[PlayFab] Kết nối thất bại: " + err)
        );
    }
}