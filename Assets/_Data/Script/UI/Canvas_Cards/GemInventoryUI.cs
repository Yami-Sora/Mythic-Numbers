using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GemInventoryUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Transform gemContent;
    [SerializeField] private GameObject gemSlotPrefab;
    [SerializeField] private int minGemSlots = 24;
    [SerializeField] private int columns = 6;

    // Hàm này để CardDetailManager gọi mỗi khi mở bảng lên
    public void RefreshGemList()
    {
        // 1. Dọn rác (Duyệt ngược)
        for (int i = gemContent.childCount - 1; i >= 0; i--)
        {
            Transform child = gemContent.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        if (InventoryManager.Instance == null) return;
        var gemList = InventoryManager.Instance.GetItemsByType(ItemDataSO.ItemType.Gem);

        // 2. Render Ngọc
        foreach (var gemItem in gemList)
        {
            CreateSlot(gemItem, false);
        }

        // 3. Logic Đệm ô xám thông minh
        int currentCount = gemList.Count;
        int emptyNeeded = 0;

        if (currentCount < minGemSlots)
        {
            emptyNeeded = minGemSlots - currentCount;
        }
        else
        {
            // Sử dụng biến columns thay vì số cứng
            int remainder = currentCount % columns;
            if (remainder > 0)
            {
                emptyNeeded = columns - remainder;
            }
        }

        for (int i = 0; i < emptyNeeded; i++)
        {
            CreateSlot(null, true);
        }
    }

    private void CreateSlot(InventoryItem item, bool isEmpty)
    {
        GameObject go = Instantiate(gemSlotPrefab, gemContent);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        UI_ItemSlot slotScript = go.GetComponent<UI_ItemSlot>();
        if (isEmpty) slotScript.SetupEmpty();
        else slotScript.Setup(item);
    }
}