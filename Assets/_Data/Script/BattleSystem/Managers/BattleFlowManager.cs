using UnityEngine;
using TMPro;

public class BattleFlowManager : MonoBehaviour
{
    public static BattleFlowManager Instance;

    [Header("Scene References")]
    [SerializeField] private Transform[] slots;
    [SerializeField] private Transform leftHandPos;
    [SerializeField] private Transform rightHandPos;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private Transform mainCanvas;

    [Header("Referee UI References")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Tắt ResultPanel phòng hờ Inspector quên tắt
        if (resultPanel) resultPanel.SetActive(false);

        // KHÔNG spawn GameManager/GameReferee nữa:
        // Cả 2 đã được đặt thẳng vào ----PLAYCARD---- trong scene.
    }

    /// <summary>
    /// Được gọi bởi BattleLauncher khi người chơi bắt đầu trận.
    /// Thay thế hoàn toàn cho SceneManager.LoadScene("PlayCardScene").
    /// </summary>
    public void StartBattle()
    {
        Debug.Log("[BattleFlowManager] Bắt đầu trận chiến!");

        // Reset bàn cờ (GameManager.Start() chỉ chạy 1 lần khi Enable lần đầu,
        // từ lần 2 trở đi phải gọi RestartGame() để deal bài mới)
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
        else
            Debug.LogError("[BattleFlowManager] Không tìm thấy GameManager! Kiểm tra ----PLAYCARD---- trong Hierarchy.");

        // Gán UI refs cho Referee
        if (GameReferee.Instance != null)
            SetupRefereeUI(GameReferee.Instance);
        else
            Debug.LogError("[BattleFlowManager] Không tìm thấy GameReferee! Kiểm tra ----PLAYCARD---- trong Hierarchy.");

        // Gán refs cho InGameUIManager
        if (InGameUIManager.Instance != null)
            SetupUIManager();
    }

    /// <summary>
    /// Được InGameUIManager.Start() gọi khi nó khởi động lần đầu.
    /// </summary>
    public void OnUIManagerReady()
    {
        Debug.Log("[BattleFlowManager] UIManager ready → Setup refs...");
        SetupUIManager();
    }

    private void SetupUIManager()
    {
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.SetupReferences(slots, leftHandPos, rightHandPos, turnText, mainCanvas);
            InGameUIManager.Instance.ResetBoardUI();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.RefreshAllCards();
    }

    public void SetupRefereeUI(GameReferee referee)
    {
        if (resultPanel == null || resultText == null)
        {
            Debug.LogError("BattleFlowManager: Thiếu ResultPanel hoặc ResultText trong Inspector!");
            return;
        }

        referee.SetUIRefs(resultPanel, resultText);
        Debug.Log("BattleFlowManager: Đã gán UI cho GameReferee.");
    }
}