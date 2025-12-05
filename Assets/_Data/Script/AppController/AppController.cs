using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;


public class AppController : MonoBehaviour
{
    public void QuitToMenu()
    {
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            // Lệnh này sẽ kích hoạt OnShutdown ở ConnectionHandler
            runner.Shutdown();
        }
        else
        {
            SceneManager.LoadScene("MenuScene");
        }
    }
    public void RestartGame()
    {
        if (GameManagerNet.Instance == null)
        {
            Debug.LogError("[AppController] Không tìm thấy GameManagerNet để restart!");
            return;
        }
        Debug.Log("[AppController] Gửi yêu cầu Restart...");
        GameManagerNet.Instance.RPC_Restart();

        if (GameRefereeNet.Instance != null && GameRefereeNet.Instance.ResultPanel != null)
        {
            GameRefereeNet.Instance.ResultPanel.SetActive(false);
        }
    }
}

