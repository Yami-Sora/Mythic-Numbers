using UnityEngine;
using Fusion;
using System.Threading.Tasks;

public class NetworkLauncher : MonoBehaviour
{
    private void Awake()
    {
        // Đảm bảo App chạy ngầm để test 2 cửa sổ trên 1 máy
        Application.runInBackground = true;
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

        // 4. Bắt đầu Game và Load Scene 1
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom", // Hardcode để test hoặc Random tên

            // QUAN TRỌNG: Dòng này bảo Fusion tự động load Scene có Index 1 (PlayCardScene)
            // Khi Host load xong, Client sẽ tự động load theo!
            Scene = SceneRef.FromIndex(1),

            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
}