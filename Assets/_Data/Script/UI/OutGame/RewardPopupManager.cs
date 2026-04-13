using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RewardPopupManager : MonoBehaviour
{
    public static RewardPopupManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;      // Nguyên cái Panel bảng thưởng
    [SerializeField] private Transform itemContainer;    // Nơi chứa các slot vật phẩm nhận được
    [SerializeField] private GameObject itemSlotPrefab;  // Prefab UI_ItemSlot (dùng chung cho đồng bộ)
    [SerializeField] private TextMeshProUGUI txtTitle;   // Tiêu đề (VD: "CHÚC MỪNG", "NHẬN THƯỞNG")


    private void Awake()
    {
        Instance = this;
        popupPanel.SetActive(false);
    }

    // --- HÀM TRỌNG TÂM: MỞ BẢNG THƯỞNG ---
    // Sếp có thể gửi 1 item hoặc 1 list item vào đây
    public void ShowRewards(List<InventoryItem> rewards, string title = "BẠN ĐÃ NHẬN ĐƯỢC")
    {
        this.gameObject.SetActive(true);
        if (rewards == null || rewards.Count == 0) return;

        txtTitle.text = title;

        // 1. Dọn rác container cũ (Duyệt ngược cho chắc kèo)
        for (int i = itemContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = itemContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        // 2. Render danh sách phần thưởng
        foreach (var item in rewards)
        {
            GameObject go = Instantiate(itemSlotPrefab, itemContainer);
            go.GetComponent<RectTransform>().localScale = Vector3.one;

            UI_ItemSlot slot = go.GetComponent<UI_ItemSlot>();
            slot.Setup(item);

            // Hiện số lượng (với ngọc ghép xong thì thường là 1, nhưng quà nhiệm vụ có thể nhiều)
            var txtAmount = slot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txtAmount != null)
            {
                txtAmount.gameObject.SetActive(true);
                txtAmount.text = item.amount > 1 ? $"x{item.amount}" : "";
            }
        }

        // 3. Hiện bảng
        popupPanel.SetActive(true);

        // (Tùy chọn) Sếp có thể thêm tí hiệu ứng Anime.js hoặc DoTween cho nó nổ ra cho sướng mắt
    }

    public void ClosePopup()
    {
        this.gameObject.SetActive(false);
    }
}