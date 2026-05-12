using UnityEngine;

public class GamePlayController : MonoBehaviour
{
    public static GamePlayController Instance { get; private set; }

    [Header("Root References")]
    [Tooltip("Kéo ----PLAYCARD---- vào đây")]
    [SerializeField] private GameObject battleRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void QuitToMenu()
    {
        Debug.Log("[GamePlayController] Quay về Menu...");

        // Đóng bảng kết quả nếu đang mở
        if (GameReferee.Instance != null && GameReferee.Instance.ResultPanel != null)
            GameReferee.Instance.ResultPanel.SetActive(false);

        // Reset bàn cờ sạch sẽ cho lần chơi tiếp theo
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();

        // Ẩn toàn bộ battle (ẩn ----PLAYCARD----)
        if (battleRoot != null)
            battleRoot.SetActive(false);
        else
            Debug.LogWarning("[GamePlayController] Chưa gán battleRoot trong Inspector!");

        // Re-enable nút Play Offline (bị lock khi vào trận để chống spam)
        Canvas_CombatManager.Instance?.OnReturnFromBattle();

        // Refresh dữ liệu PlayFab sau trận (thay OnSceneLoaded cũ)
        PlayFabDataManager.Instance?.RefreshAfterBattle();
    }

    public void RestartGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[GamePlayController] Không tìm thấy GameManager để restart!");
            return;
        }
        Debug.Log("[GamePlayController] Restart trận đấu...");
        GameManager.Instance.RestartGame();

        if (GameReferee.Instance != null && GameReferee.Instance.ResultPanel != null)
            GameReferee.Instance.ResultPanel.SetActive(false);
    }
}
