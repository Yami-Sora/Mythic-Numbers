using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePlayController : MonoBehaviour
{
    public void QuitToMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }

    public void RestartGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[GamePlayController] Không tìm thấy GameManager để restart!");
            return;
        }
        Debug.Log("[GamePlayController] Gửi yêu cầu Restart...");
        GameManager.Instance.RestartGame();

        if (GameReferee.Instance != null && GameReferee.Instance.ResultPanel != null)
        {
            GameReferee.Instance.ResultPanel.SetActive(false);
        }
    }
}
