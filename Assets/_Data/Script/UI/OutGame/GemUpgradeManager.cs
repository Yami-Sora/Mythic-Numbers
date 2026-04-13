using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class GemUpgradeManager : MonoBehaviour
{
    public static GemUpgradeManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Transform upgradeContainer;
    [SerializeField] private GameObject gemSlotPrefab;
    [SerializeField] private Button btnQuickUpgrade;
    [SerializeField] private Button btnClose;

    private void Awake()
    {
        Instance = this;
        if (btnQuickUpgrade) btnQuickUpgrade.onClick.AddListener(OnQuickUpgradeClicked);
        if (btnClose) btnClose.onClick.AddListener(ClosePanel);
        gameObject.SetActive(false);
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        RefreshUpgradeList();
    }

    public void ClosePanel() => gameObject.SetActive(false);

    private void RefreshUpgradeList()
    {
        // 1. Dọn dẹp an toàn
        for (int i = upgradeContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = upgradeContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        if (InventoryManager.Instance == null) return;

        // 2. Tìm tất cả ngọc đủ điều kiện ghép (>4 viên và KHÔNG PHẢI cấp max - tức là có nextLevelGem)
        var readyToUpgradeGroups = GetReadyUpgradeGroups();

        bool canUpgradeAny = readyToUpgradeGroups.Count > 0;
        if (btnQuickUpgrade) btnQuickUpgrade.interactable = canUpgradeAny;

        // 3. Render ra UI
        foreach (var group in readyToUpgradeGroups)
        {
            GameObject go = Instantiate(gemSlotPrefab, upgradeContainer);
            go.GetComponent<RectTransform>().localScale = Vector3.one;

            UI_ItemSlot slot = go.GetComponent<UI_ItemSlot>();

            InventoryItem displayItem = new InventoryItem(group.Key, group.Count());
            slot.Setup(displayItem);

            var txtAmount = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (txtAmount != null)
            {
                txtAmount.gameObject.SetActive(true);
                txtAmount.text = group.Count().ToString();
            }
        }
    }

    // Tách riêng hàm lọc để tái sử dụng
    private List<IGrouping<ItemDataSO, InventoryItem>> GetReadyUpgradeGroups()
    {
        var allGems = InventoryManager.Instance.GetItemsByType(ItemDataSO.ItemType.Gem);
        return allGems
            .GroupBy(g => g.data)
            .Where(group => group.Count() >= 4 && group.Key.nextLevelGem != null) // Chặn luôn ngọc cấp Max
            .ToList();
    }

    private void OnQuickUpgradeClicked()
    {
        int totalMerges = 0;
        bool keepMerging = true;

        // --- BÍ KÍP: SỔ KẾ TOÁN GHI CHÉP THU CHI ---
        Dictionary<ItemDataSO, int> netGemChanges = new Dictionary<ItemDataSO, int>();

        // Vòng lặp đập lò ngầm
        while (keepMerging)
        {
            var readyGroups = GetReadyUpgradeGroups();

            if (readyGroups.Count == 0) break; // Hết đồ ép

            foreach (var group in readyGroups)
            {
                ItemDataSO currentData = group.Key;
                int totalOwned = group.Count();
                int mergesPossible = totalOwned / 4;
                int gemsToConsume = mergesPossible * 4;

                // 1. Ghi sổ: Bị trừ đi Ngọc cấp thấp
                if (!netGemChanges.ContainsKey(currentData)) netGemChanges[currentData] = 0;
                netGemChanges[currentData] -= gemsToConsume;

                // 2. Ghi sổ: Được cộng Ngọc cấp cao
                ItemDataSO nextData = currentData.nextLevelGem;
                if (!netGemChanges.ContainsKey(nextData)) netGemChanges[nextData] = 0;
                netGemChanges[nextData] += mergesPossible;

                // 3. Thực thi Xóa/Thêm vào túi đồ (ẩn UI)
                var gemsToRemove = group.Take(gemsToConsume).ToList();
                foreach (var gem in gemsToRemove)
                {
                    InventoryManager.Instance.RemoveItem(gem, isSilent: true);
                }
                InventoryManager.Instance.AddItem(nextData, mergesPossible, isSilent: true);

                totalMerges += mergesPossible;
            }
        }

        if (totalMerges > 0)
        {
            Debug.Log($"<color=magenta>[Lò Rèn] Ầm ầm! Đã luyện hóa {totalMerges} ngọc trong chớp mắt!</color>");

            // --- LỌC RA PHẦN THƯỞNG CUỐI CÙNG TỪ SỔ KẾ TOÁN ---
            List<InventoryItem> finalRewards = new List<InventoryItem>();
            foreach (var kvp in netGemChanges)
            {
                // Chỉ lấy những cục ngọc có số dư > 0 (Tức là hàng sinh ra ở vòng cuối cùng)
                if (kvp.Value > 0)
                {
                    finalRewards.Add(new InventoryItem(kvp.Key, kvp.Value));
                }
            }

            // --- HIỆN BẢNG REWARD ---
            if (RewardPopupManager.Instance != null && finalRewards.Count > 0)
            {
                RewardPopupManager.Instance.ShowRewards(finalRewards, "ĐÚC NGỌC THÀNH CÔNG");
            }
        }
        this.gameObject.SetActive(false);
        // --- CHỐT HẠ: REFRESH UI ĐÚNG 1 LẦN ---
        RefreshUpgradeList();
        if (InventoryManager.Instance != null) InventoryManager.Instance.RefreshUI();
        if (CardDetailManager.Instance != null && CardDetailManager.Instance.gameObject.activeInHierarchy)
        {
            CardDetailManager.Instance.RefreshRightGemInventory();
        }
    }
}