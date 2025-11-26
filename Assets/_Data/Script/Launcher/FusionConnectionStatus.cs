using Fusion;
using UnityEngine;

public class FusionConnectionStatus : MonoBehaviour
{
    private NetworkRunner _runner;

    void Update()
    {
        // Tự động tìm runner nếu chưa có
        if (_runner == null) _runner = FindFirstObjectByType<NetworkRunner>();
    }

    void OnGUI()
    {
        if (_runner == null)
        {
            GUI.Label(new Rect(10, 10, 200, 20), "Status: No Runner");
            return;
        }

        string status = "";
        if (_runner.IsCloudReady) status += "Cloud Ready | ";
        if (_runner.IsConnectedToServer) status += "Connected | ";

        // Hiển thị chi tiết
        GUI.Box(new Rect(10, 10, 300, 100), "Network Info");
        GUI.Label(new Rect(20, 30, 280, 20), $"State: {_runner.SessionInfo.IsValid}"); // Có tìm thấy phòng không?
        GUI.Label(new Rect(20, 50, 280, 20), $"Region: {_runner.SessionInfo.Region}");
        GUI.Label(new Rect(20, 70, 280, 20), $"Room: {_runner.SessionInfo.Name}");
        GUI.Label(new Rect(20, 90, 280, 20), $"Shutdown: {_runner.IsShutdown}");
    }
}