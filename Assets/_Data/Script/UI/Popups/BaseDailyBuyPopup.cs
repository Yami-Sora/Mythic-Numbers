using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using System;

[Serializable]
public class DailyBuySaveData
{
    public int dailyBuys;
    public string lastBuyDate; // yyyy-MM-dd
}

public abstract class BaseDailyBuyPopup : TabListenerBase
{
    [Header("--- Base UI References ---")]
    public GameObject panelRoot;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtDescription;
    public Button btnClose;

    [Header("--- Selector ---")]
    public Button btnMinus10;
    public Button btnMinus;
    public Button btnPlus;
    public Button btnPlus10;
    public Button btnMax;
    public TextMeshProUGUI txtAmountDisplay;

    [Header("--- Purchase UI ---")]
    public Button btnSubmit;
    public TextMeshProUGUI txtPriceDisplay;
    public TextMeshProUGUI txtLimitInfo;

    [Header("--- Configuration ---")]
    public int pricePerUnit = 10;
    public int maxDailyLimit = 10;
    public string playFabDataKey = "DailyBuyData";
    public string currencyCode = "GM";

    protected int currentAmount = 1;
    protected int alreadyBoughtCount = 0;
    protected int currentVCBalance = 0;

    protected override void Start()
    {
        base.Start();

        if (btnMinus10 != null) btnMinus10.onClick.AddListener(() => ChangeAmount(-10));
        if (btnMinus != null) btnMinus.onClick.AddListener(() => ChangeAmount(-1));
        if (btnPlus != null) btnPlus.onClick.AddListener(() => ChangeAmount(1));
        if (btnPlus10 != null) btnPlus10.onClick.AddListener(() => ChangeAmount(10));
        if (btnMax != null) btnMax.onClick.AddListener(SetMaxAmount);
        
        if (btnSubmit != null) btnSubmit.onClick.AddListener(ConfirmPurchase);
        if (btnClose != null) btnClose.onClick.AddListener(Close);
    }

    public virtual void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        RefreshBalance();
        LoadDailyBuyData();
    }

    public virtual void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    protected override void OnTabChanged(Canvas_NavigationManager.TabType targetTab)
    {
        Close();
    }

    protected void RefreshBalance()
    {
        if (CurrencyUIManager.Instance != null)
        {
            // Mặc định lấy Linh Ngọc, sếp có thể override nếu dùng tiền khác
            currentVCBalance = CurrencyUIManager.Instance.GetCurrentLN();
        }
    }

    // ==========================================
    // LOGIC ĐỒNG BỘ PLAYFAB
    // ==========================================
    protected void LoadDailyBuyData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        PlayFabClientAPI.GetTime(new GetTimeRequest(), timeResult =>
        {
            DateTime serverTime = timeResult.Time;
            string today = serverTime.ToString("yyyy-MM-dd");

            PlayFabClientAPI.GetUserData(new GetUserDataRequest { Keys = new List<string> { playFabDataKey } }, result =>
            {
                if (result.Data != null && result.Data.ContainsKey(playFabDataKey))
                {
                    string json = result.Data[playFabDataKey].Value;
                    var data = JsonUtility.FromJson<DailyBuySaveData>(json);
                    alreadyBoughtCount = (data.lastBuyDate == today) ? data.dailyBuys : 0;
                }
                else
                {
                    alreadyBoughtCount = 0;
                }

                currentAmount = GetRemainingQuota() > 0 ? 1 : 0;
                UpdateUI();
            }, error => {
                alreadyBoughtCount = 0;
                UpdateUI();
            });
        }, error => {
            Debug.LogWarning($"[{name}] Lỗi lấy giờ server: " + error.GenerateErrorReport());
            UpdateUI();
        });
    }

    protected void SaveDailyBuyData()
    {
        var data = new DailyBuySaveData
        {
            dailyBuys = alreadyBoughtCount,
            lastBuyDate = DateTime.UtcNow.ToString("yyyy-MM-dd") // Tạm dùng UtcNow khi lưu
        };

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { playFabDataKey, JsonUtility.ToJson(data) } }
        };

        PlayFabClientAPI.UpdateUserData(request, null, null);
    }

    // ==========================================
    // LOGIC CHỌN SỐ LƯỢNG
    // ==========================================
    protected virtual void ChangeAmount(int change)
    {
        int remainingQuota = GetRemainingQuota();
        if (remainingQuota <= 0)
        {
            currentAmount = 0;
            UpdateUI();
            if (change > 0) ShowWarning("Hết lượt mua hôm nay!");
            return;
        }

        int target = currentAmount + change;
        int maxCanAfford = Mathf.Max(0, currentVCBalance / pricePerUnit);
        int trueMax = Mathf.Min(remainingQuota, maxCanAfford);

        if (change > 0)
        {
            if (target > maxCanAfford && maxCanAfford < remainingQuota) ShowWarning("Không đủ Linh Ngọc!");
            else if (target > remainingQuota) ShowWarning("Đạt giới hạn mua hôm nay!");
        }

        if (trueMax <= 0)
        {
            currentAmount = 0;
        }
        else
        {
            currentAmount = Mathf.Clamp(target, 1, trueMax);
        }

        UpdateUI();
    }

    protected virtual void SetMaxAmount()
    {
        int remainingQuota = GetRemainingQuota();
        int maxCanAfford = currentVCBalance / pricePerUnit;

        if (remainingQuota <= 0) ShowWarning("Hết lượt mua hôm nay!");
        else if (maxCanAfford <= 0) ShowWarning("Không đủ Linh Ngọc!");

        currentAmount = Mathf.Min(maxCanAfford, remainingQuota);
        if (currentAmount < 0) currentAmount = 0;

        UpdateUI();
    }

    protected int GetRemainingQuota() => Mathf.Max(0, maxDailyLimit - alreadyBoughtCount);

    protected virtual void UpdateUI()
    {
        if (txtAmountDisplay != null) txtAmountDisplay.text = currentAmount.ToString();
        if (txtPriceDisplay != null) txtPriceDisplay.text = "x" + (currentAmount * pricePerUnit);
        if (txtLimitInfo != null) txtLimitInfo.text = $"Đã mua: <color=#FFD700>{alreadyBoughtCount}/{maxDailyLimit}</color>";
        
        bool canAfford = currentAmount > 0 && currentVCBalance >= (currentAmount * pricePerUnit);
        if (btnSubmit != null) btnSubmit.interactable = (GetRemainingQuota() > 0 && currentAmount > 0 && canAfford);
    }

    protected void ShowWarning(string message)
    {
        if (VFXManager.Instance != null && txtAmountDisplay != null)
            VFXManager.Instance.SpawnFloatingText(message, txtAmountDisplay.transform);
    }

    // ==========================================
    // LOGIC MUA HÀNG
    // ==========================================
    protected virtual void ConfirmPurchase()
    {
        RefreshBalance();
        if (currentAmount <= 0) return;
        
        if (currentVCBalance < currentAmount * pricePerUnit)
        {
            ShowWarning("Không đủ Linh Ngọc!");
            UpdateUI();
            return;
        }

        if (btnSubmit != null) btnSubmit.interactable = false;

        PlayFabClientAPI.SubtractUserVirtualCurrency(new SubtractUserVirtualCurrencyRequest
        {
            VirtualCurrency = currencyCode,
            Amount = currentAmount * pricePerUnit
        }, result => {
            alreadyBoughtCount += currentAmount;
            SaveDailyBuyData();
            
            OnPurchaseSuccess(currentAmount, result.Balance);
            
            if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.FetchVirtualCurrencies();
            
            currentAmount = (GetRemainingQuota() > 0 && result.Balance >= pricePerUnit) ? 1 : 0;
            UpdateUI();
            Close();
        }, error => {
            Debug.LogError($"[{name}] Lỗi mua hàng: " + error.GenerateErrorReport());
            if (btnSubmit != null) btnSubmit.interactable = true;
        });
    }

    protected abstract void OnPurchaseSuccess(int amount, int remainingBalance);
}
