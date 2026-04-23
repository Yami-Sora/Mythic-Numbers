using UnityEngine;
using System.Collections.Generic;
using System;

public class Canvas_NavigationManager : MonoBehaviour
{
    public static Canvas_NavigationManager Instance { get; private set; }

    [Header("Canvas Tabs")]
    [SerializeField] private GameObject shopCanvas;
    [SerializeField] private GameObject cardsCanvas;
    [SerializeField] private GameObject combatCanvas;
    [SerializeField] private GameObject dungeonCanvas;
    [SerializeField] private GameObject inventoryCanvas;
    [SerializeField] private GameObject currencyPanel;
    [SerializeField] private GameObject playerInfoCanvas;

    // [CLEAN CODE 1]: Gom tất cả Popup vào 1 cái túi (Mảng). Thêm bớt gì cứ kéo thả trên Inspector!
    [Header("Global Popups")]
    [SerializeField] private GameObject[] allGlobalPopups;

    private GameObject _currentActiveTab;
    private Dictionary<TabType, GameObject> _tabDictionary;

    public enum TabType { Shop, Cards, Combat, Dungeon, Inventory }

    // [CLEAN CODE 2]: Tạo một cái Cổng Phát Thanh (Event). Ai thích hóng biến thì đăng ký vào đây.
    public static event Action<TabType> OnTabChanged;

    private void Awake()
    {
        Instance = this;

        if (shopCanvas) shopCanvas.SetActive(true);
        if (cardsCanvas) cardsCanvas.SetActive(true);
        if (combatCanvas) combatCanvas.SetActive(true);
        if (dungeonCanvas) dungeonCanvas.SetActive(true);
        if (inventoryCanvas) inventoryCanvas.SetActive(true);

        // Bật hết Popup lên để nó init (Awake)
        SetAllPopupsState(true);

        InitDictionary();
    }

    private void Start()
    {
        // Init xong thì tắt sạch Popup đi
        SetAllPopupsState(false);

        foreach (var tab in _tabDictionary.Values)
        {
            if (tab != null) tab.SetActive(false);
        }

        SwitchTab(TabType.Combat);
    }

    private void InitDictionary()
    {
        _tabDictionary = new Dictionary<TabType, GameObject>
        {
            { TabType.Shop, shopCanvas },
            { TabType.Cards, cardsCanvas },
            { TabType.Combat, combatCanvas },
            { TabType.Dungeon, dungeonCanvas },
            { TabType.Inventory, inventoryCanvas }
        };
    }

    [Header("Navigation")]
    [SerializeField] private TabButton[] allTabButtons;

    public void SwitchTab(TabType targetTab)
    {
        // 1. Dập hết mọi popup đang mở trên màn hình
        SetAllPopupsState(false);

        TransitionManager.Instance.PlayTransition(() => {

            // 2. Chuyển đổi Tab (Tắt cũ, Bật mới)
            if (_currentActiveTab != null) _currentActiveTab.SetActive(false);

            if (_tabDictionary.TryGetValue(targetTab, out GameObject targetGO))
            {
                targetGO.SetActive(true);
                _currentActiveTab = targetGO;
            }

            UpdateTabButtons(targetTab);

            // [HIỂN THỊ TIỀN TỆ]: Ẩn khi vào Thẻ bài, hiện ở các chỗ khác
            if (currencyPanel != null)
            {
                currencyPanel.SetActive(targetTab != TabType.Cards);
            }

            if (playerInfoCanvas != null)
            {
                playerInfoCanvas.SetActive(targetTab == TabType.Combat);
            }

            // [CLEAN CODE]: 3. BẮN PHÁO SÁNG! Thông báo cho toàn cõi server biết sếp vừa chuyển Tab
            // (Thằng CardManager hay InventoryManager nghe thấy sẽ tự động reset)
            OnTabChanged?.Invoke(targetTab);

            // Lưu dữ liệu khi chuyển Tab chính (Navigation)
            if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.SaveGameData();
        });
    }

    // Hàm tiện ích: Duyệt 1 nhát hết cả mảng Popup, code cực ngắn
    private void SetAllPopupsState(bool isActive)
    {
        if (allGlobalPopups == null) return;
        foreach (var popup in allGlobalPopups)
        {
            if (popup != null) popup.SetActive(isActive);
        }
    }

    private void UpdateTabButtons(TabType targetTab)
    {
        for (int i = 0; i < allTabButtons.Length; i++)
        {
            if (i == (int)targetTab) allTabButtons[i].Select();
            else allTabButtons[i].Deselect();
        }
    }

    public void OnTabButtonClicked(int tabIndex)
    {
        SwitchTab((TabType)tabIndex);
    }
}