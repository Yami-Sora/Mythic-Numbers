using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryManager : YamiMonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Tab Visuals")]
    [SerializeField] private Image imgTabAll;
    [SerializeField] private Image imgTabGems;
    [SerializeField] private Image imgTabProps;

    [Header("Layout Settings")]
    [SerializeField] private int minSlots = 40;
    [SerializeField] private int columns = 10;

    private Color _normalColor = Color.white;
    private Color _selectedColor = new Color(1f, 0.84f, 0f); // Vàng kim (Gold)

    private List<InventoryItem> _inventoryList = new List<InventoryItem>();
    private ItemDataSO.ItemType? _currentTab = null;

    [Header("--- HÀNG NÓNG ĐỂ TEST ---")]
    public ItemDataSO[] testItemsArray;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();
        ShowAll();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            HandleCheatItems();
        }
    }

    public List<InventoryItem> GetInventoryList()
    {
        return _inventoryList;
    }

    public List<InventoryItem> GetItemsByType(ItemDataSO.ItemType type)
    {
        return _inventoryList.Where(i => i.data.type == type).ToList();
    }

    private void HandleCheatItems()
    {
        if (testItemsArray == null || testItemsArray.Length == 0) return;

        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, testItemsArray.Length);
            AddItem(testItemsArray[randomIndex], Random.Range(1, 51));
        }
        Debug.Log("<color=cyan>[Hack] Mưa sao băng đã rơi vào túi!</color>");
    }

    // ==========================================
    // 1. PHẦN XỬ LÝ LOGIC DATA (ÉP STACK 1 CHO NGỌC)
    // ==========================================
    public void AddItem(ItemDataSO itemData, int count, bool isSilent = false)
    {
        int remaining = count;

        int currentMaxStack = (itemData.type == ItemDataSO.ItemType.Gem) ? 1 : itemData.maxStack;

        foreach (var item in _inventoryList)
        {
            if (item.data.itemID == itemData.itemID && item.amount < currentMaxStack)
            {
                int canAdd = currentMaxStack - item.amount;
                int toAdd = Mathf.Min(remaining, canAdd);

                item.amount += toAdd;
                remaining -= toAdd;

                if (remaining <= 0) break;
            }
        }

        while (remaining > 0)
        {
            int toAdd = Mathf.Min(remaining, currentMaxStack);
            _inventoryList.Add(new InventoryItem(itemData, toAdd));
            remaining -= toAdd;
        }

        if (!isSilent) RefreshUI();
    }

    // ==========================================
    // 2. PHẦN XỬ LÝ GIAO DIỆN UI
    // ==========================================
    public void DisplayInventory(ItemDataSO.ItemType? filterType = null)
    {
        _currentTab = filterType;

        for (int i = slotContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = slotContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        var itemsToShow = filterType == null
            ? _inventoryList
            : _inventoryList.Where(i => i.data.type == filterType).ToList();

        foreach (var item in itemsToShow)
        {
            CreateSlotUI(item, false);
        }

        int totalDisplay = itemsToShow.Count;
        int emptySlotsNeeded = 0;

        if (totalDisplay < minSlots)
        {
            emptySlotsNeeded = minSlots - totalDisplay;
        }
        else
        {
            int remainder = totalDisplay % columns;
            if (remainder > 0)
            {
                emptySlotsNeeded = columns - remainder;
            }
        }

        for (int i = 0; i < emptySlotsNeeded; i++)
        {
            CreateSlotUI(null, true);
        }
    }

    private void CreateSlotUI(InventoryItem item, bool isEmpty)
    {
        GameObject go = Instantiate(slotPrefab, slotContainer, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(150f, 150f);
        rect.anchoredPosition3D = Vector3.zero;

        UI_ItemSlot slotScript = go.GetComponent<UI_ItemSlot>();
        if (isEmpty)
            slotScript.SetupEmpty();
        else
            slotScript.Setup(item);
    }

    public void RefreshUI() => DisplayInventory(_currentTab);

    // ==========================================
    // 3. XỬ LÝ NÚT BẤM VÀ ĐỔI MÀU
    // ==========================================
    public void ShowAll() { DisplayInventory(null); HighlightTab(imgTabAll); }
    public void ShowGems() { DisplayInventory(ItemDataSO.ItemType.Gem); HighlightTab(imgTabGems); }
    public void ShowProps() { DisplayInventory(ItemDataSO.ItemType.Prop); HighlightTab(imgTabProps); }

    private void HighlightTab(Image activeTabImg)
    {
        if (imgTabAll) imgTabAll.color = _normalColor;
        if (imgTabGems) imgTabGems.color = _normalColor;
        if (imgTabProps) imgTabProps.color = _normalColor;

        if (activeTabImg) activeTabImg.color = _selectedColor;
    }

    public void RemoveItem(InventoryItem itemToRemove, bool isSilent = false)
    {
        if (_inventoryList.Contains(itemToRemove))
        {
            _inventoryList.Remove(itemToRemove);
            if (!isSilent) RefreshUI();
        }
    }
}