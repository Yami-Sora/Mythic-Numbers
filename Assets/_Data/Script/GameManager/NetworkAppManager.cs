using Fusion;
using TMPro;
using UnityEngine;

public class NetworkAppManager : MonoBehaviour
{
    public static NetworkAppManager Instance;

    [Header("Network Prefabs")]
    [SerializeField] private NetworkObject gameManagerNetPrefab;
    [SerializeField] private NetworkObject gameRefereeNetPrefab;

    [Header("Scene References")]
    [SerializeField] private Transform[] slots;
    [SerializeField] private Transform leftHandPos;
    [SerializeField] private Transform rightHandPos;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    private NetworkRunner runner;

    private void Awake()
    {
        // Singleton Pattern
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
    }

    // ✅ [QUAN TRỌNG NHẤT] VÒNG LẶP CỨU HỘ
    // Hàm này chạy mỗi frame. Nó sẽ tự động tìm xem có Manager nào chưa có UI không thì gán ngay.
    // Cách này giúp Client (không gọi Spawn) vẫn được gán UI.
    private void Update()
    {
        // 1. Cứu GameManagerNet
        if (GameManagerNet.Instance != null && !GameManagerNet.Instance.IsUIReady)
        {
            Debug.Log("[AppManager] Phát hiện GameManager chưa có UI -> Đang gán...");
            SetupGameManagerUI(GameManagerNet.Instance);
        }
    }

    public void StartGame(NetworkRunner runner)
    {
        this.runner = runner;
        // Spawn không cần callback nữa, vì hàm Update bên trên sẽ lo việc đó ngay lập tức
        runner.Spawn(gameManagerNetPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
        runner.Spawn(gameRefereeNetPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
    }

    // Hàm gán UI cho GameManager
    public void SetupGameManagerUI(GameManagerNet manager)
    {
        manager.SetSceneReferences(slots, leftHandPos, rightHandPos, turnText);
        Debug.Log("AppManager: Đã gán UI cho GameManagerNet.");
    }

    // Hàm gán UI cho Referee
    public void SetupRefereeUI(GameRefereeNet referee)
    {
        if (resultPanel == null || resultText == null)
        {
            Debug.LogError("AppManager: Quên kéo ResultPanel hoặc ResultText vào Inspector rồi!");
            return;
        }

        referee.SetUIRefs(resultPanel, resultText);
        Debug.Log("AppManager: Đã gán UI cho GameRefereeNet.");
    }
}