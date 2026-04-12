using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GemInfoPopupManager : YamiMonoBehaviour
{
    public static GemInfoPopupManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtNameAndStats;
    [SerializeField] private TextMeshProUGUI txtDirectionTag;
    [SerializeField] private Button btnAction;     // Nút Khảm/Gỡ
    [SerializeField] private TextMeshProUGUI txtActionBtn; // Chữ bên trong nút Khảm/Gỡ
    [SerializeField] private Button btnUpgrade;    // Nút Nâng cấp

    // Lưu lại thông tin viên ngọc đang chọn để mốt còn xử lý logic
    private InventoryManager.InventoryItem _currentGem;
    private bool _isEquippedGem;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Nếu lỡ tay có 2 thằng thì xóa bớt 1
        }
    }

    // Hàm public để các Slot (ô ngọc) gọi vào khi bị click
    public void OpenPopup(InventoryManager.InventoryItem gem, bool isEquipped)
    {
        if (gem == null || gem.data == null) return;

        _currentGem = gem;
        _isEquippedGem = isEquipped;

        // Bật UI lên
        gameObject.SetActive(true);

        // ==========================================
        // 1. ĐỔ DATA VÀO TEXT
        // (Lưu ý: Mấy biến gemDirection, bonusStat sếp nhớ thêm vào ItemDataSO nhé)
        // ==========================================
        txtNameAndStats.text = $"{gem.data.itemName}\n<color=#00FF00>Cộng thêm: +{gem.data.bonusStat}</color>";
        txtDirectionTag.text = $"Vị trí khảm: {gem.data.directionTag}";

        // ==========================================
        // 2. LOGIC ĐỔI CHỮ NÚT (Khảm <-> Gỡ)
        // ==========================================
        txtActionBtn.text = _isEquippedGem ? "Gỡ Ngọc" : "Khảm";

        // ==========================================
        // 3. NẠP ĐẠN CHO NÚT BẤM
        // ==========================================
        btnAction.onClick.RemoveAllListeners();
        btnAction.onClick.AddListener(OnActionClicked);

        btnUpgrade.onClick.RemoveAllListeners();
        btnUpgrade.onClick.AddListener(OnUpgradeClicked);
    }

    public void ClosePopup() => this.gameObject.SetActive(false);

    private void OnActionClicked()
    {
        if (_isEquippedGem)
        {
            Debug.Log($"<color=yellow>Đang gỡ {_currentGem.data.itemName} trả về túi...</color>");
            // GỌI HÀM GỠ NGỌC TỪ CARD DETAIL
            if (CardDetailManager.Instance != null)
            {
                CardDetailManager.Instance.UnequipCurrentSocket();
            }
        }
        else
        {
            Debug.Log($"<color=green>Chuẩn bị khảm {_currentGem.data.itemName}...</color>");
            // GỌI HÀM KHẢM NGỌC NHƯ CŨ
            if (CardDetailManager.Instance != null)
            {
                CardDetailManager.Instance.PrepareToEquipGem(_currentGem);
            }
        }
        ClosePopup();
    }

    private void OnUpgradeClicked()
    {
        Debug.Log("<color=cyan>Chuyển sang giao diện đập lò (Nâng cấp) tinh thạch!</color>");
        // Gọi Manager mở bảng Nâng Cấp ở đây
        ClosePopup();
    }
}