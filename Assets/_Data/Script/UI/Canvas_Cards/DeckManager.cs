using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        
        RefreshDeckUI();
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

    public bool EquipCard(OwnedCard cardToEquip)
    {
        for (int i = 0; i < maxDeckSize; i++)
        {
            // So sánh cardID thông qua .data
            if (currentDeck[i] != null && currentDeck[i].data.cardID == cardToEquip.data.cardID)
            {
                return false;
            }
        }

        for (int i = 0; i < maxDeckSize; i++)
        {
            if (currentDeck[i] == null)
            {
                currentDeck[i] = cardToEquip; // Lưu thẳng Reference (Tham chiếu)
                RefreshDeckUI();
                return true;
            }
        }
        return false;
    }

    public void UnequipCard(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < maxDeckSize && currentDeck[slotIndex] != null)
        {
            currentDeck[slotIndex] = null;
            RefreshDeckUI();
        }
    }

    public void RefreshDeckUI()
    {
        for (int i = 0; i < maxDeckSize; i++)
        {
            if (deckSlots[i] != null)
            {
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
    }
}