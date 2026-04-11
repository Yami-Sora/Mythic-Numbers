using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GemInventoryUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Transform gemContent;
    [SerializeField] private GameObject gemSlotPrefab;
    [SerializeField] private int minGemSlots = 24;

    // Hàm này để CardDetailManager gọi mỗi khi mở bảng lên
    public void RefreshGemList()
    {
        // 1. Dọn dẹp sạch sẽ
        foreach (Transform child in gemContent) Destroy(child.gameObject);

        // 2. Lấy danh sách ngọc từ "Nguồn cội" InventoryManager
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager instance not found!");
            return;
        }
        var gemList = InventoryManager.Instance.GetItemsByType(ItemDataSO.ItemType.Gem);

        // 3. Render Ngọc đang có
        foreach (var gemItem in gemList)
        {
            CreateSlot(gemItem, false);
        }

        // 4. Lấp đầy ô xám cho đủ hàng lối (Min 40 và chia hết cho 10)
        int currentCount = gemList.Count;
        int targetCount = Mathf.Max(minGemSlots, currentCount + (10 - (currentCount % 10)) % 10);
        int emptyNeeded = targetCount - currentCount;

        for (int i = 0; i < emptyNeeded; i++)
        {
            CreateSlot(null, true);
        }
    }

    private void CreateSlot(InventoryManager.InventoryItem item, bool isEmpty)
    {
        GameObject go = Instantiate(gemSlotPrefab, gemContent);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        UI_ItemSlot slotScript = go.GetComponent<UI_ItemSlot>();
        if (isEmpty) slotScript.SetupEmpty();
        else slotScript.Setup(item);
    }
}