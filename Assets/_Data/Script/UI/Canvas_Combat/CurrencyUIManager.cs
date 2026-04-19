using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class CurrencyUIManager : MonoBehaviour
{
    public static CurrencyUIManager Instance { get; private set; }

    [Header("UI Tiền Tệ (Top Bar)")]
    public TextMeshProUGUI txtGold;
    public TextMeshProUGUI txtGem;

    [Header("UI Thể Lực (Top Bar)")]
    public TextMeshProUGUI txtStamina;
    public Button btnAddStamina; // Nút "+" ngoài màn hình chính

    [Header("--- POPUP CHÍNH ---")]
    public GameObject staminaPopupPanel;
    public Button btnClosePopup;      // Nút X hoặc nút Đóng trên header
    public Button btnCloseBackground; // Cái Panel_Close to đùng che viền màn hình
    public TextMeshProUGUI txtPopupTimer; // Nằm trong Header: "Thời gian còn lại: 04:59"

    [Header("Tab Buttons (Bar)")]
    public Button btnTabBuy; // Nút "Mua Linh Ngọc"
    public Button btnTabUse; // Nút "Dùng Vật Phẩm"

    [Header("--- KHU VỰC: MUA BẰNG NGỌC ---")]
    public GameObject panelBuyArea;
    public TextMeshProUGUI txtBuyBonusInfo;
    public TextMeshProUGUI txtBuyAmount;
    public TextMeshProUGUI txtBuyCost;
    public Button btnBuyMinus10, btnBuyMinus, btnBuyPlus, btnBuyPlus10, btnBuyMax;
    public Button btnBuySubmit;

    public int staminaPerBuy = 60;
    public int gemCostPerBuy = 80;

    [Header("--- KHU VỰC: DÙNG VẬT PHẨM ---")]
    public GameObject panelUseArea;
    public ItemDataSO staminaPotionData;
    public UI_ItemSlot uiStaminaSlot;
    public TextMeshProUGUI txtUseItemName;
    public TextMeshProUGUI txtUseItemDesc;
    public TextMeshProUGUI txtUseBonusInfo;
    public TextMeshProUGUI txtUseAmount;
    public Button btnUseMinus10, btnUseMinus, btnUsePlus, btnUsePlus10, btnUseMax;
    public Button btnUseSubmit;

    public int staminaPerItem = 50;

    // --- Biến nội bộ Hệ thống ---
    private int currentStamina;
    private int maxStamina = 200;
    private float regenTimer = 0f;
    private bool isRegenerating = false;
    private int _lastDisplayedSecond = -1;

    // --- Biến thao tác UI ---
    private int _buyAmount = 1;
    private int _useAmount = 1;
    private int _maxItemInInventory = 0;
    private int _currentGemBalance = 0;

    private void Awake() => Instance = this;

    private void Start()
    {
        // 1. Nối dây Nút mở/đóng Popup
        if (btnAddStamina != null) btnAddStamina.onClick.AddListener(OpenPopup);

        // Cả nút X và cái Background đen đều gọi chung 1 hàm Đóng
        if (btnClosePopup != null) btnClosePopup.onClick.AddListener(ClosePopup);
        if (btnCloseBackground != null) btnCloseBackground.onClick.AddListener(ClosePopup);

        // 2. Nối dây Tab
        if (btnTabBuy != null) btnTabBuy.onClick.AddListener(() => SwitchTab(true));
        if (btnTabUse != null) btnTabUse.onClick.AddListener(() => SwitchTab(false));

        // 3. Nối dây Khu Mua (Buy Area)
        if (btnBuyMinus10 != null) btnBuyMinus10.onClick.AddListener(() => ChangeBuyAmount(-10));
        if (btnBuyMinus != null) btnBuyMinus.onClick.AddListener(() => ChangeBuyAmount(-1));
        if (btnBuyPlus != null) btnBuyPlus.onClick.AddListener(() => ChangeBuyAmount(1));
        if (btnBuyPlus10 != null) btnBuyPlus10.onClick.AddListener(() => ChangeBuyAmount(10));
        if (btnBuyMax != null) btnBuyMax.onClick.AddListener(SetMaxBuyAmount);
        if (btnBuySubmit != null) btnBuySubmit.onClick.AddListener(ConfirmBuyStamina);

        // 4. Nối dây Khu Dùng Item (Use Area)
        if (btnUseMinus10 != null) btnUseMinus10.onClick.AddListener(() => ChangeUseAmount(-10));
        if (btnUseMinus != null) btnUseMinus.onClick.AddListener(() => ChangeUseAmount(-1));
        if (btnUsePlus != null) btnUsePlus.onClick.AddListener(() => ChangeUseAmount(1));
        if (btnUsePlus10 != null) btnUsePlus10.onClick.AddListener(() => ChangeUseAmount(10));
        if (btnUseMax != null) btnUseMax.onClick.AddListener(SetMaxUseAmount);
        if (btnUseSubmit != null) btnUseSubmit.onClick.AddListener(ConfirmUseItem);
    }

    public void UpdateBalances(int gold, int gem)
    {
        _currentGemBalance = gem;
        if (txtGold) txtGold.text = gold.ToString("N0");
        if (txtGem) txtGem.text = gem.ToString("N0");
    }

    public void UpdateStamina(int staminaAmount, int secondsFromPlayFab)
    {
        currentStamina = staminaAmount;
        regenTimer = secondsFromPlayFab;
        isRegenerating = currentStamina < maxStamina;

        _lastDisplayedSecond = -1;
        RefreshStaminaUI();
    }

    private void Update()
    {
        if (isRegenerating)
        {
            regenTimer -= Time.deltaTime;

            if (regenTimer <= 0)
            {
                currentStamina++;
                regenTimer = 300f;
                isRegenerating = currentStamina < maxStamina;
                _lastDisplayedSecond = -1;
                RefreshStaminaUI();
            }
            else
            {
                int currentSecond = Mathf.CeilToInt(regenTimer);
                if (currentSecond != _lastDisplayedSecond)
                {
                    _lastDisplayedSecond = currentSecond;
                    UpdateTimerText();
                }
            }
        }
    }

    private void RefreshStaminaUI()
    {
        string staminaStr = $"{currentStamina}/{maxStamina}";
        if (txtStamina != null) txtStamina.text = staminaStr;

        if (currentStamina >= maxStamina)
        {
            if (txtPopupTimer != null) txtPopupTimer.text = "Thời gian còn lại: ĐÃ ĐẦY";
            isRegenerating = false;
        }
        else
        {
            UpdateTimerText();
        }
    }

    private void UpdateTimerText()
    {
        TimeSpan time = TimeSpan.FromSeconds(regenTimer);
        string timeStr = time.ToString(@"mm\:ss");

        // Chỉ update cái Text trong Popup nếu nó đang mở cho đỡ tốn tài nguyên
        if (txtPopupTimer != null && staminaPopupPanel != null && staminaPopupPanel.activeSelf)
        {
            txtPopupTimer.text = $"Thời gian còn lại: <color=#FF0000>{timeStr}</color>";
        }
    }

    // ==========================================
    // LOGIC ĐIỀU HƯỚNG POPUP & TAB
    // ==========================================
    private void OpenPopup()
    {
        if (staminaPopupPanel != null) staminaPopupPanel.SetActive(true);
        SwitchTab(true);
        RefreshStaminaUI();
        UpdateTimerText();
    }

    // Tách riêng cái hàm Đóng này ra, lỡ sếp có thích gọi từ EventTrigger ngoài Inspector cũng tiện
    public void ClosePopup()
    {
        if (staminaPopupPanel != null) staminaPopupPanel.SetActive(false);
    }

    private void SwitchTab(bool isBuyTab)
    {
        if (panelBuyArea != null) panelBuyArea.SetActive(isBuyTab);
        if (panelUseArea != null) panelUseArea.SetActive(!isBuyTab);

        if (btnTabBuy != null) btnTabBuy.interactable = !isBuyTab;
        if (btnTabUse != null) btnTabUse.interactable = isBuyTab;

        if (isBuyTab)
        {
            _buyAmount = 1;
            UpdateBuyUI();
        }
        else
        {
            RefreshUseItemData();
            _useAmount = _maxItemInInventory > 0 ? 1 : 0;
            UpdateUseUI();
        }
    }

    // ==========================================
    // LOGIC TAB 1: MUA BẰNG NGỌC
    // ==========================================
    private void ChangeBuyAmount(int change)
    {
        _buyAmount += change;
        if (_buyAmount < 1) _buyAmount = 1;

        int maxCanBuy = _currentGemBalance / gemCostPerBuy;
        if (maxCanBuy < 1) maxCanBuy = 1;

        if (_buyAmount > maxCanBuy) _buyAmount = maxCanBuy;

        UpdateBuyUI();
    }

    private void SetMaxBuyAmount()
    {
        int maxCanBuy = _currentGemBalance / gemCostPerBuy;
        _buyAmount = maxCanBuy > 0 ? maxCanBuy : 1;
        UpdateBuyUI();
    }

    private void UpdateBuyUI()
    {
        if (txtBuyAmount != null) txtBuyAmount.text = _buyAmount.ToString();
        if (txtBuyBonusInfo != null) txtBuyBonusInfo.text = $"Lần mua này có thể bổ sung: x{_buyAmount * staminaPerBuy} Thể lực";
        if (txtBuyCost != null) txtBuyCost.text = $"x{_buyAmount * gemCostPerBuy}";
    }

    private void ConfirmBuyStamina()
    {
        Debug.Log($"[Buy] Sếp Yami muốn tốn {_buyAmount * gemCostPerBuy} Ngọc để đổi lấy {_buyAmount * staminaPerBuy} Thể lực!");
    }

    // ==========================================
    // LOGIC TAB 2: DÙNG VẬT PHẨM
    // ==========================================
    private void RefreshUseItemData()
    {
        if (staminaPotionData == null) return;

        if (txtUseItemName) txtUseItemName.text = staminaPotionData.itemName;
        if (txtUseItemDesc) txtUseItemDesc.text = staminaPotionData.description;

        _maxItemInInventory = 0;
        if (InventoryManager.Instance != null)
        {
            var invItem = InventoryManager.Instance.GetItem(staminaPotionData.itemID);
            if (invItem != null) _maxItemInInventory = invItem.amount;
        }

        if (uiStaminaSlot != null)
        {
            InventoryItem displayItem = new InventoryItem(staminaPotionData, _maxItemInInventory);
            uiStaminaSlot.gameObject.SetActive(true);
            uiStaminaSlot.Setup(displayItem);
        }

        if (btnUseSubmit) btnUseSubmit.interactable = _maxItemInInventory > 0;
    }

    private void ChangeUseAmount(int change)
    {
        if (_maxItemInInventory == 0) return;

        _useAmount += change;
        if (_useAmount < 1) _useAmount = 1;
        if (_useAmount > _maxItemInInventory) _useAmount = _maxItemInInventory;

        UpdateUseUI();
    }

    private void SetMaxUseAmount()
    {
        if (_maxItemInInventory == 0) return;
        _useAmount = _maxItemInInventory;
        UpdateUseUI();
    }

    private void UpdateUseUI()
    {
        if (txtUseAmount != null) txtUseAmount.text = _useAmount.ToString();
        if (txtUseBonusInfo != null) txtUseBonusInfo.text = $"Lần dùng này có thể bổ sung: x{_useAmount * staminaPerItem} Thể lực";
    }

    private void ConfirmUseItem()
    {
        Debug.Log($"[Use] Sếp Yami vừa ực {_useAmount} bình thể lực! Hồi {_useAmount * staminaPerItem} EN.");
    }
}