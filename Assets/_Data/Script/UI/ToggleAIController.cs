using UnityEngine;
using UnityEngine.UI;
using TMPro; // ✅ Cần thêm thư viện này để dùng TextMeshPro

public class ToggleAIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle myToggle;
    [SerializeField] private TMP_Text toggleLabel; // ✅ Kéo cái Text (Label) vào đây

    void Start()
    {
        if (myToggle == null) myToggle = GetComponent<Toggle>();

        // 1. Đăng ký sự kiện
        myToggle.onValueChanged.AddListener(HandleToggle);

        // 2. Cập nhật chữ ngay từ đầu cho đúng trạng thái mặc định
        UpdateLabelText(myToggle.isOn);
    }

    void HandleToggle(bool isOn)
    {
        // ✅ Đổi chữ ngay khi bấm
        UpdateLabelText(isOn);

        // Logic cũ: Gọi sang GameManager
        if (GameManagerNet.Instance != null)
        {
            //GameManagerNet.Instance.OnAIModeChanged(isOn);
        }
        else
        {
            // Debug.LogWarning("Chưa tìm thấy GameManagerNet, nhưng UI vẫn sẽ đổi chữ.");
        }
    }

    // Hàm riêng để xử lý việc đổi chữ
    void UpdateLabelText(bool isOn)
    {
        if (toggleLabel != null)
        {
            if (isOn)
            {
                toggleLabel.text = "Mode: vs AI";
            }
            else
            {
                toggleLabel.text = "Mode: 2 Player";
            }
        }
    }

    void Update()
    {
        // Đồng bộ trạng thái ngược từ Game -> UI (nếu Game tự đổi mode)
        if (GameManagerNet.Instance != null && myToggle.isOn != GameManagerNet.Instance.playWithAI)
        {
            myToggle.isOn = GameManagerNet.Instance.playWithAI;
            UpdateLabelText(myToggle.isOn);
        }
    }
}