using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class GachaPopupManager : MonoBehaviour
{
    public static GachaPopupManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private TextMeshProUGUI txtTitle;

    [Header("Prefabs")]
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private GameObject cardSlotPrefab;

    private readonly Vector3 ITEM_SCALE = Vector3.one;
    private readonly Vector3 CARD_SCALE = new Vector3(0.1f, 0.1f, 0.1f);

    private void Awake()
    {
        Instance = this;
        if (popupPanel != null) popupPanel.SetActive(false);
        gameObject.SetActive(false);
    }

    public void ShowGachaRewards(List<CardDataSO> cards, List<ItemDataSO> gems, string title = "KẾT QUẢ GACHA")
    {
        PreparePopup(title);

        int index = 0;

        // 1. Hiển thị Card
        if (cards != null)
        {
            foreach (var card in cards)
            {
                GameObject go = Instantiate(cardSlotPrefab, itemContainer);
                UI_CardSlot slot = go.GetComponent<UI_CardSlot>();
                if (slot != null) slot.Setup(new OwnedCard(card));
                
                AnimateSlotItem(go.transform, CARD_SCALE, index++);
            }
        }

        // 2. Hiển thị Gem
        if (gems != null)
        {
            foreach (var gem in gems)
            {
                GameObject go = Instantiate(itemSlotPrefab, itemContainer);
                UI_ItemSlot slot = go.GetComponent<UI_ItemSlot>();
                if (slot != null) slot.Setup(new InventoryItem(gem, 1));

                // Bật và ép thông số Amount theo chuẩn GemUpgrade (10x4, FontSize 3, Anchor BottomRight)
                var txtAmount = slot.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txtAmount != null)
                {
                    txtAmount.gameObject.SetActive(true);
                    txtAmount.text = "1"; // Gacha lẻ luôn là 1
                    txtAmount.fontSize = 3;
                    
                    RectTransform rt = txtAmount.rectTransform;
                    rt.anchorMin = new Vector2(1, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(1, 0);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(10f, 4f);
                }

                AnimateSlotItem(go.transform, ITEM_SCALE, index++);
            }
        }
    }

    public void ClosePopup()
    {
        this.gameObject.SetActive(false);
    }

    private void PreparePopup(string title)
    {
        gameObject.SetActive(true);
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            // Đưa Panel xuống cuối để nó đè lên toàn bộ Card/Gem trong hierarchy, hứng được Raycast
            popupPanel.transform.SetAsLastSibling(); 
            
            popupPanel.transform.localScale = Vector3.zero;
            popupPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        
        if (txtTitle != null) txtTitle.text = title;

        // Dọn rác cũ
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void AnimateSlotItem(Transform slotTransform, Vector3 targetScale, int index)
    {
        slotTransform.localScale = Vector3.zero;
        slotTransform.DOScale(targetScale, 0.4f)
            .SetDelay(index * 0.05f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }
}
