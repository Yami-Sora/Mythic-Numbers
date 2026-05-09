using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(FusionObjectPool))]
public class NetworkAppManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkAppManager Instance;

    [Header("Network Prefabs")]
    [SerializeField] private NetworkObject gameManagerNetPrefab;
    [SerializeField] private NetworkObject gameRefereeNetPrefab;

    [Header("Scene References")]
    [SerializeField] private Transform[] slots;
    [SerializeField] private Transform leftHandPos;
    [SerializeField] private Transform rightHandPos;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private Transform mainCanvas;

    [Header("Referee UI References")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    private NetworkRunner runner;
    private FusionObjectPool _objectProvider;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Lấy component Pool đã gắn trên cùng GameObject
        _objectProvider = GetComponent<FusionObjectPool>();
    }

    private void Start()
    {
        // 1. Tìm Runner đã tồn tại (do Scene 0 mang sang)
        runner = FindFirstObjectByType<NetworkRunner>();

        // 2. Đăng ký nhận sự kiện
        if (runner != null)
        {
            runner.AddCallbacks(this);
            // Nếu là Server (Host/Offline) -> Spawn Manager ngay
            if (runner.IsServer)
            {
                SpawnGameManagers();
            }
        }

        if (resultPanel) resultPanel.SetActive(false);
    }

    public async void StartGame(NetworkRunner runner, GameMode mode)
    {
        this.runner = runner;

        var scene = SceneManager.GetActiveScene();
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid())
        {
            sceneInfo.AddSceneRef(SceneRef.FromIndex(scene.buildIndex));
        }

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            Scene = sceneInfo,
            //Gán Pool vào đây để Fusion dùng nó thay vì Instantiate/Destroy
            ObjectProvider = _objectProvider
        });
    }



    private bool _hasSpawnedManagers = false;

    private void SpawnGameManagers()
    {
        // Thêm cờ kiểm tra để chống spam spawn do bất đồng bộ (Instance chưa kịp gán)
        if (_hasSpawnedManagers) return;
        _hasSpawnedManagers = true;

        // Kiểm tra xem đã spawn chưa để tránh trùng lặp
        if (GameManagerNet.Instance == null)
        {
            Debug.Log("AppManager: Scene đã load, tiến hành Spawn Manager...");
            runner.Spawn(gameManagerNetPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
            runner.Spawn(gameRefereeNetPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
        }
    }

    public void OnUIManagerReady()
    {
        Debug.Log("[AppManager] Tìm thấy GameUIManager -> Đang chuyển giao tham chiếu UI...");
        SetupUIManager();
    }

    private void SetupUIManager()
    {
        InGameUIManager.Instance.SetupReferences(slots, leftHandPos, rightHandPos, turnText, mainCanvas);
        InGameUIManager.Instance.ResetBoardUI();
        // Đảm bảo bài hiển thị đúng vị trí khi UI mới được cấu hình (client reconnect)
        if (GameManagerNet.Instance != null)
            GameManagerNet.Instance.RefreshAllCards();
    }

    // Hàm gán UI cho Referee
    public void SetupRefereeUI(GameRefereeNet referee)
    {
        if (resultPanel == null || resultText == null)
        {
            Debug.LogError("AppManager: Quên kéo ResultPanel hoặc ResultText vào Inspector rồi!");
            return;
        }

        referee.SetUIRefs(resultPanel, resultText);
        Debug.Log("AppManager: Đã gán UI cho GameRefereeNet.");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        //// Dự phòng: Nếu Start chưa chạy kịp hoặc logic load scene bất đồng bộ
        if (runner.IsServer && GameManagerNet.Instance == null)
        {
            SpawnGameManagers();
        }
    }

    // --- KẾ THỪA TỪ INetworkRunnerCallbacks ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
#pragma warning disable UNT0006 
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
#pragma warning restore UNT0006
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}