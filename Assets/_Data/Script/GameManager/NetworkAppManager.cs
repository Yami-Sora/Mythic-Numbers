using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;

// Thêm interface INetworkRunnerCallbacks để biết khi nào Scene load xong
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
    // Flag kiểm tra xem đã cấu hình UI xong chưa để tránh gọi liên tục trong Update
    private bool _uiConfigured = false;
    private bool _refereeConfigured = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
    private void SpawnGameManagers()
    {
        // Kiểm tra xem đã spawn chưa để tránh trùng lặp
        if (GameManagerNet.Instance == null)
        {
            Debug.Log("AppManager: Scene đã load, tiến hành Spawn Manager...");
            runner.Spawn(gameManagerNetPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
            runner.Spawn(gameRefereeNetPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
        }
    }

    // ✅ VÒNG LẶP CẤU HÌNH UI (Đã sửa đổi)
    private void FixedUpdate()
    {
        // 1. Cấu hình cho GameUIManager (Thay vì GameManagerNet như cũ)
        // Chúng ta kiểm tra _uiConfigured để chỉ thực hiện việc này 1 lần
        if (!_uiConfigured && GameUIManager.Instance != null)
        {
            Debug.Log("[AppManager] Tìm thấy GameUIManager -> Đang chuyển giao tham chiếu UI...");
            SetupUIManager();
            _uiConfigured = true;
        }

        // 2. Cấu hình cho Referee (Giữ nguyên nếu Referee chưa tách file)
        if (!_refereeConfigured && GameRefereeNet.Instance != null)
        {
            SetupRefereeUI(GameRefereeNet.Instance);
            _refereeConfigured = true;
        }
    }

    private void SetupUIManager()
    {
        // Gọi hàm SetupReferences bên GameUIManager
        GameUIManager.Instance.SetupReferences(slots, leftHandPos, rightHandPos, turnText, mainCanvas);

        // Reset lại UI bàn cờ cho sạch sẽ
        GameUIManager.Instance.ResetBoardUI();
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
        // Dự phòng: Nếu Start chưa chạy kịp hoặc logic load scene bất đồng bộ
        if (runner.IsServer && GameManagerNet.Instance == null)
        {
            SpawnGameManagers();
        }
    }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
#pragma warning disable UNT0006 // Tắt cảnh báo sai về Unity Message Signature

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