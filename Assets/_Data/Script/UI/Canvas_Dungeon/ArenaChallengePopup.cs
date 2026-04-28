using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using System;

// --- CLASS LƯU TRỮ DATA ---
[Serializable]
public class ArenaBuySaveData
{
    public int dailyBuys;
    public string lastBuyDate; // Lưu dưới dạng chuỗi "yyyy-MM-dd"
}

public class ArenaChallengePopup : MonoBehaviour
{
    public static ArenaChallengePopup Instance { get; private set; }

    // --- KEY LƯU PLAYFAB ---
    private const string ARENA_DATA_KEY = "DailyArenaBuys";

    [Header("UI References")]
    public GameObject panelRoot;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtDescription;
    public Button btnClose;

    [Header("Selector")]
    public Button btnMinus10;
    public Button btnMinus;
    public Button btnPlus;
    public Button btnPlus10;
    public Button btnMax;
    public TextMeshProUGUI txtAmount;

    [Header("Purchase")]
    public Button btnBuy;
    public TextMeshProUGUI txtPrice;
    public TextMeshProUGUI txtLimit;

    [Header("Settings")]
    public int pricePerTicket = 5;
    public int maxDailyBuys = 10;

    private int _currentAmount = 1;
    private int _alreadyBoughtCount = 0;
    private int _currentLNBalance = 0;

    private void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Start()
    {
        // Tải dữ liệu từ PlayFab ngay khi vào game (nếu muốn) hoặc khi mở popup
        LoadDailyBuyData();

        if (btnMinus10 != null) btnMinus10.onClick.AddListener(() => ChangeAmount(-10));
        if (btnMinus != null) btnMinus.onClick.AddListener(() => ChangeAmount(-1));
        if (btnPlus != null) btnPlus.onClick.AddListener(() => ChangeAmount(1));
        if (btnPlus10 != null) btnPlus10.onClick.AddListener(() => ChangeAmount(10));
        if (btnMax != null) btnMax.onClick.AddListener(SetMaxAmount);
        
        if (btnBuy != null) btnBuy.onClick.AddListener(ConfirmPurchase);
        if (btnClose != null) btnClose.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        if (txtTitle != null) txtTitle.text = "Lượt khiêu chiến";
        if (txtDescription != null) txtDescription.text = "Xác nhận tiêu Linh Ngọc mua Lượt khiêu chiến?";
        
        // Cập nhật số dư LN từ UI
        if (CurrencyUIManager.Instance != null)
        {
            _currentLNBalance = CurrencyUIManager.Instance.GetCurrentLN();
        }

        LoadDailyBuyData();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ==========================================
    // LOGIC ĐỒNG BỘ PLAYFAB (TẢI & LƯU)
    // ==========================================
    private void LoadDailyBuyData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        // Lấy thời gian chuẩn từ Server
        PlayFabClientAPI.GetTime(new GetTimeRequest(), timeResult =>
        {
            DateTime serverTime = timeResult.Time;
            string today = serverTime.ToString("yyyy-MM-dd");

            PlayFabClientAPI.GetUserData(new GetUserDataRequest { Keys = new List<string> { ARENA_DATA_KEY } }, result =>
            {
                if (result.Data != null && result.Data.ContainsKey(ARENA_DATA_KEY))
                {
                    string json = result.Data[ARENA_DATA_KEY].Value;
                    var data = JsonUtility.FromJson<ArenaBuySaveData>(json);
                    
                    if (data.lastBuyDate == today)
                    {
                        _alreadyBoughtCount = data.dailyBuys;
                    }
                    else
                    {
                        _alreadyBoughtCount = 0;
                    }
                }
                else
                {
                    _alreadyBoughtCount = 0;
                }

                _currentAmount = GetRemainingLimit() > 0 ? 1 : 0;
                UpdateBuyUI();
            }, error =>
            {
                Debug.LogWarning("[Arena] Chưa có lịch sử mua lượt: " + error.GenerateErrorReport());
                _alreadyBoughtCount = 0;
                _currentAmount = 1;
                UpdateBuyUI();
            });
        }, error => {
            Debug.LogWarning("[Arena] Lỗi lấy thời gian server: " + error.GenerateErrorReport());
            _currentAmount = 1;
            UpdateBuyUI();
        });
    }

    private void SaveDailyBuyData()
    {
        var data = new ArenaBuySaveData
        {
            dailyBuys = _alreadyBoughtCount,
            lastBuyDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
        };
        string json = JsonUtility.ToJson(data);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { ARENA_DATA_KEY, json } }
        };

        PlayFabClientAPI.UpdateUserData(request,
            res => Debug.Log("<color=cyan>[Arena] Đã ghi sổ Nam Tào: Số lượt mua Arena hôm nay!</color>"),
            err => Debug.LogError("[Arena] Lỗi ghi sổ mua Arena: " + err.GenerateErrorReport())
        );
    }

    private int GetRemainingLimit()
    {
        return Mathf.Max(0, maxDailyBuys - _alreadyBoughtCount);
    }

    // ==========================================
    // LOGIC CHỌN SỐ LƯỢNG
    // ==========================================
    private void ChangeAmount(int change)
    {
        int remainingQuota = maxDailyBuys - _alreadyBoughtCount;

        if (remainingQuota <= 0)
        {
            _currentAmount = 0;
            UpdateBuyUI();
            if (change > 0) ShowFloatingWarning("Hết lượt mua hôm nay!");
            return;
        }

        int targetAmount = _currentAmount + change;
        int maxCanAfford = _currentLNBalance / pricePerTicket;
        int trueMaxAllowed = Mathf.Min(maxCanAfford, remainingQuota);

        if (change > 0)
        {
            if (targetAmount > maxCanAfford && maxCanAfford < remainingQuota)
            {
                ShowFloatingWarning("Không đủ Linh Ngọc!");
            }
            else if (targetAmount > remainingQuota)
            {
                ShowFloatingWarning("Đạt giới hạn mua hôm nay!");
            }
        }

        _currentAmount = targetAmount;

        if (_currentAmount < 1) _currentAmount = 1;
        if (_currentAmount > trueMaxAllowed) _currentAmount = trueMaxAllowed;
        if (maxCanAfford <= 0) _currentAmount = 0;

        UpdateBuyUI();
    }

    private void SetMaxAmount()
    {
        int remainingQuota = maxDailyBuys - _alreadyBoughtCount;
        if (remainingQuota <= 0)
        {
            _currentAmount = 0;
            UpdateBuyUI();
            ShowFloatingWarning("Hết lượt mua hôm nay!");
            return;
        }

        int maxCanAfford = _currentLNBalance / pricePerTicket;

        if (maxCanAfford <= 0)
        {
            _currentAmount = 0;
            UpdateBuyUI();
            ShowFloatingWarning("Không đủ Linh Ngọc!");
            return;
        }

        int trueMaxAllowed = Mathf.Min(maxCanAfford, remainingQuota);
        _currentAmount = trueMaxAllowed > 0 ? trueMaxAllowed : 0;

        UpdateBuyUI();
    }

    private void ShowFloatingWarning(string message)
    {
        if (VFXManager.Instance != null && txtAmount != null)
        {
            VFXManager.Instance.SpawnFloatingText(message, txtAmount.transform);
        }
    }

    private void UpdateBuyUI()
    {
        if (txtAmount != null) txtAmount.text = _currentAmount.ToString();
        if (txtPrice != null) txtPrice.text = "x" + (_currentAmount * pricePerTicket);
        
        if (txtLimit != null) 
        {
            txtLimit.text = $"Đã mua: <color=#FFD700>{_alreadyBoughtCount}/{maxDailyBuys}</color>";
        }

        if (btnBuy != null)
        {
            btnBuy.interactable = (_alreadyBoughtCount < maxDailyBuys) && (_currentAmount > 0);
        }
    }

    private void ConfirmPurchase()
    {
        if (_currentAmount <= 0) return;

        int totalCost = _currentAmount * pricePerTicket;

        if (btnBuy != null) btnBuy.interactable = false;

        var request = new SubtractUserVirtualCurrencyRequest
        {
            VirtualCurrency = PlayFabConstants.CURRENCY_LN,
            Amount = totalCost
        };

        PlayFabClientAPI.SubtractUserVirtualCurrency(request, 
            result => {
                Debug.Log($"<color=green>[Arena]</color> Đã mua {_currentAmount} lượt với giá {totalCost} LN.");
                
                if (ArenaManager.Instance != null)
                {
                    ArenaManager.Instance.AddChallenges(_currentAmount); 
                }

                if (PlayFabDataManager.Instance != null)
                {
                    PlayFabDataManager.Instance.FetchVirtualCurrencies();
                }

                _alreadyBoughtCount += _currentAmount;
                SaveDailyBuyData();

                int remainingQuota = maxDailyBuys - _alreadyBoughtCount;
                int maxCanAfford = result.Balance / pricePerTicket;

                if (remainingQuota <= 0 || maxCanAfford <= 0) _currentAmount = 0;
                else _currentAmount = 1;

                UpdateBuyUI();
                Close();
            },
            error => {
                Debug.LogError("[Arena] Lỗi trừ tiền: " + error.GenerateErrorReport());
                if (btnBuy != null) btnBuy.interactable = true;
            }
        );
    }
}
