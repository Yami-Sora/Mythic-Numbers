using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;

    private void Start()
    {
        //Đăng ký nhận callback
        runner = GetComponent<NetworkRunner>();
        if(runner == null ) runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null) runner.AddCallbacks(this);
    }
    // --- KHI RUNNER BỊ TẮT (DO HOST THOÁT HOẶC MẤT MẠNG) ---
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Runner Shutdown! Lý do: {shutdownReason}");

        // 1. Xóa Runner cũ để dọn dẹp bộ nhớ
        // (Lưu ý: Nếu object này nằm trên runner thì cẩn thận kẻo bị destroy theo)

        // 2. Mở lại màn hình Menu/Launcher
        // Đảm bảo trỏ chuột được bật lại
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Load lại Scene 0 (Menu)
        SceneManager.LoadScene("MenuScene");
    }

    // --- CÁC HÀM KHÁC BẮT BUỘC CỦA INTERFACE (ĐỂ TRỐNG CŨNG ĐƯỢC) ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}