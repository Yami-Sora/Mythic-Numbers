using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GamePlayController : MonoBehaviour
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
            Debug.LogError("[GamePlayController] Không tìm thấy GameManagerNet để restart!");
            return;
        }
        Debug.Log("[GamePlayController] Gửi yêu cầu Restart...");
        GameManagerNet.Instance.RPC_Restart();

        if (GameRefereeNet.Instance != null && GameRefereeNet.Instance.ResultPanel != null)
        {
            GameRefereeNet.Instance.ResultPanel.SetActive(false);
        }
    }
}

