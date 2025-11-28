using Fusion;
using UnityEngine;

public class FusionConnectionStatus : MonoBehaviour
{
    private NetworkRunner _runner;

    private void Start()
    {
        // ✅ QUAN TRỌNG: Giữ script này sống sót khi chuyển từ Menu sang Game
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Tự động tìm runner nếu chưa có
        if (_runner == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
        }
    }

    void OnGUI()
    {
        // Nếu chưa có Runner thì hiện thông báo nhỏ
        if (_runner == null) return;

        // Tạo style cho chữ dễ đọc hơn
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.white;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;

        // Gom thông tin lại
        string status = "Disconnected";
        if (_runner.IsCloudReady) status = "Cloud Ready";
        if (_runner.IsConnectedToServer) status = "Connected";

        string mode = "Unknown";
        if (_runner.GameMode == GameMode.Single) mode = "Offline (Single)";
        else if (_runner.IsServer) mode = "Host";
        else if (_runner.IsClient) mode = "Client";

        // Vẽ bảng thông tin
        GUI.Box(new Rect(10, 10, 350, 130), "Network Debug Info");

        GUI.Label(new Rect(20, 30, 330, 20), $"Status: {status}", style);
        GUI.Label(new Rect(20, 50, 330, 20), $"Mode: {mode}", style);

        if (_runner.SessionInfo.IsValid)
        {
            GUI.Label(new Rect(20, 70, 330, 20), $"Room: {_runner.SessionInfo.Name}");
            GUI.Label(new Rect(20, 90, 330, 20), $"Region: {_runner.SessionInfo.Region}");
            GUI.Label(new Rect(20, 110, 330, 20), $"Players: {_runner.SessionInfo.PlayerCount}/{_runner.SessionInfo.MaxPlayers}");
        }
        else
        {
            GUI.Label(new Rect(20, 70, 330, 20), "Room: Creating/Joining...");
        }
    }
}