using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("Cấu hình Đội Hình")]
    public int maxDeckSize = 5;

    public OwnedCard[] currentDeck;
    public UI_CardSlot[] deckSlots;

    [Header("UI Controls (Trạng Thái Xếp Bài)")]
    public Button btnEditDeck;
    public Button btnSaveDeck;
    public Button btnCancelEdit;

    public bool isEditingDeck { get; private set; } = false;

    // Đổi kiểu dữ liệu khay Backup luôn
    private OwnedCard[] backupDeck;

    private void Awake()
    {
        Instance = this;
        currentDeck = new OwnedCard[maxDeckSize];
        backupDeck = new OwnedCard[maxDeckSize];

        if (btnEditDeck != null) btnEditDeck.onClick.AddListener(EnterEditMode);
        if (btnSaveDeck != null) btnSaveDeck.onClick.AddListener(SaveDeck);
        if (btnCancelEdit != null) btnCancelEdit.onClick.AddListener(CancelEdit);
    }

    private void OnEnable()
    {
        RefreshDeckUI();
        CancelEdit();
    }

    /// <summary>
    /// Kiểm tra xem sếp đã lắp đủ 5 lá bài chưa
    /// </summary>
    public bool IsDeckFull()
    {
        if (currentDeck == null) return false;
        for (int i = 0; i < maxDeckSize; i++)
        {
            if (currentDeck[i] == null || currentDeck[i].data == null) return false;
        }
        return true;
    }

    public void EnterEditMode()
    {
        isEditingDeck = true;

        for (int i = 0; i < maxDeckSize; i++) backupDeck[i] = currentDeck[i];

        if (btnEditDeck != null) btnEditDeck.gameObject.SetActive(false);
        if (btnSaveDeck != null) btnSaveDeck.gameObject.SetActive(true);
        if (btnCancelEdit != null) btnCancelEdit.gameObject.SetActive(true);

        RefreshDeckUI();
    }

    public void SaveDeck()
    {
        isEditingDeck = false;

        if (btnEditDeck != null) btnEditDeck.gameObject.SetActive(true);
        if (btnSaveDeck != null) btnSaveDeck.gameObject.SetActive(false);
        if (btnCancelEdit != null) btnCancelEdit.gameObject.SetActive(false);

        PlayFabDataManager.Instance.SaveCurrentDeck(currentDeck);
        LocalDeckContext.SetDeck(currentDeck);

        RefreshDeckUI();

        PlayerInfoUI.UpdateAllPower();
    }

    public void CancelEdit()
    {
        if (isEditingDeck)
        {
            for (int i = 0; i < maxDeckSize; i++) currentDeck[i] = backupDeck[i];
        }

        isEditingDeck = false;

        if (btnEditDeck != null) btnEditDeck.gameObject.SetActive(true);
        if (btnSaveDeck != null) btnSaveDeck.gameObject.SetActive(false);
        if (btnCancelEdit != null) btnCancelEdit.gameObject.SetActive(false);

        RefreshDeckUI();
    }

    // ==========================================
    // LOGIC LẮP BÀI (THÊM DOTWEEN)
    // ==========================================
    public bool EquipCard(OwnedCard cardToEquip)
    {
        // [BỌC THÉP TẦNG 1]: Thẻ định lắp vào mà rỗng ruột thì sút nó ra luôn!
        if (cardToEquip == null || cardToEquip.data == null)
        {
            Debug.LogWarning("[DeckManager] Lỗi: Thẻ định lắp bị rỗng Data!");
            return false;
        }

        for (int i = 0; i < maxDeckSize; i++)
        {
            // [BỌC THÉP TẦNG 2]: Quét xem mấy thẻ ĐANG NẰM SẴN trong Deck có cái nào bị ma nhập không
            if (currentDeck[i] != null && currentDeck[i].data != null)
            {
                // Nếu thẻ bình thường, check xem ID có bị trùng với thẻ đang định lắp không
                if (currentDeck[i].data.cardID == cardToEquip.data.cardID)
                {
                    return false; // Bị trùng ID rồi sếp! Không cho lắp 2 lá giống nhau.
                }
            }
        }

        // Bắt đầu tìm ô trống để nhét thẻ vào
        for (int i = 0; i < maxDeckSize; i++)
        {
            // [BỌC THÉP TẦNG 3]: Nếu ô trống, HOẶC ô đó chứa thẻ lỗi (data null), thì cứ thẳng tay đè thẻ mới lên!
            if (currentDeck[i] == null || currentDeck[i].data == null)
            {
                currentDeck[i] = cardToEquip; // Lưu thẳng Reference
                RefreshDeckUI();

                // [DOTWEEN]: Hiệu ứng nảy "Pưng"
                if (deckSlots[i] != null)
                {
                    Transform slotTrans = deckSlots[i].transform;
                    slotTrans.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                    slotTrans.DOScale(0.8f, 0.4f).SetEase(Ease.OutBack);
                }

                return true;
            }
        }
        return false;
    }

    // ==========================================
    // LOGIC GỠ BÀI (THÊM DOTWEEN)
    // ==========================================
    public void UnequipCard(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < maxDeckSize && currentDeck[slotIndex] != null)
        {
            if (deckSlots[slotIndex] != null)
            {
                Transform slotTrans = deckSlots[slotIndex].transform;

                // [DOTWEEN]: Hút nhỏ thẻ bài về 0 rồi mới gỡ data để tạo cảm giác "cất đi"
                slotTrans.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    currentDeck[slotIndex] = null;
                    RefreshDeckUI();

                    // Xóa xong phải trả lại Scale = 0.8 cho cái ô trống (khung xương) hiển thị
                    slotTrans.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                });
            }
            else
            {
                // Fallback nếu thiếu UI
                currentDeck[slotIndex] = null;
                RefreshDeckUI();
            }
        }
    }

    public void RefreshDeckUI()
    {
        for (int i = 0; i < maxDeckSize; i++)
        {
            if (deckSlots[i] != null)
            {
                // [BỌC THÉP]: Đảm bảo mọi ô khi load lại đều phải có Scale = 1, tránh bị dính DOTween cũ làm tàng hình
                deckSlots[i].transform.DOKill(); // Ngắt mọi hiệu ứng cũ (nếu có) đang chạy dở
                deckSlots[i].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f); // Scale mặc định cho ô trống

                if (currentDeck[i] != null)
                {
                    deckSlots[i].gameObject.SetActive(true);
                    deckSlots[i].Setup(currentDeck[i]);
                }
                else
                {
                    deckSlots[i].ClearSlot();
                }
            }
        }

        if (CardListManager.Instance != null && CardListManager.Instance.gameObject.activeInHierarchy)
        {
            CardListManager.Instance.DisplayCards();
        }

        PlayerInfoUI.UpdateAllPower();
    }
}