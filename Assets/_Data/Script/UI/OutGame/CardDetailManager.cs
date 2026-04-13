using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDetailManager : YamiMonoBehaviour
{
    public static CardDetailManager Instance { get; private set; }

    [Header("Bên Phải - Kho Ngọc")]
    [SerializeField] private GemInventoryUI gemInventoryUI;

    [Header("Bên Trái - Thẻ Bài")]
    [SerializeField] private UI_CardBase cardPreviewVisual;

    [Header("Bên Trái")]
    [SerializeField] private TextMeshProUGUI txtCardName;
    [SerializeField] private TextMeshProUGUI txtCardName2;
    [SerializeField] private TextMeshProUGUI txtCardDesc;
    [SerializeField] private Transform socketContainer;
    [SerializeField] private GameObject socketPrefab;

    [Header("Stats References")]
    [SerializeField] private TextMeshProUGUI txtStatTop;
    [SerializeField] private TextMeshProUGUI txtStatRight;
    [SerializeField] private TextMeshProUGUI txtStatBottom;
    [SerializeField] private TextMeshProUGUI txtStatLeft;

    private OwnedCard _selectedCard;
    private UI_Socket _currentSelectedSocket;
    private InventoryItem _pendingGemToEquip;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenDetail(OwnedCard card)
    {
        _selectedCard = card;
        gameObject.SetActive(true);

        RefreshCardInfo();

        if (gemInventoryUI != null) gemInventoryUI.RefreshGemList();
    }

    private void RefreshCardInfo()
    {
        if (cardPreviewVisual != null) cardPreviewVisual.Setup(_selectedCard.data);
        if (txtCardName != null) txtCardName.text = _selectedCard.data.cardName;
        if (txtCardName2 != null) txtCardName2.text = _selectedCard.data.cardName;
        if (txtCardDesc != null && _selectedCard.data.skill != null) txtCardDesc.text = _selectedCard.data.skill.description;

        UI_Socket[] allSockets = socketContainer.GetComponentsInChildren<UI_Socket>(true);

        // 1. Mặc định: KHÓA TẤT CẢ 12 LỖ (Vẫn hiện Khung, nhưng có hình Ổ Khóa)
        foreach (var socket in allSockets)
        {
            socket.gameObject.SetActive(true); // ĐẢM BẢO LUÔN HIỆN
            socket.SetupState(isLocked: true);
        }

        // 2. Đi tìm các lỗ được phép mở theo thẻ
        int topIdx = 0, botIdx = 3, rightIdx = 6, leftIdx = 9;

        foreach (GemDirection dir in _selectedCard.data.availableSockets)
        {
            int targetIndex = -1;

            if (dir == GemDirection.Top && topIdx <= 2) { targetIndex = topIdx; topIdx++; }
            else if (dir == GemDirection.Bottom && botIdx <= 5) { targetIndex = botIdx; botIdx++; }
            else if (dir == GemDirection.Right && rightIdx <= 8) { targetIndex = rightIdx; rightIdx++; }
            else if (dir == GemDirection.Left && leftIdx <= 11) { targetIndex = leftIdx; leftIdx++; }

            if (targetIndex != -1 && targetIndex < allSockets.Length)
            {
                // MỞ KHÓA LỖ NÀY
                allSockets[targetIndex].SetupState(isLocked: false);
            }
        }
        for (int i = 0; i < allSockets.Length; i++)
        {
            // Nếu trong data báo lỗ thứ i này có ngọc
            if (_selectedCard.equippedGems[i] != null && _selectedCard.equippedGems[i].data != null)
            {
                // Vẽ viên ngọc đó lên UI
                allSockets[i].EquipGem(_selectedCard.equippedGems[i]);
            }
        }
        RefreshStatsDisplay();
    }

    private void RefreshStatsDisplay()
    {
        if (_selectedCard == null) return;

        int bonusTop = 7;
        int bonusRight = 26;
        int bonusBottom = 12;
        int bonusLeft = 8;

        UpdateSingleStatText(txtStatTop, "Trên", _selectedCard.data.top, bonusTop);
        UpdateSingleStatText(txtStatRight, "Phải", _selectedCard.data.right, bonusRight);
        UpdateSingleStatText(txtStatBottom, "Dưới", _selectedCard.data.bottom, bonusBottom);
        UpdateSingleStatText(txtStatLeft, "Trái", _selectedCard.data.left, bonusLeft);
    }

    private void UpdateSingleStatText(TextMeshProUGUI tmp, string label, int baseVal, int bonusVal)
    {
        if (tmp == null) return;
        string bonusText = bonusVal > 0 ? $" <color=#00FF00>(+{bonusVal})</color>" : "";
        tmp.text = $"{label}: {baseVal}{bonusText}";
    }

    // ==========================================
    // LOGIC CHUẨN BỊ KHẢM (DÙNG ENUM)
    // ==========================================
    public void PrepareToEquipGem(InventoryItem gem)
    {
        _pendingGemToEquip = gem;
        GemDirection targetDir = gem.data.directionTag; // Lấy thẳng Enum từ Ngọc

        UI_Socket[] allSockets = socketContainer.GetComponentsInChildren<UI_Socket>(true);

        for (int i = 0; i < allSockets.Length; i++)
        {
            GemDirection socketDir = GetDirectionByIndex(i);

            // NẾU: Đúng hướng Enum + Lỗ đang mở + Lỗ chưa có ngọc
            if (socketDir == targetDir && allSockets[i].EquippedGem == null && allSockets[i].IsUnlocked)
            {
                allSockets[i].SetReadyToEquip(true); // Nhuộm vàng
            }
            else
            {
                allSockets[i].SetReadyToEquip(false);
            }
        }
    }

    // Hàm chuyển Index sang Enum
    private GemDirection GetDirectionByIndex(int index)
    {
        if (index >= 0 && index <= 2) return GemDirection.Top;
        if (index >= 3 && index <= 5) return GemDirection.Bottom;
        if (index >= 6 && index <= 8) return GemDirection.Right;
        if (index >= 9 && index <= 11) return GemDirection.Left;
        return GemDirection.None;
    }

    // ==========================================
    // LOGIC CLICK VÀO LỖ (CHỐNG DÍNH CLICK)
    // ==========================================
    public void OnSocketClicked(UI_Socket socket)
    {
        // --------------------------------------------------------
        // TRƯỜNG HỢP 1: SẾP ĐANG CẦM NGỌC TRÊN TAY -> CHỈ THỰC HIỆN KHẢM
        // --------------------------------------------------------
        if (_pendingGemToEquip != null)
        {
            int index = socket.transform.GetSiblingIndex();
            GemDirection socketDir = GetDirectionByIndex(index);

            // Check chuẩn Enum và lỗ trống
            // Check chuẩn Enum và lỗ trống
            if (socketDir == _pendingGemToEquip.data.directionTag && socket.EquippedGem == null)
            {
                int socketIndex = socket.transform.GetSiblingIndex(); // Lấy vị trí lỗ (0-11)

                // 1. Ghi vào UI (Cái vỏ)
                socket.EquipGem(_pendingGemToEquip);

                // 2. GHI VÀO DATA (Linh hồn)
                _selectedCard.equippedGems[socketIndex] = _pendingGemToEquip;

                if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem(_pendingGemToEquip);
                if (gemInventoryUI != null) gemInventoryUI.RefreshGemList();
            }

            // Xử lý xong thì "rửa tay", hủy trạng thái chờ khảm
            _pendingGemToEquip = null;
            ResetAllSocketHighlights();

            // Xong việc thì cút luôn, không chạy xuống dưới nữa!
            return;
        }

        // --------------------------------------------------------
        // TRƯỜNG HỢP 2: TAY KHÔNG BẤM VÀO LỖ -> CHỈ ĐỂ SOI THÔNG TIN
        // --------------------------------------------------------
        else
        {
            // Tắt highlight cũ (nếu có)
            if (_currentSelectedSocket != null) _currentSelectedSocket.SetReadyToEquip(false);

            _currentSelectedSocket = socket;
            _currentSelectedSocket.SetReadyToEquip(true); // Bật highlight cho lỗ đang chọn

            // Mở popup nếu cái lỗ sếp nhấp vào đang chứa ngọc
            if (socket.EquippedGem != null && GemInfoPopupManager.Instance != null)
            {
                GemInfoPopupManager.Instance.OpenPopup(socket.EquippedGem, true);
            }
        }
    }

    private void ResetAllSocketHighlights()
    {
        UI_Socket[] allSockets = socketContainer.GetComponentsInChildren<UI_Socket>(true);
        foreach (var s in allSockets) s.SetReadyToEquip(false);
    }

    public void CloseDetail()
    {
        _pendingGemToEquip = null;
        ResetAllSocketHighlights();

        if (CardListManager.Instance != null)
        {
            CardListManager.Instance.gameObject.SetActive(true);
        }
        this.gameObject.SetActive(false);
    }
    // --- LOGIC GỠ NGỌC ---
    public void UnequipCurrentSocket()
    {
        if (_currentSelectedSocket != null && _currentSelectedSocket.EquippedGem != null)
        {
            // 1. Lưu lại thông tin viên ngọc đang nằm trong lỗ
            int socketIndex = _currentSelectedSocket.transform.GetSiblingIndex();
            var gemToReturn = _currentSelectedSocket.EquippedGem;

            // 2. Trả ngọc về túi đồ
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(gemToReturn.data, 1);
            }
            // XÓA KHỎI DATA
            _selectedCard.equippedGems[socketIndex] = null;
            if (gemInventoryUI != null) gemInventoryUI.RefreshGemList(); // F5 lại UI túi ngọc nhỏ

            // 3. Reset lỗ về trạng thái Mở Khóa nhưng Trống không
            _currentSelectedSocket.SetupState(isLocked: false);
            _currentSelectedSocket.SetReadyToEquip(false); // Tắt viền vàng
            _currentSelectedSocket = null; // Quên lỗ này đi

            // 4. (Tương lai) Sếp gọi thêm hàm RefreshStatsDisplay() ở đây để trừ chỉ số
        }
    }
    public void RefreshRightGemInventory()
    {
        if (gemInventoryUI != null)
        {
            gemInventoryUI.RefreshGemList();
        }
    }
}