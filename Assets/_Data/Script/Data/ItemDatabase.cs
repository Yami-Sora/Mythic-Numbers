using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ItemDatabase : YamiMonoBehaviour
{
    public static ItemDatabase Instance;

    [SerializeField] private ItemDataSO[] allItems;
    private Dictionary<string, ItemDataSO> _itemLookup;

    protected override void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        base.Awake();
        InitializeDictionary();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        // Giả sử sếp lưu các file ngọc ở folder: Resources/ItemDataSO
        if (allItems == null || allItems.Length == 0)
        {
            allItems = Resources.LoadAll<ItemDataSO>("ItemDataSO");
            Debug.LogWarning("Đã tự động nạp Items từ Resources!");
        }
    }

    private void InitializeDictionary()
    {
        _itemLookup = new Dictionary<string, ItemDataSO>();
        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemID) && !_itemLookup.ContainsKey(item.itemID))
            {
                _itemLookup.Add(item.itemID, item);
            }
        }
        Debug.Log($"[ItemDatabase] Nạp thành công {_itemLookup.Count} vật phẩm.");
    }

    public ItemDataSO GetItemData(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_itemLookup != null && _itemLookup.ContainsKey(id)) return _itemLookup[id];
        return null;
    }
}