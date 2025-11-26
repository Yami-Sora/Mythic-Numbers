using UnityEngine;
using Fusion;
using System.Threading.Tasks;

public class NetworkLauncher : MonoBehaviour
{
    // Gọi hàm này khi bấm nút "Play Online"
    public async void OnPlayOnlineClicked()
    {
        GameUIManager.Instance.SetLauncherModeUI(true);
        // Tự động quyết định (Người đầu là Host, người sau là Client)
        await StartGame(GameMode.AutoHostOrClient);
    }

    // Gọi hàm này khi bấm nút "Play Solo (Offline)"
    public async void OnPlayOfflineClicked()
    {
        GameUIManager.Instance.SetLauncherModeUI(false);
        GameUIManager.Instance.SetLeftImageActive(true);
        // GameMode.Single chạy offline hoàn toàn
        await StartGame(GameMode.Single);
    }
    async Task StartGame(GameMode mode)
    {
        // Hủy Manager cũ nếu còn sót lại
        if (GameManagerNet.Instance != null)
        {
            Destroy(GameManagerNet.Instance.gameObject);
        }
        // Tạo Runner (môi trường mạng)
        // ✅ FIX: Tái sử dụng Runner nếu có, thay vì Destroy/Create liên tục
        NetworkRunner runner = GetComponent<NetworkRunner>();
        if (runner == null)
        {
            runner = gameObject.AddComponent<NetworkRunner>();
        }

        // Đảm bảo Runner sạch sẽ trước khi Start
        runner.ProvideInput = true;

        // Kết nối tới phòng tên "TestRoom"
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });


        // ✅ ĐOẠN NÀY QUAN TRỌNG:
        // Khi người thứ 2 vào, mode của họ là Client -> runner.IsServer = false.
        // Họ sẽ KHÔNG chạy vào trong if này -> KHÔNG Spawn GameManagerNet (Đúng logic).
        // Họ sẽ chờ Host (người 1) spawn và đồng bộ về máy họ.
        if (runner.IsServer || runner.GameMode == GameMode.Single)
        {
            NetworkAppManager appManager = FindFirstObjectByType<NetworkAppManager>();
            if (appManager != null)
            {
                appManager.StartGame(runner);
            }
        }
    }
}
