using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class BattleFlowManager : MonoBehaviour
{
    public static BattleFlowManager Instance;

    [Header("Core Prefabs")]
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject gameRefereePrefab;

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
        if (resultPanel) resultPanel.SetActive(false);
        
        // Spawn Managers ngay khi vào Scene
        SpawnGameManagers();
    }

    private bool _hasSpawnedManagers = false;

    private void SpawnGameManagers()
    {
        if (_hasSpawnedManagers) return;
        _hasSpawnedManagers = true;

        if (GameManager.Instance == null)
        {
            Debug.Log("BattleFlowManager: Tiến hành Spawn Manager...");
            if (gameManagerPrefab != null) Instantiate(gameManagerPrefab);
            if (gameRefereePrefab != null) Instantiate(gameRefereePrefab);
        }
    }

    public void OnUIManagerReady()
    {
        Debug.Log("[BattleFlowManager] Tìm thấy GameUIManager -> Đang chuyển giao tham chiếu UI...");
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

    // Hàm gán UI cho Referee
    public void SetupRefereeUI(GameReferee referee)
    {
        if (resultPanel == null || resultText == null)
        {
            Debug.LogError("BattleFlowManager: Quên kéo ResultPanel hoặc ResultText vào Inspector rồi!");
            return;
        }

        referee.SetUIRefs(resultPanel, resultText);
        Debug.Log("BattleFlowManager: Đã gán UI cho GameReferee.");
    }
}