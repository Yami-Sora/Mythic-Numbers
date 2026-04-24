using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class RewardPopupManager : MonoBehaviour
{
    public static RewardPopupManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private TextMeshProUGUI txtTitle;

    [Header("Gacha Reward Settings")]
    [SerializeField] private GameObject cardSlotPrefab;

    // Kích thước chuẩn để DOTween phóng to tới (Tránh hardcode lặp lại)
    private readonly Vector3 ITEM_SCALE = Vector3.one;
    private readonly Vector3 CARD_SCALE = new Vector3(0.08f, 0.08f, 0.08f);

    private void Awake()
    {
        Instance = this;
        popupPanel.SetActive(false);
        gameObject.SetActive(false);
    }

    // ========================================================
    // 1. CÁC HÀM PUBLIC GỌI TỪ BÊN NGOÀI
    // ========================================================

    public void ShowRewards(List<InventoryItem> rewards, string title = "BẠN ĐÃ NHẬN ĐƯỢC")
    {
        if (rewards == null || rewards.Count == 0) return;

        PreparePopup(title);

        for (int i = 0; i < rewards.Count; i++)
        {
            var item = rewards[i];
            GameObject go = Instantiate(itemSlotPrefab, itemContainer);

            UI_ItemSlot slot = go.GetComponent<UI_ItemSlot>();
            slot.Setup(item);

            // Bật và ép thông số Amount theo chuẩn GemUpgrade (10x4, FontSize 3, Anchor BottomRight)
            var txtAmount = slot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txtAmount != null)
            {
                txtAmount.gameObject.SetActive(true);
                txtAmount.text = item.amount.ToString(); // Gán số lượng thật
                txtAmount.fontSize = 3;
                
                RectTransform rt = txtAmount.rectTransform;
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(10f, 4f);
            }

            // Gọi hiệu ứng nổ cho cục ngọc
            AnimateSlotItem(go.transform, ITEM_SCALE, i);
        }
    }

    public void ShowCardRewards(List<CardDataSO> cards, string title = "KẾT QUẢ GACHA")
    {
        if (cards == null || cards.Count == 0) return;

        PreparePopup(title);

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            GameObject go = Instantiate(cardSlotPrefab, itemContainer);

            UI_CardSlot slot = go.GetComponent<UI_CardSlot>();
            if (slot != null) slot.Setup(new OwnedCard(card));

            // Gọi hiệu ứng nổ cho lá bài
            AnimateSlotItem(go.transform, CARD_SCALE, i);
        }
    }

    public void ClosePopup()
    {
        // Hiệu ứng thu nhỏ bảng trước khi tắt
        popupPanel.transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .SetUpdate(true) // Vẫn chạy kể cả khi timeScale = 0
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                popupPanel.SetActive(false);
            });
    }

    // ========================================================
    // 2. CÁC HÀM HELPER NỘI BỘ (CLEAN CODE)
    // ========================================================

    /// <summary>
    /// Dọn dẹp rác cũ, đổi Title và tạo hiệu ứng bật Popup
    /// </summary>
    private void PreparePopup(string title)
    {
        gameObject.SetActive(true);
        popupPanel.SetActive(true);
        txtTitle.text = title;

        // Dọn rác an toàn
        for (int i = itemContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(itemContainer.GetChild(i).gameObject);
        }

        // DOTween: Nguyên cái bảng đập vào mặt
        popupPanel.transform.localScale = Vector3.zero;
        popupPanel.transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    /// <summary>
    /// Hiệu ứng nổ dây chuyền cho từng vật phẩm / thẻ bài
    /// </summary>
    private void AnimateSlotItem(Transform slotTransform, Vector3 targetScale, int index)
    {
        slotTransform.localScale = Vector3.zero;

        slotTransform.DOScale(targetScale, 0.4f)
            .SetDelay(index * 0.1f) // Nổ lần lượt: Cái thứ 1 đợi 0s, cái thứ 2 đợi 0.1s...
            .SetEase(Ease.OutBack)  // Nảy nảy nhún nhún
            .SetUpdate(true);
    }
}