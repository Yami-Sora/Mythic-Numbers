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
        if (cardPreviewVisual != null) cardPreviewVisual.Setup(_selectedCard);
        if (txtCardName != null) txtCardName.text = _selectedCard.data.cardName;
        if (txtCardName2 != null) txtCardName2.text = _selectedCard.data.cardName;
        // [UPDATE]: Nâng cấp hiển thị txtCardDesc (Gộp Rate + Skill)
        if (txtCardDesc != null)
        {
            string rateColor = GetRateColorHex(_selectedCard.data.rate);
            string rateString = $"Độ hiếm: <color={rateColor}><b>{_selectedCard.data.rate}</b></color>";
            string skillDesc = _selectedCard.data.skill != null ? _selectedCard.data.skill.description : "<i>Thẻ này không có kỹ năng.</i>";

            // Ép dòng Rate lên trên, cách 1 dòng rồi tới Kỹ năng
            txtCardDesc.text = $"{rateString}\n\n{skillDesc}";
        }

        UI_Socket[] allSockets = socketContainer.GetComponentsInChildren<UI_Socket>(true);

        // 1. Mặc định: KHÓA TẤT CẢ 12 LỖ
        foreach (var socket in allSockets)
        {
            socket.gameObject.SetActive(true);
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
                allSockets[targetIndex].SetupState(isLocked: false);
            }
        }
        for (int i = 0; i < allSockets.Length; i++)
        {
            if (_selectedCard.equippedGems[i] != null && _selectedCard.equippedGems[i].data != null)
            {
                allSockets[i].EquipGem(_selectedCard.equippedGems[i]);
            }
        }

        RefreshStatsDisplay();
    }

    // ==========================================
    // LOGIC CẬP NHẬT CHỈ SỐ (ĐÃ ĐƯỢC TỐI ƯU CLEAN CODE)
    // ==========================================
    private void RefreshStatsDisplay()
    {
        if (_selectedCard == null || _selectedCard.data == null) return;

        // 1. Tận dụng "Động cơ" GetTotal từ OwnedCard, xóa bỏ vòng lặp tính tay cũ!
        int totalTop = _selectedCard.GetTotalTop();
        int totalRight = _selectedCard.GetTotalRight();
        int totalBottom = _selectedCard.GetTotalBottom();
        int totalLeft = _selectedCard.GetTotalLeft();

        // 2. Tính ra lượng ngọc buff thêm để in màu xanh
        int bonusTop = totalTop - _selectedCard.data.top;
        int bonusRight = totalRight - _selectedCard.data.right;
        int bonusBottom = totalBottom - _selectedCard.data.bottom;
        int bonusLeft = totalLeft - _selectedCard.data.left;

        // 3. Bắn data ra UI hiển thị (Hiển thị trực tiếp Chỉ số Tổng lên Text)
        UpdateSingleStatText(txtStatTop, "Trên", totalTop, bonusTop);
        UpdateSingleStatText(txtStatRight, "Phải", totalRight, bonusRight);
        UpdateSingleStatText(txtStatBottom, "Dưới", totalBottom, bonusBottom);
        UpdateSingleStatText(txtStatLeft, "Trái", totalLeft, bonusLeft);

        // 4. Ép cái hình thẻ bài thu nhỏ bên trái phải vẽ lại số mỗi khi tháo/lắp ngọc
        if (cardPreviewVisual != null) cardPreviewVisual.Setup(_selectedCard);
    }

    private void UpdateSingleStatText(TextMeshProUGUI tmp, string label, int totalVal, int bonusVal)
    {
        if (tmp == null) return;

        // Hiện số (+Bonus) màu xanh lá. Nếu hiển thị là "Trên: 150 (+50)"
        string bonusText = bonusVal > 0 ? $" <color=#00FF00>(+{bonusVal})</color>" : "";
        tmp.text = $"{label}: {totalVal}{bonusText}";

        // Lưu ý: Nếu sếp muốn màu của Text đổi theo Cấp Bậc chỉ số (Đỏ, Vàng, Xanh) giống y như trên hình thẻ bài,
        // thì mở comment 2 dòng dưới đây (Đảm bảo file có truy cập được RealmCalculator):
        // var realm = RealmCalculator.GetRealmStat(totalVal);
        // tmp.color = realm.displayColor; 
    }

    // ==========================================
    // LOGIC CHUẨN BỊ KHẢM
    // ==========================================
    public void PrepareToEquipGem(InventoryItem gem)
    {
        _pendingGemToEquip = gem;
        GemDirection targetDir = gem.data.directionTag;

        UI_Socket[] allSockets = socketContainer.GetComponentsInChildren<UI_Socket>(true);

        for (int i = 0; i < allSockets.Length; i++)
        {
            GemDirection socketDir = GetDirectionByIndex(i);

            if (socketDir == targetDir && allSockets[i].EquippedGem == null && allSockets[i].IsUnlocked)
            {
                allSockets[i].SetReadyToEquip(true);
            }
            else
            {
                allSockets[i].SetReadyToEquip(false);
            }
        }
    }

    private GemDirection GetDirectionByIndex(int index)
    {
        if (index >= 0 && index <= 2) return GemDirection.Top;
        if (index >= 3 && index <= 5) return GemDirection.Bottom;
        if (index >= 6 && index <= 8) return GemDirection.Right;
        if (index >= 9 && index <= 11) return GemDirection.Left;
        return GemDirection.None;
    }

    // ==========================================
    // LOGIC CLICK VÀO LỖ 
    // ==========================================
    public void OnSocketClicked(UI_Socket socket)
    {
        if (_pendingGemToEquip != null)
        {
            int index = socket.transform.GetSiblingIndex();
            GemDirection socketDir = GetDirectionByIndex(index);

            if (socketDir == _pendingGemToEquip.data.directionTag && socket.EquippedGem == null)
            {
                int socketIndex = socket.transform.GetSiblingIndex();

                socket.EquipGem(_pendingGemToEquip);
                _selectedCard.equippedGems[socketIndex] = _pendingGemToEquip;

                if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem(_pendingGemToEquip);
                if (gemInventoryUI != null) gemInventoryUI.RefreshGemList();

                // Gọi làm mới UI (Bao gồm cả text bên phải và thẻ bài bên trái)
                RefreshStatsDisplay();
            }

            _pendingGemToEquip = null;
            ResetAllSocketHighlights();
            return;
        }
        else
        {
            if (_currentSelectedSocket != null) _currentSelectedSocket.SetReadyToEquip(false);

            _currentSelectedSocket = socket;
            _currentSelectedSocket.SetReadyToEquip(true);

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
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.gameObject.SetActive(true);
        }
        this.gameObject.SetActive(false);
    }

    // --- LOGIC GỠ NGỌC ---
    public void UnequipCurrentSocket()
    {
        if (_currentSelectedSocket != null && _currentSelectedSocket.EquippedGem != null)
        {
            int socketIndex = _currentSelectedSocket.transform.GetSiblingIndex();
            var gemToReturn = _currentSelectedSocket.EquippedGem;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(gemToReturn.data, 1);
            }

            _selectedCard.equippedGems[socketIndex] = null;
            if (gemInventoryUI != null) gemInventoryUI.RefreshGemList();

            _currentSelectedSocket.SetupState(isLocked: false);
            _currentSelectedSocket.SetReadyToEquip(false);
            _currentSelectedSocket.ClearSocket();
            _currentSelectedSocket = null;

            // Cập nhật lại UI khi tháo ngọc
            RefreshStatsDisplay();
        }
    }

    public void RefreshRightGemInventory()
    {
        if (gemInventoryUI != null)
        {
            gemInventoryUI.RefreshGemList();
        }
    }

    // ==========================================
    // HÀM TIỆN ÍCH: PHA MÀU CHO ĐỘ HIẾM
    // ==========================================
    private string GetRateColorHex(CardDataSO.CardRate rate)
    {
        switch (rate)
        {
            case CardDataSO.CardRate.N: return "#80C840"; // Xanh lá
            case CardDataSO.CardRate.R: return "#00BFFF"; // Xanh dương hi vọng
            case CardDataSO.CardRate.SR: return "#BA55D3"; // Tím mộng mơ
            case CardDataSO.CardRate.SSR: return "#FFD700"; // Vàng kim chói lóa
            default: return "#FFFFFF";
        }
    }
}