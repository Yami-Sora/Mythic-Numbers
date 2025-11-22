using UnityEngine;
using Fusion;
using System.Threading.Tasks;

public class NetworkLauncher : MonoBehaviour
{
    public GameObject gameManagerPrefab; // Prefab chứa GameManager

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

        // Nếu là người tạo phòng, sinh ra GameManager
        if (runner.IsSharedModeMasterClient)
        {
            runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
        }
    }
}
