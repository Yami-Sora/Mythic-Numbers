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

    public static event System.Action OnBackendReady;

    private async void TestConnection()
    {
        var loginSuccess = await Auth.LoginAnonymousAsync();
        if (loginSuccess)
        {
            Debug.Log("<color=green>[PlayFab] Kết nối thành công!</color>");

            // === BẬT TÍNH NĂNG TỰ ĐỘNG CẬP NHẬT RANKING MỖI 5 PHÚT ===
            StartCoroutine(Leaderboard.AutoFetchRoutine());

            try
            {
                // Lấy ELO hiện tại và in ra để debug
                var result = await PlayerData.GetEloTaskAsync();
                Debug.Log($"[PlayFab] Current stats - ELO: {result.elo}, Wins: {result.wins}, Losses: {result.losses}, TotalGames: {result.totalGames}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[PlayFab] Lỗi khi lấy thống kê người chơi: " + e.Message);
            }

            // Bắn tín hiệu ra ngoài để UI hoặc DataManager khác tự bắt đầu kéo dữ liệu
            OnBackendReady?.Invoke();
        }
        else
        {
            Debug.LogError("[PlayFab] Kết nối thất bại.");
        }
    }
}