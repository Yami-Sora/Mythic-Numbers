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
        // Tìm GameManagerNet đang hoạt động
        if (GameManagerNet.Instance != null)
        {
            Debug.Log("Gửi yêu cầu Restart...");
            GameManagerNet.Instance.RequestRestart();
            GameRefereeNet.Instance.ResultPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Không tìm thấy GameManager để restart!");
        }
    }

}

