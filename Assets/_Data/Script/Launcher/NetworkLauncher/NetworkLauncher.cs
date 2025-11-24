using UnityEngine;
using Fusion;
using System.Threading.Tasks;

public class NetworkLauncher : MonoBehaviour
{

    //async void Start()
    //{
    //    await StartGame(GameMode.Shared);
    //}

    // Gọi hàm này khi bấm nút "Play Online"
    public async void OnPlayOnlineClicked()
    {
        await StartGame(GameMode.Shared);
    }

    // Gọi hàm này khi bấm nút "Play Solo (Offline)"
    public async void OnPlayOfflineClicked()
    {
        // GameMode.Single chạy offline hoàn toàn
        await StartGame(GameMode.Single);
    }
    async Task StartGame(GameMode mode)
    {
        // Tạo Runner (môi trường mạng)
        NetworkRunner runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        // Kết nối tới phòng tên "TestRoom"
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        // ✅ KẾT NỐI VỚI NETWORK APP MANAGER
        // Chỉ Master Client (người tạo phòng) mới có quyền sinh ra các Network Object quản lý
        if (runner.IsSharedModeMasterClient || runner.GameMode == GameMode.Single)
        {
            NetworkAppManager appManager = FindFirstObjectByType<NetworkAppManager>();
            if (appManager != null)
            {
                // Tự động bật AI nếu chơi Single
                if (mode == GameMode.Single)
                {
                    // (Tùy chọn) Bạn có thể truyền cờ vào đây để báo Manager bật AI
                    Debug.Log("Chế độ Offline: Tự động kích hoạt AI");
                }
                appManager.StartGame(runner);
            }
        }
    }
}
