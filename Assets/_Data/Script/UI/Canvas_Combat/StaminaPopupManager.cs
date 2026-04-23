using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;

// --- CLASS LƯU TRỮ DATA ---
[Serializable]
public class StaminaBuySaveData
{
    public int dailyBuys;
    public string lastBuyDate; // Lưu dưới dạng chuỗi "yyyy-MM-dd"
}

public class StaminaPopupManager : TabListenerBase
{
    public static StaminaPopupManager Instance { get; private set; }

    // --- KEY LƯU PLAYFAB ---
    private const string STAMINA_DATA_KEY = "DailyStaminaBuys";

    [Header("--- POPUP CHÍNH ---")]
    public GameObject staminaPopupPanel;
    public Button btnClosePopup;
    public Button btnCloseBackground;
    public TextMeshProUGUI txtPopupTimer;

    [Header("Tab Buttons (Bar)")]
    public Button btnTabBuy;
    public Button btnTabUse;

    [Header("--- KHU VỰC: MUA BẰNG NGỌC ---")]
    public GameObject panelBuyArea;
    public TextMeshProUGUI txtBuyBonusInfo;
    public TextMeshProUGUI txtBuyAmount;
    public TextMeshProUGUI txtBuyCost;
    public Button btnBuyMinus10, btnBuyMinus, btnBuyPlus, btnBuyPlus10, btnBuyMax;
    public Button btnBuySubmit;

    public int staminaPerBuy = 60;
    public int gemCostPerBuy = 80;

    [Header("Giới Hạn Mua")]
    [SerializeField] public int maxDailyBuyLimit = 3;
    [SerializeField] public TextMeshProUGUI txtBuyLimitInfo;
    private int _currentDailyBuys = 0;

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

    private int _buyAmount = 1;
    private int _useAmount = 1;
    private int _maxItemInInventory = 0;
    private int _currentGemBalance = 0;

    protected override void Awake() => Instance = this;

    protected override void Start()
    {
        base.Start();

        // [MỚI] TẢI DỮ LIỆU MUA TỪ PLAYFAB NGAY KHI VÀO GAME
        LoadDailyBuyData();

        if (btnClosePopup != null) btnClosePopup.onClick.AddListener(ClosePopup);
        if (btnCloseBackground != null) btnCloseBackground.onClick.AddListener(ClosePopup);

        if (btnTabBuy != null) btnTabBuy.onClick.AddListener(() => SwitchTab(true));
        if (btnTabUse != null) btnTabUse.onClick.AddListener(() => SwitchTab(false));

        if (btnBuyMinus10 != null) btnBuyMinus10.onClick.AddListener(() => ChangeBuyAmount(-10));
        if (btnBuyMinus != null) btnBuyMinus.onClick.AddListener(() => ChangeBuyAmount(-1));
        if (btnBuyPlus != null) btnBuyPlus.onClick.AddListener(() => ChangeBuyAmount(1));
        if (btnBuyPlus10 != null) btnBuyPlus10.onClick.AddListener(() => ChangeBuyAmount(10));
        if (btnBuyMax != null) btnBuyMax.onClick.AddListener(SetMaxBuyAmount);
        if (btnBuySubmit != null) btnBuySubmit.onClick.AddListener(ConfirmBuyStamina);

        if (btnUseMinus10 != null) btnUseMinus10.onClick.AddListener(() => ChangeUseAmount(-10));
        if (btnUseMinus != null) btnUseMinus.onClick.AddListener(() => ChangeUseAmount(-1));
        if (btnUsePlus != null) btnUsePlus.onClick.AddListener(() => ChangeUseAmount(1));
        if (btnUsePlus10 != null) btnUsePlus10.onClick.AddListener(() => ChangeUseAmount(10));
        if (btnUseMax != null) btnUseMax.onClick.AddListener(SetMaxUseAmount);
        if (btnUseSubmit != null) btnUseSubmit.onClick.AddListener(ConfirmUseItem);
    }

    protected override void OnTabChanged(Canvas_NavigationManager.TabType targetTab)
    {
        ClosePopup();
    }

    public void OpenPopup()
    {
        if (staminaPopupPanel != null) staminaPopupPanel.SetActive(true);
        SwitchTab(true);
    }

    public void ClosePopup()
    {
        if (staminaPopupPanel != null) staminaPopupPanel.SetActive(false);
    }

    public void UpdatePopupRealtimeData(string timeStr, bool isMax, int gemBalance)
    {
        _currentGemBalance = gemBalance;

        if (txtPopupTimer != null && staminaPopupPanel.activeSelf)
        {
            if (isMax) txtPopupTimer.text = "Thời gian còn lại: ĐÃ ĐẦY";
            else txtPopupTimer.text = $"Thời gian còn lại: <color=#FF0000>{timeStr}</color>";
        }
    }

    // ==========================================
    // LOGIC ĐỒNG BỘ PLAYFAB (TẢI & LƯU)
    // ==========================================
    private void LoadDailyBuyData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            Keys = new List<string> { STAMINA_DATA_KEY }
        }, result => {
            if (result.Data != null && result.Data.ContainsKey(STAMINA_DATA_KEY))
            {
                string json = result.Data[STAMINA_DATA_KEY].Value;
                var data = JsonUtility.FromJson<StaminaBuySaveData>(json);

                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

                if (data.lastBuyDate == today) _currentDailyBuys = data.dailyBuys;
                else _currentDailyBuys = 0;
            }
            else
            {
                _currentDailyBuys = 0;
            }

            // [FIX BUG BẤT ĐỒNG BỘ]: Kéo xong data là phải ép màn hình vẽ lại ngay lập tức!
            UpdateBuyUI();

        }, error => {
            Debug.LogWarning("[PlayFab] Không tìm thấy lịch sử mua thể lực: " + error.GenerateErrorReport());
            _currentDailyBuys = 0;
            UpdateBuyUI(); // Lỗi mạng cũng phải ép nó vẽ lại cho an toàn
        });
    }

    private void SaveDailyBuyData()
    {
        var data = new StaminaBuySaveData
        {
            dailyBuys = _currentDailyBuys,
            lastBuyDate = DateTime.UtcNow.ToString("yyyy-MM-dd") // Đóng dấu ngày hiện tại
        };
        string json = JsonUtility.ToJson(data);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { STAMINA_DATA_KEY, json } }
        };

        PlayFabClientAPI.UpdateUserData(request,
            res => Debug.Log("<color=cyan>[PlayFab] Đã ghi sổ Nam Tào: Số lượt mua thể lực hôm nay!</color>"),
            err => Debug.LogError("[PlayFab] Lỗi ghi sổ mua thể lực: " + err.GenerateErrorReport())
        );
    }

    // ==========================================
    // LOGIC TAB 1: MUA BẰNG NGỌC
    // ==========================================
    private void SwitchTab(bool isBuyTab)
    {
        if (panelBuyArea != null) panelBuyArea.SetActive(isBuyTab);
        if (panelUseArea != null) panelUseArea.SetActive(!isBuyTab);

        if (btnTabBuy != null) btnTabBuy.interactable = !isBuyTab;
        if (btnTabUse != null) btnTabUse.interactable = isBuyTab;

        if (isBuyTab)
        {
            int remainingQuota = maxDailyBuyLimit - _currentDailyBuys;
            int maxCanAfford = _currentGemBalance / gemCostPerBuy;

            if (remainingQuota <= 0 || maxCanAfford <= 0) _buyAmount = 0;
            else _buyAmount = 1;

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
    // LOGIC TAB 1: MUA BẰNG NGỌC (BẢN CÓ FLOATING TEXT)
    // ==========================================
    private void ChangeBuyAmount(int change)
    {
        int remainingQuota = maxDailyBuyLimit - _currentDailyBuys;

        // Cạn lượt mua
        if (remainingQuota <= 0)
        {
            _buyAmount = 0;
            UpdateBuyUI();
            if (change > 0) ShowFloatingWarning("Hết lượt mua hôm nay!");
            return;
        }

        int targetAmount = _buyAmount + change;
        int maxCanAfford = _currentGemBalance / gemCostPerBuy;
        int trueMaxAllowed = Mathf.Min(maxCanAfford, remainingQuota);

        // [TÍNH NĂNG MỚI]: Bắt lỗi khi sếp bấm nút tăng (+)
        if (change > 0)
        {
            // Nếu muốn mua nhiều hơn số tiền chịu được VÀ nghèo là rào cản chính
            if (targetAmount > maxCanAfford && maxCanAfford < remainingQuota)
            {
                ShowFloatingWarning("Không đủ Linh Ngọc!");
            }
            // Nếu đủ tiền nhưng lại đòi mua lố Quota
            else if (targetAmount > remainingQuota)
            {
                ShowFloatingWarning("Đạt giới hạn mua hôm nay!");
            }
        }

        _buyAmount = targetAmount;

        // Ràng buộc số
        if (_buyAmount < 1) _buyAmount = 1;
        if (_buyAmount > trueMaxAllowed) _buyAmount = trueMaxAllowed;
        if (maxCanAfford <= 0) _buyAmount = 0;

        UpdateBuyUI();
    }

    private void SetMaxBuyAmount()
    {
        int remainingQuota = maxDailyBuyLimit - _currentDailyBuys;
        if (remainingQuota <= 0)
        {
            _buyAmount = 0;
            UpdateBuyUI();
            ShowFloatingWarning("Hết lượt mua hôm nay!");
            return;
        }

        int maxCanAfford = _currentGemBalance / gemCostPerBuy;

        // Đỗ nghèo khỉ bấm "Tối đa"
        if (maxCanAfford <= 0)
        {
            _buyAmount = 0;
            UpdateBuyUI();
            ShowFloatingWarning("Không đủ Linh Ngọc!");
            return;
        }

        int trueMaxAllowed = Mathf.Min(maxCanAfford, remainingQuota);
        _buyAmount = trueMaxAllowed > 0 ? trueMaxAllowed : 0;

        UpdateBuyUI();
    }

    // --- HÀM HELPER BẮN CHỮ BAY (Dùng Dotween) ---
    private void ShowFloatingWarning(string message)
    {
        // Mượn ShopManager của sếp để spawn chữ. 
        // Lấy txtBuyAmount làm mốc tọa độ bay lên cho nó nằm giữa Popup!
        if (VFXManager.Instance != null && txtBuyAmount != null)
        {
            VFXManager.Instance.SpawnFloatingText(message, txtBuyAmount.transform);
        }
    }

    private void UpdateBuyUI()
    {
        if (txtBuyAmount != null) txtBuyAmount.text = _buyAmount.ToString();
        if (txtBuyBonusInfo != null) txtBuyBonusInfo.text = $"Lần mua này có thể bổ sung: x{_buyAmount * staminaPerBuy} Thể lực";
        if (txtBuyCost != null) txtBuyCost.text = $"x{_buyAmount * gemCostPerBuy}";

        if (txtBuyLimitInfo != null)
        {
            txtBuyLimitInfo.text = $"Đã mua: <color=#FFD700>{_currentDailyBuys}/{maxDailyBuyLimit}</color>";
        }

        if (btnBuySubmit != null)
        {
            btnBuySubmit.interactable = (_currentDailyBuys < maxDailyBuyLimit) && (_buyAmount > 0);
        }
    }

    private void ConfirmBuyStamina()
    {
        if (_buyAmount <= 0) return;

        int totalGemCost = _buyAmount * gemCostPerBuy;
        int totalStaminaGained = _buyAmount * staminaPerBuy;

        if (btnBuySubmit != null) btnBuySubmit.interactable = false;

        var subtractGemRequest = new SubtractUserVirtualCurrencyRequest
        {
            VirtualCurrency = "GM",
            Amount = totalGemCost
        };

        PlayFabClientAPI.SubtractUserVirtualCurrency(subtractGemRequest,
            subResult =>
            {
                var addStaminaRequest = new AddUserVirtualCurrencyRequest
                {
                    VirtualCurrency = "EN",
                    Amount = totalStaminaGained
                };

                PlayFabClientAPI.AddUserVirtualCurrency(addStaminaRequest,
                    addResult =>
                    {
                        if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.FetchVirtualCurrencies();

                        _currentDailyBuys += _buyAmount;

                        // [MỚI] GIAO DỊCH XONG LÀ GHI SỔ LƯU LÊN MÂY NGAY LẬP TỨC
                        SaveDailyBuyData();

                        int remainingQuota = maxDailyBuyLimit - _currentDailyBuys;
                        int maxCanAfford = subResult.Balance / gemCostPerBuy;

                        if (remainingQuota <= 0 || maxCanAfford <= 0) _buyAmount = 0;
                        else _buyAmount = 1;

                        UpdateBuyUI();
                    },
                    addError =>
                    {
                        Debug.LogError("Lỗi bơm thể lực: " + addError.GenerateErrorReport());
                        if (btnBuySubmit != null) btnBuySubmit.interactable = true;
                    }
                );
            },
            subError =>
            {
                Debug.LogWarning("Giao dịch thất bại: " + subError.GenerateErrorReport());
                if (btnBuySubmit != null) btnBuySubmit.interactable = true;
            }
        );
    }

    // ==========================================
    // LOGIC TAB 2: DÙNG VẬT PHẨM
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

        bool isRemoved = InventoryManager.Instance.RemoveItemAmount(staminaPotionData.itemID, _useAmount);

        if (isRemoved)
        {
            int staminaToRecover = _useAmount * staminaPerItem;

            var request = new AddUserVirtualCurrencyRequest
            {
                VirtualCurrency = "EN",
                Amount = staminaToRecover
            };

            PlayFabClientAPI.AddUserVirtualCurrency(request,
                result => {
                    if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.FetchVirtualCurrencies();
                    RefreshUseItemData();
                    _useAmount = _maxItemInInventory > 0 ? 1 : 0;
                    UpdateUseUI();
                },
                error => Debug.LogError("Lỗi bơm thể lực: " + error.GenerateErrorReport())
            );
        }
    }
}