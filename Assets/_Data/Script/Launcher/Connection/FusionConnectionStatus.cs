using Fusion;
using UnityEngine;

public class FusionConnectionStatus : MonoBehaviour
{
    private NetworkRunner runner;

    private void Start()
    {
        // ✅ QUAN TRỌNG: Giữ script này sống sót khi chuyển từ Menu sang Game
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Tự động tìm runner nếu chưa có
        if (runner == null)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
        }
    }

    void OnGUI()
    {
        // Nếu chưa có Runner thì hiện thông báo nhỏ
        if (runner == null) return;

        // Tạo style cho chữ dễ đọc hơn
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.white;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;

        // Gom thông tin lại
        string status = "Disconnected";
        if (runner.IsCloudReady) status = "Cloud Ready";
        if (runner.IsConnectedToServer) status = "Connected";

        string mode = "Unknown";
        if (runner.GameMode == GameMode.Single) mode = "Offline (Single)";
        else if (runner.IsServer) mode = "Host";
        else if (runner.IsClient) mode = "Client";

        // Vẽ bảng thông tin
        GUI.Box(new Rect(10, 10, 350, 130), "Network Debug Info");

        GUI.Label(new Rect(20, 30, 330, 20), $"Status: {status}", style);
        GUI.Label(new Rect(20, 50, 330, 20), $"Mode: {mode}", style);

        if (runner.SessionInfo.IsValid)
        {
            GUI.Label(new Rect(20, 70, 330, 20), $"Room: {runner.SessionInfo.Name}");
            GUI.Label(new Rect(20, 90, 330, 20), $"Region: {runner.SessionInfo.Region}");
            GUI.Label(new Rect(20, 110, 330, 20), $"Players: {runner.SessionInfo.PlayerCount}/{runner.SessionInfo.MaxPlayers}");
        }
        else
        {
            GUI.Label(new Rect(20, 70, 330, 20), "Room: Creating/Joining...");
        }
    }
}