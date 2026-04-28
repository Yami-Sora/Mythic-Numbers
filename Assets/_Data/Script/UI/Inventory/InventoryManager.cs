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
    public Image imgTabProps;
    public Image imgTabGems;

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
        ShowProps(); // Mặc định vào túi là xem Đạo cụ trước
    }

    private void Update()
    {
        // Check phím Space cho PC (giữ nguyên cho Yami test trên máy)
        if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
        {
            HandleCheatItems();
        }

        // Check Double Tap cho Mobile (Ngắn - Gọn - Đúng hệ)
        var touch = Touchscreen.current?.primaryTouch;
        if (touch != null && touch.press.wasPressedThisFrame && touch.tapCount.ReadValue() == 2)
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

        foreach (var item in testItemsArray)
        {
            AddItem(item, Random.Range(1, 51), true); // isSilent = true để tránh lag UI khi add nhiều
        }
        RefreshUI();
        
        Debug.Log("<color=cyan>[Hack] Mưa sao băng lại rơi vào túi (Tất cả test items)!</color>");
        
        if (PlayFabDataManager.Instance != null)
        {
            PlayFabDataManager.Instance.MarkDirty();
        }
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

        if (!isSilent) 
        {
            RefreshUI();
            if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.MarkDirty();
        }
    }

    // ==========================================
    // 2. PHẦN XỬ LÝ GIAO DIỆN UI
    // ==========================================
    public void DisplayInventory(ItemDataSO.ItemType? filterType = null)
    {
        _currentTab = filterType;

        if (slotContainer == null) return;

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
    public void ShowProps() { DisplayInventory(ItemDataSO.ItemType.Prop); HighlightTab(imgTabProps); }
    public void ShowGems() { DisplayInventory(ItemDataSO.ItemType.Gem); HighlightTab(imgTabGems); }

    private void HighlightTab(Image activeTabImg)
    {
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
    // ==========================================
    // CÁC HÀM TIỆN ÍCH LẤY DATA (ĐÃ NÂNG CẤP)
    // ==========================================

    // Lấy 1 cục InventoryItem đầu tiên khớp với ID 
    public InventoryItem GetItem(string itemID)
    {
        return _inventoryList.FirstOrDefault(i => i.data != null && i.data.itemID == itemID);
    }

    // [NEW] Hàm "Gom Bi": Cộng dồn số lượng của TẤT CẢ các slot chứa chung 1 loại item
    public int GetTotalItemAmount(string itemID)
    {
        int total = 0;
        foreach (var item in _inventoryList)
        {
            if (item != null && item.data != null && item.data.itemID == itemID)
            {
                total += item.amount;
            }
        }
        return total;
    }

    // [NÂNG CẤP]: Hàm trừ item bây giờ có thể trừ xuyên qua nhiều Slot khác nhau!
    public bool RemoveItemAmount(string itemID, int amountToRemove)
    {
        // 1. Kiểm tra xem tổng tài sản có đủ để trừ không
        int totalAmount = GetTotalItemAmount(itemID);
        if (totalAmount < amountToRemove) return false;

        int remainingToRemove = amountToRemove;

        // 2. Đi lùng sục và vặt lông từng Slot một cho đến khi đủ số lượng
        for (int i = _inventoryList.Count - 1; i >= 0; i--) // Quét ngược từ cuối lên cho an toàn khi Remove
        {
            var item = _inventoryList[i];
            if (item != null && item.data != null && item.data.itemID == itemID)
            {
                if (item.amount <= remainingToRemove)
                {
                    // Ô này không đủ hoặc vừa đủ -> Húp trọn ổ rồi Xóa Slot
                    remainingToRemove -= item.amount;
                    _inventoryList.RemoveAt(i);
                }
                else
                {
                    // Ô này có nhiều hơn mức cần -> Trừ đi lượng cần thiết và giữ lại Slot
                    item.amount -= remainingToRemove;
                    remainingToRemove = 0;
                }

                if (remainingToRemove <= 0) break; // Đủ chỉ tiêu thì dừng tay
            }
        }

        RefreshUI();

        // 3. Trừ đồ xong phải Save lên mây ngay cho nóng!
        if (PlayFabDataManager.Instance != null) 
        {
            PlayFabDataManager.Instance.MarkDirty();
            PlayFabDataManager.Instance.SaveGameData();
        }

        return true;
    }
}