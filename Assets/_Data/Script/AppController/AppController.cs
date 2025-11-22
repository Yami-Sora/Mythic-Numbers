using UnityEngine;


public class AppController : MonoBehaviour
{
    public void QuitGame()
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
    public void RestartGame()
    {
        // Tìm GameManagerNet đang hoạt động
        if (GameManagerNet.Instance != null)
        {
            Debug.Log("Gửi yêu cầu Restart...");
            GameManagerNet.Instance.RequestRestart();
            GameRefereeNet.Instance.resultPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Không tìm thấy GameManager để restart!");
        }
    }

}

