using Fusion;
using TMPro;
using UnityEngine;

// Lớp này là Monobehaviour bình thường, chỉ tồn tại trong Scene
public class NetworkAppManager : MonoBehaviour
{
    [Header("Network Prefabs to Spawn")]
    // Kéo Prefab GameManagerNet vào đây (NÓ LÀ PREFAB GỐC, KHÔNG CÓ TRONG HIERARCHY)
    [SerializeField] private NetworkObject gameManagerNetPrefab;
    [SerializeField] private NetworkObject GameRefereeNetPrefab;

    [Header("Scene References")]
    [SerializeField] private Transform[] slots;
    [SerializeField] private Transform leftHandPos;
    [SerializeField] private Transform rightHandPos;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    private NetworkRunner runner;

    // HÀM NÀY PHẢI ĐƯỢC GỌI KHI MẠNG KHỞI TẠO (Trong Fusion Launcher/UI)
    public void StartGame(NetworkRunner runner)
    {
        this.runner = runner;
        // Bắt đầu sinh ra Network Manager
        runner.Spawn(gameManagerNetPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer, InitializeManager);
        runner.Spawn(GameRefereeNetPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer, InitializeRefereeNet);
    }

    // Callback này được gọi khi Network Object đã được sinh ra thành công
    private void InitializeManager(NetworkRunner runner, NetworkObject networkObject)
    {
        GameManagerNet manager = networkObject.GetComponent<GameManagerNet>();

        if (manager != null)
        {
            // ✅ QUAN TRỌNG: Gán các tham chiếu tĩnh (Scene UI) vào Network Manager VỪA ĐƯỢC SINH RA
            // Vì các biến này trong GameManagerNet là [SerializeField] private, 
            // ta sẽ phải dùng Reflective Property/Method hoặc Public Setter (cách an toàn hơn).

            // Tạm thời, để code đơn giản, ta cần tạo Public Setter trong GameManagerNet cho các biến này:
            manager.SetSceneReferences(slots, leftHandPos, rightHandPos, turnText);
            Debug.Log("GameManagerNet đã được sinh ra và các tham chiếu UI đã được gán.");
        }
    }
    private void InitializeRefereeNet(NetworkRunner runner, NetworkObject networkObject)
    {
        GameRefereeNet refereeNet = networkObject.GetComponent<GameRefereeNet>();

        if (refereeNet != null)
        {
            refereeNet.SetUIRefs(resultPanel, resultText);
            Debug.Log("GameRefereeNet đã được sinh ra và các tham chiếu UI đã được gán.");
        }
    }
}