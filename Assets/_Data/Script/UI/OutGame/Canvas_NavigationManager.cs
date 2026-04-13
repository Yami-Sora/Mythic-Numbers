using UnityEngine;
using System.Collections.Generic;

public class Canvas_NavigationManager : MonoBehaviour
{
    public static Canvas_NavigationManager Instance { get; private set; }

    [Header("Canvas Tabs")]
    [SerializeField] private GameObject shopCanvas;
    [SerializeField] private GameObject cardsCanvas;
    [SerializeField] private GameObject combatCanvas;
    [SerializeField] private GameObject dungeonCanvas;
    [SerializeField] private GameObject inventoryCanvas;

    [Header("Global Popups")]
    [SerializeField] private GameObject gemInfoPopup;
    [SerializeField] private GameObject gemUpgrade;
    [SerializeField] private GameObject RewardPopup;

    private GameObject _currentActiveTab;
    private Dictionary<TabType, GameObject> _tabDictionary;

    public enum TabType { Shop, Cards, Combat, Dungeon, Inventory }

    private void Awake()
    {
        Instance = this;

        if (shopCanvas) shopCanvas.SetActive(true);
        if (cardsCanvas) cardsCanvas.SetActive(true);
        if (combatCanvas) combatCanvas.SetActive(true);
        if (dungeonCanvas) dungeonCanvas.SetActive(true);
        if (inventoryCanvas) inventoryCanvas.SetActive(true);

        if (gemInfoPopup != null) gemInfoPopup.SetActive(true);
        if (gemUpgrade != null) gemUpgrade.SetActive(true);
        if (RewardPopup != null) RewardPopup.SetActive(true);

        InitDictionary();
    }

    private void Start()
    {
        if (gemInfoPopup != null) gemInfoPopup.SetActive(false);
        if (gemUpgrade != null) gemUpgrade.SetActive(false);
        if (RewardPopup != null) RewardPopup.SetActive(false);

        // 2. Lúc này (chuyển sang Start), mọi hàm Awake của tụi đàn em đã chạy xong hết rồi.
        // Giờ mình dọn dẹp: "Trảm" (tắt) hết tụi nó đi trước khi game kịp render khung hình đầu tiên.
        foreach (var tab in _tabDictionary.Values)
        {
            if (tab != null)
            {
                tab.SetActive(false);
            }
        }

        // 3. Cuối cùng, bật duy nhất tab sếp muốn lên (Combat)
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
        if (gemInfoPopup != null) gemInfoPopup.SetActive(false);
        if (gemUpgrade != null) gemUpgrade.SetActive(false);
        if (RewardPopup != null) RewardPopup.SetActive(false);

        // Gọi hiệu ứng chuyển cảnh
        TransitionManager.Instance.PlayTransition(() => {

            // --- ĐOẠN NÀY CHẠY KHI MÀN HÌNH ĐANG ĐEN ---

            // 1. Tắt tab cũ, bật tab mới (Logic cũ của mình)
            if (_currentActiveTab != null) _currentActiveTab.SetActive(false);

            Canvas_CardManager.Instance.OnTransition(); // Reset trạng thái

            if (_tabDictionary.TryGetValue(targetTab, out GameObject targetGO))
            {
                targetGO.SetActive(true);
                _currentActiveTab = targetGO;
            }

            // 2. Cập nhật trạng thái "Nhón chân" của các nút
            for (int i = 0; i < allTabButtons.Length; i++)
            {
                if (i == (int)targetTab)
                    allTabButtons[i].Select();
                else
                    allTabButtons[i].Deselect();
            }
        });
    }

    // Helper cho các nút bấm UI gọi vào
    public void OnTabButtonClicked(int tabIndex)
    {
        SwitchTab((TabType)tabIndex);
    }
}