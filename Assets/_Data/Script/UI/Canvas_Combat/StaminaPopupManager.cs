using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;

public class StaminaPopupManager : BaseDailyBuyPopup
{
    private static StaminaPopupManager _instance;
    public static StaminaPopupManager Instance 
    { 
        get 
        {
            if (_instance == null) return null;
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("--- POPUP CHÍNH (STAMINA) ---")]
    public TextMeshProUGUI txtPopupTimer;
    public Button btnCloseBackground;

    [Header("Tab Buttons (Bar)")]
    public Button btnTabBuy;
    public Button btnTabUse;
    public GameObject panelBuyArea;

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

    public int staminaPerBuy = 60;
    public int staminaPerItem = 50;

    private int _useAmount = 1;
    private int _maxItemInInventory = 0;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[Stamina] Phát hiện có nhiều hơn 1 StaminaPopupManager! Đang xóa bản cũ trên {gameObject.name}");
            // Không xóa cả GameObject vì có thể chứa các UI khác, chỉ xóa Component hoặc báo lỗi
        }
        Instance = this;
        Debug.Log($"[Stamina] Awake trên {gameObject.name}");
        
        // Cấu hình base
        playFabDataKey = "DailyStaminaBuys";
        pricePerUnit = 80;
        maxDailyLimit = 3;
        currencyCode = "GM"; // Linh Ngọc
    }

    private void OnDestroy()
    {
        Debug.Log($"[Stamina] OnDestroy trên {gameObject.name}");
        if (Instance == this) Instance = null;
    }

    protected override void Start()
    {
        base.Start();

        if (btnCloseBackground != null) btnCloseBackground.onClick.AddListener(Close);
        if (btnTabBuy != null) btnTabBuy.onClick.AddListener(() => SwitchTab(true));
        if (btnTabUse != null) btnTabUse.onClick.AddListener(() => SwitchTab(false));

        // Logic Use Item
        if (btnUseMinus10 != null) btnUseMinus10.onClick.AddListener(() => ChangeUseAmount(-10));
        if (btnUseMinus != null) btnUseMinus.onClick.AddListener(() => ChangeUseAmount(-1));
        if (btnUsePlus != null) btnUsePlus.onClick.AddListener(() => ChangeUseAmount(1));
        if (btnUsePlus10 != null) btnUsePlus10.onClick.AddListener(() => ChangeUseAmount(10));
        if (btnUseMax != null) btnUseMax.onClick.AddListener(SetMaxUseAmount);
        if (btnUseSubmit != null) btnUseSubmit.onClick.AddListener(ConfirmUseItem);
    }

    public override void Open()
    {
        base.Open();
        SwitchTab(true);
    }

    public void UpdatePopupRealtimeData(string timeStr, bool isMax, int lnBalance)
    {
        if (this == null) return; // Bảo vệ nếu object đã bị destroy nhưng vẫn bị gọi
        currentVCBalance = lnBalance;
        if (txtPopupTimer != null && panelRoot != null && panelRoot.activeSelf)
        {
            if (isMax) txtPopupTimer.text = "Thời gian còn lại: ĐÃ ĐẦY";
            else txtPopupTimer.text = $"Thời gian còn lại: <color=#FF0000>{timeStr}</color>";
        }
        UpdateUI();
    }

    private void SwitchTab(bool isBuyTab)
    {
        if (panelBuyArea != null) panelBuyArea.SetActive(isBuyTab);
        if (panelUseArea != null) panelUseArea.SetActive(!isBuyTab);

        if (btnTabBuy != null) btnTabBuy.interactable = !isBuyTab;
        if (btnTabUse != null) btnTabUse.interactable = isBuyTab;

        if (isBuyTab)
        {
            RefreshBalance();
            UpdateUI();
        }
        else
        {
            RefreshUseItemData();
            _useAmount = _maxItemInInventory > 0 ? 1 : 0;
            UpdateUseUI();
        }
    }

    protected override void UpdateUI()
    {
        base.UpdateUI();
        // Cập nhật thêm bonus info đặc thù của Stamina
        if (txtBuyDescription != null) 
            txtBuyDescription.text = $"Lần mua này có thể bổ sung: x{currentAmount * staminaPerBuy} Thể lực";
    }

    protected override void OnPurchaseSuccess(int amount, int remainingBalance)
    {
        int totalStaminaGained = amount * staminaPerBuy;
        
        var addStaminaRequest = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = "EN",
            Amount = totalStaminaGained
        };

        PlayFabClientAPI.AddUserVirtualCurrency(addStaminaRequest,
            addResult => {
                Debug.Log($"<color=green>[Stamina]</color> Đã mua {totalStaminaGained} Thể lực.");
                if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.FetchVirtualCurrencies();
            }, null);
    }

    // ==========================================
    // LOGIC TAB 2: DÙNG VẬT PHẨM (GIỮ NGUYÊN)
    // ==========================================
    public void RefreshUseItemData()
    {
        if (staminaPotionData == null) return;
        if (txtUseItemName) txtUseItemName.text = staminaPotionData.itemName;
        if (txtUseItemDesc) txtUseItemDesc.text = staminaPotionData.description;

        _maxItemInInventory = 0;
        if (InventoryManager.Instance != null)
        {
            _maxItemInInventory = InventoryManager.Instance.GetTotalItemAmount(staminaPotionData.itemID);
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
        if (_useAmount <= 0) return;

        // Lưu ý: Item đã bị trừ ở Client trước khi gọi API để UX mượt
        bool isRemoved = InventoryManager.Instance.RemoveItemAmount(staminaPotionData.itemID, _useAmount);

        if (isRemoved)
        {
            SendAddStaminaRequest();
        }
    }

    private void SendAddStaminaRequest()
    {
        int staminaToRecover = _useAmount * staminaPerItem;

        var request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = "EN",
            Amount = staminaToRecover
        };

        PlayFabClientAPI.AddUserVirtualCurrency(request,
            result => {
                if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);
                if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.FetchVirtualCurrencies();
                RefreshUseItemData();
                _useAmount = _maxItemInInventory > 0 ? 1 : 0;
                UpdateUseUI();
            },
            error => {
                Debug.LogWarning("[Stamina] Lỗi bơm thể lực, đang thử lại sau 3s...");
                StartCoroutine(DelayRetry(SendAddStaminaRequest));
            }
        );
    }

    private System.Collections.IEnumerator DelayRetry(System.Action action)
    {
        if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(true);
        yield return new WaitForSeconds(3f);
        action?.Invoke();
    }
}