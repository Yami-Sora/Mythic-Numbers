using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;

    public void OnEnable()
    {
        // Tìm runner hiện tại để đăng ký callback
        if (runner == null) runner = FindFirstObjectByType<NetworkRunner>();

        // Đăng ký nhận thông báo từ Fusion
        if (runner != null) runner.AddCallbacks(this);
    }

    // OnDisable chạy mỗi khi GameObject/Script bị tắt hoặc hủy.
    // Bắt buộc phải hủy đăng ký ở đây để tránh lỗi Memory Leak.
    public void OnDisable()
    {
        if (runner != null) runner.RemoveCallbacks(this);
    }
    //Cho phép bên ngoài đăng ký Runner thủ công
    public void RegisterRunner(NetworkRunner runner)
    {
        // Nếu đã có runner cũ thì hủy đăng ký trước
        if (runner != null) runner.RemoveCallbacks(this);

        this.runner = runner;
        if (runner != null)
        {
            runner.AddCallbacks(this);
            Debug.Log("[ConnectionHandler] Đã đăng ký lắng nghe sự kiện từ Runner thành công!");
        }
    }
    // --- KHI RUNNER BỊ TẮT (DO HOST THOÁT HOẶC MẤT MẠNG) ---
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Runner Shutdown! Lý do: {shutdownReason}");
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPaused)
        {
            UnityEditor.EditorApplication.isPaused = false;
        }
#endif
        SceneManager.LoadScene("MenuScene");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) 
    { 
        Debug.Log($"Kết nối thất bại đến {remoteAddress}. Lý do: {reason}");
        runner.Shutdown();
    }
#pragma warning disable UNT0006 // Tắt cảnh báo sai về Unity Message Signature
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Đã ngắt kết nối từ Server! Lý do: {reason}");

        runner.Shutdown();
    }
    public void OnConnectedToServer(NetworkRunner runner) { }

#pragma warning restore UNT0006

    // --- CÁC HÀM KHÁC BẮT BUỘC CỦA INTERFACE (ĐỂ TRỐNG CŨNG ĐƯỢC) ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
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