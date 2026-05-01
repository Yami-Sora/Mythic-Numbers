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

    [Header("Bên Trái - Chi tiết")]
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

    [Header("Bên Phải - Tab Nâng Sao")]
    [SerializeField] private GameObject starUpgradePanel;
    [SerializeField] private UI_CardBase uiCardSlotBefore;
    [SerializeField] private UI_CardBase uiCardSlotAfter;
    [SerializeField] private TextMeshProUGUI txtCurrentStar;
    [SerializeField] private TextMeshProUGUI txtNextStar;
    [SerializeField] private TextMeshProUGUI txtNextLevelInfo;
    [SerializeField] private TextMeshProUGUI txtStarProgress;
    [SerializeField] private Button btnUpgradeStar;

    [Header("Bên Phải - Tab Chuyển Đổi")]
    [SerializeField] private Button btnTabGem;
    [SerializeField] private Button btnTabStar;

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

        SwitchToGemTab();
        RefreshCardInfo();

        if (gemInventoryUI != null) gemInventoryUI.RefreshGemList();
    }

    private void RefreshCardInfo()
    {
        if (cardPreviewVisual != null) cardPreviewVisual.Setup(_selectedCard);
        if (txtCardName != null) txtCardName.text = _selectedCard.data.cardName;
        if (txtCardName2 != null) txtCardName2.text = _selectedCard.data.cardName;

        if (txtCardDesc != null)
        {
            string rateColor = GetRateColorHex(_selectedCard.data.rate);
            string rateString = $"Độ hiếm: <color={rateColor}><b>{_selectedCard.data.rate}</b></color>";
            string skillDesc = _selectedCard.data.skill != null ? _selectedCard.data.skill.description : "<i>Thẻ này không có kỹ năng.</i>";
            txtCardDesc.text = $"{rateString}\n\n{skillDesc}";
        }

        UI_Socket[] allSockets = socketContainer.GetComponentsInChildren<UI_Socket>(true);

        // 1. Khóa toàn bộ 12 lỗ
        foreach (var socket in allSockets)
        {
            socket.gameObject.SetActive(true);
            socket.SetupState(isLocked: true);
        }

        // 2. TÍNH QUOTA LỖ DỰA TRÊN SAO
        int maxOpenSockets = 4 + _selectedCard.starLevel;
        if (maxOpenSockets > 12) maxOpenSockets = 12;

        // [PHÁP THUẬT MỚI]: Mảng quy định thứ tự mở lỗ trải đều 4 góc
        // 0-3 là 4 lỗ cơ bản. Tiếp theo sẽ ưu tiên mở Trên->Dưới->Phải->Trái
        int[] unlockSequence = new int[] { 0, 3, 6, 9, 1, 4, 7, 10, 2, 5, 8, 11 };

        // Mở khóa đúng số lượng Quota
        for (int i = 0; i < maxOpenSockets; i++)
        {
            int targetIndex = unlockSequence[i];
            if (targetIndex < allSockets.Length)
            {
                allSockets[targetIndex].SetupState(isLocked: false);
            }
        }

        // 3. Khảm ngọc vào
        for (int i = 0; i < allSockets.Length; i++)
        {
            if (_selectedCard.equippedGems[i] != null && _selectedCard.equippedGems[i].data != null)
            {
                allSockets[i].EquipGem(_selectedCard.equippedGems[i]);
            }
        }

        RefreshStatsDisplay();
    }

    private void RefreshStatsDisplay()
    {
        if (_selectedCard == null || _selectedCard.data == null) return;

        int totalTop = _selectedCard.GetTotalTop();
        int totalRight = _selectedCard.GetTotalRight();
        int totalBottom = _selectedCard.GetTotalBottom();
        int totalLeft = _selectedCard.GetTotalLeft();

        int bonusTop = totalTop - _selectedCard.data.top;
        int bonusRight = totalRight - _selectedCard.data.right;
        int bonusBottom = totalBottom - _selectedCard.data.bottom;
        int bonusLeft = totalLeft - _selectedCard.data.left;

        UpdateSingleStatText(txtStatTop, "Trên", totalTop, bonusTop);
        UpdateSingleStatText(txtStatRight, "Phải", totalRight, bonusRight);
        UpdateSingleStatText(txtStatBottom, "Dưới", totalBottom, bonusBottom);
        UpdateSingleStatText(txtStatLeft, "Trái", totalLeft, bonusLeft);

        if (cardPreviewVisual != null) cardPreviewVisual.Setup(_selectedCard);

        RefreshStarUpgradeUI();
    }

    private void UpdateSingleStatText(TextMeshProUGUI tmp, string label, int totalVal, int bonusVal)
    {
        if (tmp == null) return;
        string bonusText = bonusVal > 0 ? $" <color=#00FF00>(+{bonusVal})</color>" : "";
        tmp.text = $"{label}: {totalVal}{bonusText}";
    }

    // ==========================================
    // LOGIC NÂNG SAO (ĐỘT PHÁ)
    // ==========================================
    public void RefreshStarUpgradeUI()
    {
        if (_selectedCard == null) return;

        bool isMaxStar = _selectedCard.IsMaxStar();

        if (uiCardSlotBefore != null) uiCardSlotBefore.Setup(_selectedCard);

        if (uiCardSlotAfter != null)
        {
            if (isMaxStar)
            {
                uiCardSlotAfter.gameObject.SetActive(false);
            }
            else
            {
                uiCardSlotAfter.gameObject.SetActive(true);
                OwnedCard previewCard = new OwnedCard(_selectedCard.data)
                {
                    level = _selectedCard.level,
                    starLevel = _selectedCard.starLevel + 1,
                    equippedGems = _selectedCard.equippedGems
                };
                uiCardSlotAfter.Setup(previewCard);
            }
        }

        if (isMaxStar)
        {
            if (txtCurrentStar != null) txtCurrentStar.text = $"Cấp sao hiện tại: MAX ({_selectedCard.starLevel} Sao)";
            if (txtNextStar != null) txtNextStar.text = "Cấp sao tiếp theo: KHÔNG THỂ NÂNG THÊM";
            if (txtNextLevelInfo != null) txtNextLevelInfo.text = "Thẻ đã đạt cảnh giới tối cao!";
            if (txtStarProgress != null) txtStarProgress.text = "MAX";
            if (btnUpgradeStar != null) btnUpgradeStar.interactable = false;
        }
        else
        {
            int reqShards = _selectedCard.GetRequiredShardsForNextStar();

            if (txtCurrentStar != null) txtCurrentStar.text = $"Cấp sao hiện tại: {_selectedCard.starLevel} Sao";
            if (txtNextStar != null) txtNextStar.text = $"Cấp sao tiếp theo: {_selectedCard.starLevel + 1} Sao";

            if (txtNextLevelInfo != null)
            {
                // [CẬP NHẬT CHỈ SỐ TEXT MỞ Ô NGỌC]
                int nextSocketNum = _selectedCard.starLevel + 5;
                string socketInfo = nextSocketNum <= 12 ? $"\nMở khóa ô Tinh thạch số {nextSocketNum}" : "";

                txtNextLevelInfo.text = $"Chỉ số cơ bản: <color=#00FF00>+10%</color>{socketInfo}";
            }

            if (txtStarProgress != null)
            {
                string colorHex = _selectedCard.currentShards >= reqShards ? "#00FF00" : "#FF0000";
                txtStarProgress.text = $"<color={colorHex}>{_selectedCard.currentShards}</color>/{reqShards}";
            }

            if (btnUpgradeStar != null)
                btnUpgradeStar.interactable = _selectedCard.currentShards >= reqShards;
        }
    }

    public void OnUpgradeStarClicked()
    {
        if (_selectedCard == null || _selectedCard.IsMaxStar()) return;

        int reqShards = _selectedCard.GetRequiredShardsForNextStar();

        if (_selectedCard.currentShards >= reqShards)
        {
            _selectedCard.currentShards -= reqShards;
            _selectedCard.starLevel++;

            Debug.Log($"[Upgrade] 💥 Đột phá thành công! {_selectedCard.data.cardName} đã lên {_selectedCard.starLevel} Sao!");
            if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.MarkDirty();

            // Tính lại số ô ngọc mới được mở và load lại 2 con số Text
            RefreshCardInfo();

            if (CardListManager.Instance != null) CardListManager.Instance.DisplayCards();
        }
    }

    // ==========================================
    // CHUYỂN TAB 
    // ==========================================
    public void SwitchToGemTab()
    {
        if (gemInventoryUI != null) gemInventoryUI.gameObject.SetActive(true);
        if (starUpgradePanel != null) starUpgradePanel.gameObject.SetActive(false);

        if (btnTabGem != null) btnTabGem.interactable = false;
        if (btnTabStar != null) btnTabStar.interactable = true;
    }

    public void SwitchToStarTab()
    {
        if (gemInventoryUI != null) gemInventoryUI.gameObject.SetActive(false);
        if (starUpgradePanel != null) starUpgradePanel.gameObject.SetActive(true);

        if (btnTabGem != null) btnTabGem.interactable = true;
        if (btnTabStar != null) btnTabStar.interactable = false;

        RefreshStarUpgradeUI();
    }

    // ==========================================
    // LOGIC NGỌC (Giữ nguyên)
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

                if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.MarkDirty();
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

        if (CardListManager.Instance != null) CardListManager.Instance.gameObject.SetActive(true);
        if (DeckManager.Instance != null) DeckManager.Instance.gameObject.SetActive(true);

        this.gameObject.SetActive(false);
    }

    public void UnequipCurrentSocket()
    {
        if (_currentSelectedSocket != null && _currentSelectedSocket.EquippedGem != null)
        {
            int socketIndex = _currentSelectedSocket.transform.GetSiblingIndex();
            var gemToReturn = _currentSelectedSocket.EquippedGem;

            if (InventoryManager.Instance != null) InventoryManager.Instance.AddItem(gemToReturn.data, 1);

            _selectedCard.equippedGems[socketIndex] = null;
            if (gemInventoryUI != null) gemInventoryUI.RefreshGemList();

            _currentSelectedSocket.SetupState(isLocked: false);
            _currentSelectedSocket.SetReadyToEquip(false);
            _currentSelectedSocket.ClearSocket();
            _currentSelectedSocket = null;

            RefreshStatsDisplay();
        }
    }

    public void RefreshRightGemInventory()
    {
        if (gemInventoryUI != null) gemInventoryUI.RefreshGemList();
    }

    private string GetRateColorHex(CardDataSO.CardRate rate)
    {
        switch (rate)
        {
            case CardDataSO.CardRate.N: return "#80C840";
            case CardDataSO.CardRate.R: return "#00BFFF";
            case CardDataSO.CardRate.SR: return "#BA55D3";
            case CardDataSO.CardRate.SSR: return "#FFD700";
            default: return "#FFFFFF";
        }
    }
}