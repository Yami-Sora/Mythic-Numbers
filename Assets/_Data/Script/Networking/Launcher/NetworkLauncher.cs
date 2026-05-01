using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using UnityEngine;

public class NetworkLauncher : MonoBehaviour
{
    [Header("Security")]
    [SerializeField] private PhotonConfig photonConfig;

    [Header("Reconnect / TTL (seconds)")]
    [Tooltip("Time (in seconds) to keep a disconnected player 'alive' so they can reconnect. Recommended: 60-120")]
    [SerializeField] private int playerTtl = 90;

    private readonly MatchmakingService _matchmaking = new MatchmakingService();

    private void Awake()
    {
        Application.runInBackground = true;
    }

    public async void OnPlayOnlineClicked()
    {
        Debug.Log("Đang kết nối Online (Matchmaking theo ELO)...");
        await StartGameOnline();
    }

    public async void OnPlayOfflineClicked()
    {
        Debug.Log("Đang vào chế độ Offline...");
        if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.SaveGameData();
        await StartGame(GameMode.Single);
    }

    public void OnQuitClicked()
    {
        // Log ra console để biết nút hoạt động khi ở trong Editor
        Debug.Log("Application Quit called!");

        // Lệnh thoát game (chỉ chạy khi đã Build ra file .exe/.apk)
        Application.Quit();

        // Nếu đang chạy trong Editor thì dừng Play mode (tiện để test)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    async Task StartGame(GameMode mode)
    {
        // 0. BẢO HIỂM DỮ LIỆU: Cập nhật lại LocalDeckContext trước khi vào trận đấu
        // Đảm bảo chỉ số mới nhất từ việc khảm Gem sẽ được nạp thẳng vào máy chủ trận đấu
        if (DeckManager.Instance != null && DeckManager.Instance.currentDeck != null)
        {
            LocalDeckContext.SetDeck(DeckManager.Instance.currentDeck);
        }

        // 1. Dọn dẹp GameManager cũ nếu còn sót lại từ lần chơi trước
        if (GameManagerNet.Instance != null)
        {
            Destroy(GameManagerNet.Instance.gameObject);
        }
        // 2. Tạo hoặc tìm Runner
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
        {
            // Tạo một GameObject tạm để chứa Runner
            GameObject go = new GameObject("NetworkRunner");
            runner = go.AddComponent<NetworkRunner>();
        }

        var pool = runner.GetComponent<FusionObjectPool>();
        if (pool == null)
        {
            pool = runner.gameObject.AddComponent<FusionObjectPool>();
        }

        // Attach or update session settings so server code can read TTL after start
        var settings = runner.gameObject.GetComponent<LauncherSessionSettings>();
        if (settings == null) settings = runner.gameObject.AddComponent<LauncherSessionSettings>();
        settings.PlayerTtl = Mathf.Clamp(playerTtl, 60, 120);

        // Đảm bảo Runner không đang chạy phiên cũ
        if (runner.IsRunning)
        {
            await runner.Shutdown();
        }
        // Cấu hình Runner
        runner.ProvideInput = true;

        // 3. Đăng ký ConnectionHandler (nếu có) để xử lý ngắt kết nối
        ConnectionHandler handler = GetComponentInChildren<ConnectionHandler>();
        if (handler == null) handler = FindFirstObjectByType<ConnectionHandler>();
        if (handler != null) handler.RegisterRunner(runner);

        var customAppSettings = new FusionAppSettings
        {
            AppIdFusion = photonConfig.appId,
            FixedRegion = "asia",
        };

        if (mode == GameMode.AutoHostOrClient && GameServices.Instance != null)
        {
            // Matchmaking theo ELO
            var ok = await _matchmaking.FindMatchAsync(
                runner,
                customAppSettings,
                runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                pool,
                SceneRef.FromIndex(1),
                playerTtl);
            if (!ok)
                Debug.LogError("[NetworkLauncher] Matchmaking thất bại.");
        }
        else
        {
            // Offline hoặc không có GameServices
            await runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = mode == GameMode.Single ? "Offline" : "TestRoom",
                Scene = SceneRef.FromIndex(1),
                SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                CustomPhotonAppSettings = customAppSettings,
                ObjectProvider = pool,
            });
        }
    }

    private async Task StartGameOnline()
    {
        await StartGame(GameMode.AutoHostOrClient);
    }
}