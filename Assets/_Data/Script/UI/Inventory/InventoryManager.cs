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

    private Color _normalColor = Color.white;
    private Color _selectedColor = new Color(1f, 0.84f, 0f); // Vàng kim (Gold)

    // CHUYỂN SANG LIST: Để một loại ItemID có thể nằm trên nhiều ô khác nhau
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
        ShowAll(); // Vào game là hiện luôn tab Tất Cả
    }

    private void Update()
    {
        // Bắt phím Space hệ Input System mới
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            HandleCheatItems();
        }
    }
    public List<InventoryItem> GetInventoryList()
    {
        return _inventoryList;
    }

    // 2. Hàm lọc đồ theo loại (Gem, Prop...) - Tiện cho việc khảm ngọc
    public List<InventoryItem> GetItemsByType(ItemDataSO.ItemType type)
    {
        return _inventoryList.Where(i => i.data.type == type).ToList();
    }
    private void HandleCheatItems()
    {
        if (testItemsArray == null || testItemsArray.Length == 0) return;

        // Hack mỗi lần 30 món, mỗi món nhảy random 1-50 cái để sếp thấy nó tràn ô cho sướng
        for (int i = 0; i < 30; i++)
        {
            int randomIndex = Random.Range(0, testItemsArray.Length);
            AddItem(testItemsArray[randomIndex], Random.Range(1, 51));
        }
        Debug.Log("<color=cyan>[Hack] Mưa sao băng đã rơi vào túi!</color>");
    }

    [System.Serializable]
    public class InventoryItem
    {
        public ItemDataSO data;
        public int amount;

        public InventoryItem(ItemDataSO data, int amount)
        {
            this.data = data;
            this.amount = amount;
        }
    }

    // ==========================================
    // 1. PHẦN XỬ LÝ LOGIC DATA (TRÀN STACK)
    // ==========================================
    public void AddItem(ItemDataSO itemData, int count)
    {
        int remaining = count;

        // BƯỚC A: Tìm trong túi xem có ô nào cùng loại mà CHƯA ĐẦY không?
        foreach (var item in _inventoryList)
        {
            if (item.data.itemID == itemData.itemID && item.amount < itemData.maxStack)
            {
                int canAdd = itemData.maxStack - item.amount; // Chỗ trống còn lại trong ô này
                int toAdd = Mathf.Min(remaining, canAdd);

                item.amount += toAdd;
                remaining -= toAdd;

                if (remaining <= 0) break;
            }
        }

        // BƯỚC B: Nếu vẫn còn dư đồ (do ô cũ đầy hoặc chưa có ô nào), tạo ô mới
        while (remaining > 0)
        {
            int toAdd = Mathf.Min(remaining, itemData.maxStack);
            _inventoryList.Add(new InventoryItem(itemData, toAdd));
            remaining -= toAdd;
        }

        RefreshUI();
    }

    // ==========================================
    // 2. PHẦN XỬ LÝ GIAO DIỆN UI
    // ==========================================
    public void DisplayInventory(ItemDataSO.ItemType? filterType = null)
    {
        _currentTab = filterType;

        // Dọn dẹp grid cũ
        foreach (Transform child in slotContainer) Destroy(child.gameObject);

        // Lọc danh sách theo Tab
        var itemsToShow = filterType == null
            ? _inventoryList
            : _inventoryList.Where(i => i.data.type == filterType).ToList();

        // 3. Render các ô CÓ ĐỒ
        foreach (var item in itemsToShow)
        {
            CreateSlotUI(item, false);
        }

        // 4. Render các ô TRỐNG (Tối thiểu 100 ô, lấp đầy hàng 10)
        int totalDisplay = itemsToShow.Count;
        int minSlots = 40;
        int targetSlots = Mathf.Max(minSlots, totalDisplay + (10 - (totalDisplay % 10)) % 10);
        int emptySlotsNeeded = targetSlots - totalDisplay;

        for (int i = 0; i < emptySlotsNeeded; i++)
        {
            CreateSlotUI(null, true);
        }
    }

    private void CreateSlotUI(InventoryItem item, bool isEmpty)
    {
        GameObject go = Instantiate(slotPrefab, slotContainer, false);

        // Trấn áp Scale và Size
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

    private void RefreshUI() => DisplayInventory(_currentTab);

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
}