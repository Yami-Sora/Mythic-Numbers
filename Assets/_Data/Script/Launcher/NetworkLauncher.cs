using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class NetworkLauncher : MonoBehaviour
{
    private void Start()
    {
        // Đảm bảo chuột luôn hiển thị ở Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public async void OnPlayOnlineClicked()
    {
        Debug.Log("Đang kết nối Online...");
        // Dùng AutoHostOrClient để tự động chọn Host hoặc Join
        await StartGame(GameMode.AutoHostOrClient);
    }

    public async void OnPlayOfflineClicked()
    {
        Debug.Log("Đang vào chế độ Offline...");
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
        // 1. Tạo hoặc tìm Runner
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
        {
            // Tạo một GameObject tạm để chứa Runner
            GameObject go = new GameObject("NetworkRunner");
            runner = go.AddComponent<NetworkRunner>();
        }

        // 2. Cấu hình Runner
        runner.ProvideInput = true;

        // 3. Bắt đầu Game và Load Scene 1
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom", // Hardcode để test hoặc Random tên

            // QUAN TRỌNG: Dòng này bảo Fusion tự động load Scene có Index 1 (PlayCardScene)
            // Khi Host load xong, Client sẽ tự động load theo!
            Scene = SceneRef.FromIndex(1),

            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        // 4. Lưu ý: Code Spawn Manager cũ đã bị xóa.
        // Tại sao? Vì khi load sang Scene 1, NetworkAppManager có sẵn trong Scene 1 
        // sẽ tự động chạy logic của nó
    }
}