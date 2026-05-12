using UnityEngine;

public class BattleLauncher : MonoBehaviour
{
    [Header("Root References")]
    [Tooltip("Kéo ----PLAYCARD---- vào đây")]
    [SerializeField] private GameObject battleRoot;
    [SerializeField] private BattleFlowManager battleFlowManager;

    private void Awake()
    {
        Application.runInBackground = true;
    }

    public void OnPlayOfflineClicked()
    {
        Debug.Log("Đang vào chế độ Offline...");

        // Lưu dữ liệu trước khi vào trận
        if (PlayFabDataManager.Instance != null)
            PlayFabDataManager.Instance.SaveGameData();

        StartGame();
    }

    public void OnQuitClicked()
    {
        Debug.Log("Application Quit called!");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void StartGame()
    {
        // 1. Cập nhật LocalDeckContext trước khi vào trận
        //    Đảm bảo gem vừa khảm sẽ được tính vào trận ngay lập tức
        if (DeckManager.Instance != null && DeckManager.Instance.currentDeck != null)
            LocalDeckContext.SetDeck(DeckManager.Instance.currentDeck);

        // 2. Bật ----PLAYCARD---- (chứa toàn bộ battle UI + managers)
        if (battleRoot != null)
            battleRoot.SetActive(true);
        else
        {
            Debug.LogError("[BattleLauncher] Chưa kéo ----PLAYCARD---- vào field Battle Root trong Inspector!");
            return;
        }

        // 3. Kích hoạt luồng chiến đấu
        if (battleFlowManager != null)
            battleFlowManager.StartBattle();
        else
            Debug.LogError("[BattleLauncher] Chưa kéo BattleFlowManager vào Inspector!");
    }
}