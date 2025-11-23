using UnityEngine;
using Fusion;
using System.Threading.Tasks;

public class NetworkLauncher : MonoBehaviour
{

    async void Start()
    {
        await StartGame(GameMode.Shared);
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
        if (runner.IsSharedModeMasterClient)
        {
            // Tìm NetworkAppManager đang có sẵn trong Scene
            NetworkAppManager appManager = FindFirstObjectByType<NetworkAppManager>();

            if (appManager != null)
            {
                Debug.Log("Master Client: Đang gọi NetworkAppManager để khởi tạo Game...");

                // Gọi hàm StartGame bên kia để nó Spawn cả GameManager lẫn Referee
                // VÀ quan trọng nhất là gán tham chiếu UI (SetSceneReferences)
                appManager.StartGame(runner);
            }
            else
            {
                Debug.LogError("LỖI: Không tìm thấy NetworkAppManager trong Scene! Hãy chắc chắn bạn đã tạo GameObject này.");
            }
        }
    }
}
