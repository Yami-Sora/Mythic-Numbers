using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleLauncher : MonoBehaviour
{
    private void Awake()
    {
        Application.runInBackground = true;
    }

    public void OnPlayOfflineClicked()
    {
        Debug.Log("Đang vào chế độ Offline...");
        if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.SaveGameData();
        StartGame();
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

    private void StartGame()
    {
        // 0. BẢO HIỂM DỮ LIỆU: Cập nhật lại LocalDeckContext trước khi vào trận đấu
        // Đảm bảo chỉ số mới nhất từ việc khảm Gem sẽ được nạp thẳng vào máy chủ trận đấu
        if (DeckManager.Instance != null && DeckManager.Instance.currentDeck != null)
        {
            LocalDeckContext.SetDeck(DeckManager.Instance.currentDeck);
        }

        // 1. Dọn dẹp GameManager cũ nếu còn sót lại từ lần chơi trước
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        // 2. Chuyển scene thuần túy
        SceneManager.LoadScene(1);
    }
}